using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace API_diag
{
    public delegate void IconeChargeeCallback(Bitmap bitmap);

    // Gère le téléchargement, la mise en cache disque et la limitation de concurrence
    // pour les icônes d'applications - conçu pour ne jamais bloquer l'UI ni saturer
    // un appareil WinMo lent.
    public static class IconLoader
    {
        private static readonly object _verrou = new object();
        private static readonly Queue<TacheIcone> _file = new Queue<TacheIcone>();
        private static int _threadsActifs = 0;
        private const int MaxThreadsSimultanes = 2;

        private class TacheIcone
        {
            public string Url;
            public string CheminCache;
            public Control ControleCible;
            public IconeChargeeCallback Callback;
        }

        private static string ObtenirDossierApplication()
        {
            string cheminAssembly = System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase;
            return Path.GetDirectoryName(cheminAssembly);
        }

        public static void Charger(string url, string idApplication, Control controleCible, IconeChargeeCallback callback)
        {
            string dossierCache = ObtenirDossierApplication();
            if (!Directory.Exists(dossierCache))
            {
                try { Directory.CreateDirectory(dossierCache); }
                catch { /* si la création échoue, on continue sans cache disque */ }
            }
            string cheminCache = Path.Combine(dossierCache, idApplication + ".icon");

            if (File.Exists(cheminCache))
            {
                // Déjà en cache : lecture disque sur un thread séparé (le décodage
                // de l'image reste coûteux même sans réseau, on évite de figer l'UI).
                Thread t = new Thread(new ThreadStart(delegate
                {
                    Bitmap bmp = null;
                    try { bmp = new Bitmap(cheminCache); }
                    catch { bmp = null; }
                    LivrerResultat(controleCible, callback, bmp);
                }));
                t.IsBackground = true;
                t.Start();
                return;
            }

            TacheIcone tache = new TacheIcone();
            tache.Url = url;
            tache.CheminCache = cheminCache;
            tache.ControleCible = controleCible;
            tache.Callback = callback;

            lock (_verrou)
            {
                _file.Enqueue(tache);
                if (_threadsActifs < MaxThreadsSimultanes)
                {
                    _threadsActifs++;
                    Thread worker = new Thread(new ThreadStart(TraiterFile));
                    worker.IsBackground = true;
                    worker.Start();
                }
            }
        }

        private static void TraiterFile()
        {
            while (true)
            {
                TacheIcone tache;
                lock (_verrou)
                {
                    if (_file.Count == 0)
                    {
                        _threadsActifs--;
                        return;
                    }
                    tache = _file.Dequeue();
                }

                Bitmap bmp = null;
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(tache.Url);
                    request.Method = "GET";
                    request.Timeout = 8000;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    using (Stream flux = response.GetResponseStream())
                    using (MemoryStream ms = new MemoryStream())
                    {
                        byte[] buffer = new byte[2048];
                        int lu;
                        while ((lu = flux.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, lu);
                        }

                        byte[] octets = ms.ToArray();

                        try
                        {
                            using (FileStream fs = new FileStream(tache.CheminCache, FileMode.Create, FileAccess.Write))
                            {
                                fs.Write(octets, 0, octets.Length);
                            }
                        }
                        catch { /* cache non critique : l'icône reste utilisable même si l'écriture échoue */ }

                        ms.Position = 0;
                        bmp = new Bitmap(ms);
                    }
                }
                catch
                {
                    bmp = null;
                }

                LivrerResultat(tache.ControleCible, tache.Callback, bmp);
            }
        }

        private static void LivrerResultat(Control controleCible, IconeChargeeCallback callback, Bitmap bmp)
        {
            if (bmp == null) return;

            try
            {
                controleCible.Invoke(new IconeChargeeCallback(callback), new object[] { bmp });
            }
            catch { /* le contrôle a pu être détruit entre-temps (changement de page) */ }
        }
    }
}