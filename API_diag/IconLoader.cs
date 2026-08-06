using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace API_diag
{
    public delegate void IconLoadedCallback(Bitmap bitmap);

    // Handles downloading, disk caching, and concurrency limiting for application icons -
    // designed to never block the UI or overload a slow WinMo device.
    public static class IconLoader
    {
        private static readonly object _lock = new object();
        private static readonly Queue<IconTask> _queue = new Queue<IconTask>();
        private static int _activeThreads = 0;
        private const int MaxConcurrentThreads = 2;

        private class IconTask
        {
            public string Url;
            public string CachePath;
            public Control TargetControl;
            public IconLoadedCallback Callback;
        }

        private static string GetApplicationFolder()
        {
            string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
            return Path.GetDirectoryName(assemblyPath);
        }

        public static void Load(string url, string applicationId, Control targetControl, IconLoadedCallback callback)
        {
            string cacheFolder = Path.Combine(GetApplicationFolder(), "IconCache");
            if (!Directory.Exists(cacheFolder))
            {
                try { Directory.CreateDirectory(cacheFolder); }
                catch { /* if creation fails, continue without disk cache */ }
            }
            string cachePath = Path.Combine(cacheFolder, applicationId + ".icon");

            if (File.Exists(cachePath))
            {
                // Already cached: read from disk on a separate thread (image decoding
                // is still costly even without network, so we avoid freezing the UI).
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Bitmap bmp = null;
                    try { bmp = new Bitmap(cachePath); }
                    catch { bmp = null; }
                    DeliverResult(targetControl, callback, bmp);
                }));
                t.IsBackground = true;
                t.Start();
                return;
            }

            IconTask task = new IconTask();
            task.Url = url;
            task.CachePath = cachePath;
            task.TargetControl = targetControl;
            task.Callback = callback;

            lock (_lock)
            {
                _queue.Enqueue(task);
                if (_activeThreads < MaxConcurrentThreads)
                {
                    _activeThreads++;
                    Thread worker = new Thread(new ThreadStart(ProcessQueue));
                    worker.IsBackground = true;
                    worker.Start();
                }
            }
        }

        private static void ProcessQueue()
        {
            while (true)
            {
                IconTask task;
                lock (_lock)
                {
                    if (_queue.Count == 0)
                    {
                        _activeThreads--;
                        return;
                    }
                    task = _queue.Dequeue();
                }

                Bitmap bmp = null;
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(task.Url);
                    request.Method = "GET";
                    request.Timeout = 8000;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream stream = response.GetResponseStream())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[2048];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, read);
                        }

                        byte[] bytes = ms.ToArray();

                        try
                        {
                            using (FileStream fs = new FileStream(task.CachePath, FileMode.Create, FileAccess.Write))
                            {
                                fs.Write(bytes, 0, bytes.Length);
                            }
                        }
                        catch { /* cache is non-critical: the icon still works even if writing fails */ }

                        ms.Position = 0;
                        bmp = new Bitmap(ms);
                    }
                }
                catch
                {
                    bmp = null;
                }

                DeliverResult(task.TargetControl, task.Callback, bmp);
            }
        }

        private static void DeliverResult(Control targetControl, IconLoadedCallback callback, Bitmap bmp)
        {
            if (bmp == null) return;

            try
            {
                targetControl.Invoke(new IconLoadedCallback(callback), new object[] { bmp });
            }
            catch { /* the control may have been destroyed in the meantime (page change) */ }
        }
    }
}