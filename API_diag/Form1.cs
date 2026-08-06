using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;
using CodeBetter.Json;
using System.Reflection;
using System.Drawing.Imaging;

namespace API_diag
{
    public partial class WinMoStore : Form
    {
        //private const string ApiBaseUrl = "http://192.168.1.102:3000";
        private const string ApiBaseUrl = "http://vps-f5acc160.vps.ovh.net:3000";

        // --- List page ---
        private Panel panelListHeader;
        private Panel logoMark;
        private Label labelLogo;
        private Label labelListTitle;
        private Label labelListSubtitle;
        private Panel healthDot;
        private Panel panelSearch;
        private Panel panelSearchPill;
        private TextBox textBoxSearch;
        private Button buttonSearch;
        private Panel panelUpdate;
        private Button buttonUpdate;
        private ProgressBar progressUpdate;
        private Label labelUpdateStatus;
        private Label labelStatus;
        private Panel panelList;
        private MainMenu mainMenu;
        private MenuItem menuItemRefresh;
        private MenuItem menuItemExit;

        // --- Details page ---
        private Panel panelDetails;
        private Panel panelDetailsHeader;
        private Label labelDetailsTitle;
        private Panel infoCard;
        private Label labelDetailsName;
        private Label labelDetailsId;
        private Button buttonDownload;
        private Button buttonBack;
        private ProgressBar progressDownload;
        private Label labelDownloadStatus;

        private const int headerHeight = 54;
        private bool isLoading = false;
        private bool isDownloading = false;
        private bool isCheckingHealth = false;
        private bool isCheckingUpdate = false;
        private bool isDownloadingUpdate = false;

        private ApplicationApi _currentApp;
        private ApplicationApi[] _lastApplications;
        private string _availableUpdateVersion;
        private string _downloadedUpdatePath; // non-null once the update .cab has been downloaded
        private Color _healthColor = Theme.HealthColorUnknown;
        private System.Windows.Forms.Timer healthTimer;

        private delegate void LoadCompletedDelegate(string jsonResponse, string error);
        private delegate void DownloadProgressDelegate(int percentage);
        private delegate void DownloadCompletedDelegate(string filePath, string error);
        private delegate void HealthCheckedDelegate(bool online);
        private delegate void UpdateCheckedDelegate(bool available, string version);
        private delegate void UpdateProgressDelegate(int percentage);
        private delegate void UpdateDownloadCompletedDelegate(string filePath, string error);

        public WinMoStore()
        {
            CreateControls();
            LayoutControls();
            this.Resize += new EventHandler(WinMoStore_Resize);
            InitializeHealthMonitoring();
            CheckForUpdate();
        }

        private void WinMoStore_Resize(object sender, EventArgs e)
        {
            LayoutControls();

            if (_lastApplications != null)
            {
                DisplayApplications(_lastApplications);
            }
        }

        private void CreateControls()
        {
            this.Text = "WinMo Store";
            this.BackColor = Theme.BackgroundColor;
            this.WindowState = FormWindowState.Maximized;

            menuItemRefresh = new MenuItem();
            menuItemRefresh.Text = "Refresh";
            menuItemRefresh.Click += new EventHandler(menuItemRefresh_Click);

            menuItemExit = new MenuItem();
            menuItemExit.Text = "Exit";
            menuItemExit.Click += new EventHandler(menuItemExit_Click);

            mainMenu = new MainMenu();
            mainMenu.MenuItems.Add(menuItemRefresh);
            mainMenu.MenuItems.Add(menuItemExit);
            this.Menu = mainMenu;

            // ===================== LIST PAGE =====================

            panelListHeader = new Panel();
            panelListHeader.BackColor = Theme.BackgroundColor;

            logoMark = new Panel();
            logoMark.BackColor = Theme.BackgroundColor;
            logoMark.Paint += new PaintEventHandler(LogoMark_Paint);

            labelLogo = new Label();
            labelLogo.Text = "W";
            labelLogo.Font = Theme.FontLogo;
            labelLogo.ForeColor = Theme.TextColorLight;
            labelLogo.BackColor = Theme.AccentColor;
            labelLogo.TextAlign = ContentAlignment.TopCenter;
            logoMark.Controls.Add(labelLogo);

            labelListTitle = new Label();
            labelListTitle.Text = "WinMo Store";
            labelListTitle.ForeColor = Theme.TextColor;
            labelListTitle.Font = Theme.FontHeader;

            labelListSubtitle = new Label();
            labelListSubtitle.Text = "v" + GetApplicationVersion();
            labelListSubtitle.ForeColor = Theme.TextColorSecondary;
            labelListSubtitle.Font = Theme.FontSubtitle;

            healthDot = new Panel();
            healthDot.BackColor = Theme.BackgroundColor;
            healthDot.Paint += new PaintEventHandler(HealthDot_Paint);

            panelListHeader.Controls.Add(logoMark);
            panelListHeader.Controls.Add(labelListTitle);
            panelListHeader.Controls.Add(labelListSubtitle);
            panelListHeader.Controls.Add(healthDot);

            // --- Pill-shaped search bar ---
            panelSearch = new Panel();
            panelSearch.BackColor = Theme.BackgroundColor;

            panelSearchPill = new Panel();
            panelSearchPill.BackColor = Theme.BackgroundColor;
            panelSearchPill.Paint += new PaintEventHandler(SearchPill_Paint);

            textBoxSearch = new TextBox();
            textBoxSearch.Font = Theme.FontNormal;
            textBoxSearch.ForeColor = Theme.TextColor;
            textBoxSearch.BackColor = Theme.CardColor;
            try { textBoxSearch.BorderStyle = BorderStyle.None; }
            catch { /* ignored if not supported by the ROM */ }
            textBoxSearch.KeyDown += new KeyEventHandler(textBoxSearch_KeyDown);
            panelSearchPill.Controls.Add(textBoxSearch);

            buttonSearch = new Button();
            buttonSearch.Text = "→";
            buttonSearch.Font = Theme.FontButton;
            buttonSearch.ForeColor = Theme.TextColorLight;
            buttonSearch.BackColor = Theme.AccentColor;
            buttonSearch.Click += new EventHandler(buttonSearch_Click);

            panelSearch.Controls.Add(panelSearchPill);
            panelSearch.Controls.Add(buttonSearch);

            // --- "Update available" block: hidden until an update is detected ---
            panelUpdate = new Panel();
            panelUpdate.BackColor = Theme.BackgroundColor;
            panelUpdate.Visible = false;

            buttonUpdate = new Button();
            buttonUpdate.Text = "Update available";
            buttonUpdate.Font = Theme.FontButton;
            buttonUpdate.ForeColor = Theme.TextColorLight;
            buttonUpdate.BackColor = Theme.AccentColor;
            buttonUpdate.Click += new EventHandler(buttonUpdate_Click);

            progressUpdate = new ProgressBar();
            progressUpdate.Minimum = 0;
            progressUpdate.Maximum = 100;
            progressUpdate.Visible = false;

            labelUpdateStatus = new Label();
            labelUpdateStatus.Font = Theme.FontSmall;
            labelUpdateStatus.ForeColor = Theme.TextColorSecondary;
            labelUpdateStatus.Visible = false;

            panelUpdate.Controls.Add(buttonUpdate);
            panelUpdate.Controls.Add(progressUpdate);
            panelUpdate.Controls.Add(labelUpdateStatus);

            labelStatus = new Label();
            labelStatus.ForeColor = Theme.TextColorSecondary;
            labelStatus.Font = Theme.FontSmall;
            labelStatus.Text = "Press Refresh to load the list.";

            panelList = new Panel();
            panelList.BackColor = Theme.BackgroundColor;
            panelList.AutoScroll = true;

            // ===================== DETAILS PAGE =====================

            panelDetails = new Panel();
            panelDetails.BackColor = Theme.BackgroundColor;
            panelDetails.Visible = false;
            panelDetails.AutoScroll = true;

            panelDetailsHeader = new Panel();
            panelDetailsHeader.BackColor = Theme.BackgroundColor;

            labelDetailsTitle = new Label();
            labelDetailsTitle.Text = "Application details";
            labelDetailsTitle.ForeColor = Theme.TextColor;
            labelDetailsTitle.Font = Theme.FontHeader;
            panelDetailsHeader.Controls.Add(labelDetailsTitle);

            infoCard = new Panel();
            infoCard.BackColor = Theme.BackgroundColor;
            infoCard.Paint += new PaintEventHandler(InfoCard_Paint);

            labelDetailsName = new Label();
            labelDetailsName.Font = Theme.FontDetailsTitle;
            labelDetailsName.ForeColor = Theme.TextColor;

            labelDetailsId = new Label();
            labelDetailsId.Font = Theme.FontSmall;
            labelDetailsId.ForeColor = Theme.TextColorSecondary;

            infoCard.Controls.Add(labelDetailsName);
            infoCard.Controls.Add(labelDetailsId);

            buttonDownload = new Button();
            buttonDownload.Text = "Download";
            buttonDownload.Font = Theme.FontButton;
            buttonDownload.ForeColor = Theme.TextColorLight;
            buttonDownload.BackColor = Theme.AccentColor;
            buttonDownload.Click += new EventHandler(buttonDownload_Click);

            progressDownload = new ProgressBar();
            progressDownload.Minimum = 0;
            progressDownload.Maximum = 100;
            progressDownload.Visible = false;

            labelDownloadStatus = new Label();
            labelDownloadStatus.Font = Theme.FontSmall;
            labelDownloadStatus.ForeColor = Theme.TextColorSecondary;
            labelDownloadStatus.Visible = false;

            buttonBack = new Button();
            buttonBack.Text = "‹ Back";
            buttonBack.Font = Theme.FontNormal;
            buttonBack.ForeColor = Theme.TextColor;
            buttonBack.BackColor = Theme.CardColor;
            buttonBack.Click += new EventHandler(buttonBack_Click);

            panelDetails.Controls.Add(buttonBack);
            panelDetails.Controls.Add(labelDownloadStatus);
            panelDetails.Controls.Add(progressDownload);
            panelDetails.Controls.Add(buttonDownload);
            panelDetails.Controls.Add(infoCard);
            panelDetails.Controls.Add(panelDetailsHeader);

            this.Controls.Add(panelDetails);
            this.Controls.Add(panelList);
            this.Controls.Add(labelStatus);
            this.Controls.Add(panelUpdate);
            this.Controls.Add(panelSearch);
            this.Controls.Add(panelListHeader);
        }

        private void LayoutControls()
        {
            int width = this.ClientSize.Width;
            int height = this.ClientSize.Height;

            // ===== LIST PAGE =====
            panelListHeader.Left = 0;
            panelListHeader.Top = 0;
            panelListHeader.Width = width;
            panelListHeader.Height = headerHeight;

            logoMark.Left = 10;
            logoMark.Top = (headerHeight - 32) / 2;
            logoMark.Width = 32;
            logoMark.Height = 32;

            labelLogo.Left = 6;
            labelLogo.Top = 6;
            labelLogo.Width = 20;
            labelLogo.Height = 20;

            healthDot.Width = 12;
            healthDot.Height = 12;
            healthDot.Left = width - 22;
            healthDot.Top = (headerHeight - 12) / 2;

            labelListTitle.Left = logoMark.Left + logoMark.Width + 10;
            labelListTitle.Top = 6;
            labelListTitle.Width = healthDot.Left - labelListTitle.Left - 6;
            labelListTitle.Height = 20;

            labelListSubtitle.Left = labelListTitle.Left;
            labelListSubtitle.Top = labelListTitle.Top + labelListTitle.Height;
            labelListSubtitle.Width = labelListTitle.Width;
            labelListSubtitle.Height = 16;

            panelSearch.Left = 0;
            panelSearch.Top = panelListHeader.Top + panelListHeader.Height;
            panelSearch.Width = width;
            panelSearch.Height = 44;

            buttonSearch.Width = 44;
            buttonSearch.Height = 32;
            buttonSearch.Left = panelSearch.Width - buttonSearch.Width - 8;
            buttonSearch.Top = (panelSearch.Height - buttonSearch.Height) / 2;

            panelSearchPill.Left = 8;
            panelSearchPill.Top = (panelSearch.Height - 32) / 2;
            panelSearchPill.Width = buttonSearch.Left - 8 - 8;
            panelSearchPill.Height = 32;

            textBoxSearch.Left = 14;
            textBoxSearch.Top = (panelSearchPill.Height - 20) / 2;
            textBoxSearch.Width = panelSearchPill.Width - 24;
            textBoxSearch.Height = 20;

            panelUpdate.Left = 0;
            panelUpdate.Top = panelSearch.Top + panelSearch.Height;
            panelUpdate.Width = width;
            panelUpdate.Height = 78;

            buttonUpdate.Left = 8;
            buttonUpdate.Top = 6;
            buttonUpdate.Width = width - 16;
            buttonUpdate.Height = 34;

            progressUpdate.Left = 8;
            progressUpdate.Top = buttonUpdate.Top + buttonUpdate.Height + 6;
            progressUpdate.Width = width - 16;
            progressUpdate.Height = 16;

            labelUpdateStatus.Left = 8;
            labelUpdateStatus.Top = progressUpdate.Top + progressUpdate.Height + 4;
            labelUpdateStatus.Width = width - 16;
            labelUpdateStatus.Height = 16;

            int topAfterSearch = panelUpdate.Visible
                ? panelUpdate.Top + panelUpdate.Height
                : panelSearch.Top + panelSearch.Height;

            labelStatus.Left = 8;
            labelStatus.Top = topAfterSearch + 4;
            labelStatus.Width = width - 16;
            labelStatus.Height = 18;

            panelList.Left = 0;
            panelList.Top = labelStatus.Top + labelStatus.Height + 2;
            panelList.Width = width;
            panelList.Height = height - panelList.Top;

            // ===== DETAILS PAGE =====
            panelDetails.Left = 0;
            panelDetails.Top = 0;
            panelDetails.Width = width;
            panelDetails.Height = height;

            panelDetailsHeader.Left = 0;
            panelDetailsHeader.Top = 0;
            panelDetailsHeader.Width = panelDetails.Width;
            panelDetailsHeader.Height = headerHeight - 20;

            labelDetailsTitle.Left = 8;
            labelDetailsTitle.Top = 10;
            labelDetailsTitle.Width = panelDetailsHeader.Width - 16;
            labelDetailsTitle.Height = 22;

            infoCard.Left = 10;
            infoCard.Top = panelDetailsHeader.Height + 12;
            infoCard.Width = panelDetails.Width - 20;
            infoCard.Height = 90;

            labelDetailsName.Left = 14;
            labelDetailsName.Top = 12;
            labelDetailsName.Width = infoCard.Width - 28;
            labelDetailsName.Height = 40;

            labelDetailsId.Left = 14;
            labelDetailsId.Top = labelDetailsName.Top + labelDetailsName.Height;
            labelDetailsId.Width = infoCard.Width - 28;
            labelDetailsId.Height = 30;

            buttonDownload.Left = 10;
            buttonDownload.Top = infoCard.Top + infoCard.Height + 16;
            buttonDownload.Width = panelDetails.Width - 20;
            buttonDownload.Height = 42;

            progressDownload.Left = 10;
            progressDownload.Top = buttonDownload.Top + buttonDownload.Height + 8;
            progressDownload.Width = panelDetails.Width - 20;
            progressDownload.Height = 18;

            labelDownloadStatus.Left = 10;
            labelDownloadStatus.Top = progressDownload.Top + progressDownload.Height + 4;
            labelDownloadStatus.Width = panelDetails.Width - 20;
            labelDownloadStatus.Height = 16;

            buttonBack.Left = 10;
            buttonBack.Top = labelDownloadStatus.Top + labelDownloadStatus.Height + 8;
            buttonBack.Width = panelDetails.Width - 20;
            buttonBack.Height = 36;
        }

        private void LogoMark_Paint(object sender, PaintEventArgs pe)
        {
            Panel p = (Panel)sender;
            using (SolidBrush brush = new SolidBrush(Theme.AccentColor))
            {
                RoundedRendering.FillRoundedRectangle(pe.Graphics, brush, 0, 0, p.Width, p.Height, 10);
            }
        }

        private void SearchPill_Paint(object sender, PaintEventArgs pe)
        {
            Panel p = (Panel)sender;
            using (SolidBrush brush = new SolidBrush(Theme.CardColor))
            {
                RoundedRendering.FillRoundedRectangle(pe.Graphics, brush, 0, 0, p.Width, p.Height, p.Height / 2);
            }
        }

        private void InfoCard_Paint(object sender, PaintEventArgs pe)
        {
            Panel p = (Panel)sender;
            using (SolidBrush brush = new SolidBrush(Theme.CardColor))
            {
                RoundedRendering.FillRoundedRectangle(pe.Graphics, brush, 0, 0, p.Width, p.Height, 14);
            }
        }

        private void HealthDot_Paint(object sender, PaintEventArgs pe)
        {
            Panel p = (Panel)sender;
            using (SolidBrush brush = new SolidBrush(_healthColor))
            {
                pe.Graphics.FillEllipse(brush, 0, 0, p.Width - 1, p.Height - 1);
            }
        }

        #region /api/health monitoring

        private void InitializeHealthMonitoring()
        {
            healthTimer = new System.Windows.Forms.Timer();
            healthTimer.Interval = 15000;
            healthTimer.Tick += new EventHandler(healthTimer_Tick);
            healthTimer.Enabled = true;

            CheckHealth();
        }

        private void healthTimer_Tick(object sender, EventArgs e)
        {
            CheckHealth();
        }

        private void CheckHealth()
        {
            if (isCheckingHealth) return;
            isCheckingHealth = true;

            Thread healthThread = new Thread(new ThreadStart(delegate
            {
                bool online = false;
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiBaseUrl + "/api/health");
                    request.Method = "GET";
                    request.Timeout = 5000;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        online = (response.StatusCode == HttpStatusCode.OK);
                    }
                }
                catch
                {
                    online = false;
                }

                this.Invoke(new HealthCheckedDelegate(HealthChecked), new object[] { online });
            }));

            healthThread.IsBackground = true;
            healthThread.Start();
        }

        private void HealthChecked(bool online)
        {
            isCheckingHealth = false;
            _healthColor = online ? Theme.HealthColorOk : Theme.HealthColorKo;
            healthDot.Invalidate();
        }

        #endregion

        #region Application update check

        private void CheckForUpdate()
        {
            if (isCheckingUpdate) return;
            isCheckingUpdate = true;

            Thread updateThread = new Thread(new ThreadStart(delegate
            {
                bool available = false;
                string remoteVersion = null;

                try
                {
                    string json = GetApiData(ApiBaseUrl + "/api/getAppVersion");
                    AppVersionResponse response = Converter.Deserialize<AppVersionResponse>(json);

                    if (response != null && response.success && !string.IsNullOrEmpty(response.version))
                    {
                        remoteVersion = response.version;
                        string localVersion = GetApplicationVersion();
                        available = CompareVersions(remoteVersion, localVersion) > 0;
                    }
                }
                catch
                {
                    available = false;
                }

                this.Invoke(new UpdateCheckedDelegate(UpdateChecked), new object[] { available, remoteVersion });
            }));

            updateThread.IsBackground = true;
            updateThread.Start();
        }

        private void UpdateChecked(bool available, string version)
        {
            isCheckingUpdate = false;
            _availableUpdateVersion = version;

            if (available)
            {
                buttonUpdate.Text = "Update available (v" + version + ")";
                buttonUpdate.Enabled = true;
                progressUpdate.Visible = false;
                progressUpdate.Value = 0;
                labelUpdateStatus.Visible = false;
                _downloadedUpdatePath = null; // new version detected: back to "needs download" state
            }

            panelUpdate.Visible = available;

            LayoutControls();
        }

        private static int CompareVersions(string a, string b)
        {
            int[] pa = ParseVersionParts(a);
            int[] pb = ParseVersionParts(b);

            for (int i = 0; i < 3; i++)
            {
                if (pa[i] != pb[i]) return pa[i] - pb[i];
            }
            return 0;
        }

        private static int[] ParseVersionParts(string v)
        {
            int[] parts = new int[3];
            if (string.IsNullOrEmpty(v)) return parts;

            string[] segments = v.Split('.');
            for (int i = 0; i < 3 && i < segments.Length; i++)
            {
                try
                {
                    parts[i] = int.Parse(segments[i]);
                }
                catch
                {
                    parts[i] = 0;
                }
            }
            return parts;
        }

        // Toggles between "download" and "install" depending on whether a .cab has already been downloaded.
        private void buttonUpdate_Click(object sender, EventArgs e)
        {
            if (_downloadedUpdatePath != null)
            {
                InstallUpdate();
                return;
            }

            if (isDownloadingUpdate) return;

            isDownloadingUpdate = true;
            buttonUpdate.Enabled = false;
            buttonUpdate.Text = "Downloading...";
            progressUpdate.Visible = true;
            progressUpdate.Value = 0;
            labelUpdateStatus.Visible = true;
            labelUpdateStatus.Text = "Starting...";

            string cabUrl = ApiBaseUrl + "/api/updateAppCab";
            const string fileName = "WinMoStore.cab";

            Thread updateDownloadThread = new Thread(new ThreadStart(delegate
            {
                string filePath = null;
                string error = null;

                try
                {
                    filePath = DownloadUpdateFile(cabUrl, fileName);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                this.Invoke(new UpdateDownloadCompletedDelegate(UpdateDownloadCompleted), new object[] { filePath, error });
            }));

            updateDownloadThread.Start();
        }

        private string DownloadUpdateFile(string url, string fileName)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                long totalSize = response.ContentLength;

                string fullPath = Path.Combine(GetApplicationFolder(), fileName);

                using (Stream networkStream = response.GetResponseStream())
                using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[4096];
                    long totalRead = 0;
                    int read;

                    while ((read = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, read);
                        totalRead += read;

                        if (totalSize > 0)
                        {
                            int percentage = (int)((totalRead * 100L) / totalSize);
                            this.Invoke(new UpdateProgressDelegate(UpdateDownloadProgressChanged), new object[] { percentage });
                        }
                    }
                }

                return fullPath;
            }
        }

        private void UpdateDownloadProgressChanged(int percentage)
        {
            if (percentage < 0) percentage = 0;
            if (percentage > 100) percentage = 100;
            progressUpdate.Value = percentage;
            labelUpdateStatus.Text = percentage + " %";
        }

        private void UpdateDownloadCompleted(string filePath, string error)
        {
            isDownloadingUpdate = false;

            if (error != null)
            {
                buttonUpdate.Enabled = true;
                buttonUpdate.Text = "Update available (v" + _availableUpdateVersion + ")";
                labelUpdateStatus.Text = "Download failed.";
                ShowWarning("The update download failed.\n\n" + error);
                return;
            }

            _downloadedUpdatePath = filePath;

            progressUpdate.Value = 100;
            labelUpdateStatus.Text = "Downloaded: " + filePath;

            buttonUpdate.Enabled = true;
            buttonUpdate.Text = "Install";
        }

        // Launches the native installer on the downloaded .cab, then closes the app
        // to hand off cleanly to wceload.exe without conflicting with the current process.
        private void InstallUpdate()
        {
            try
            {
                Process.Start(new ProcessStartInfo(_downloadedUpdatePath, ""));
                Application.Exit();
            }
            catch (Exception ex)
            {
                ShowWarning("The file was downloaded (" + _downloadedUpdatePath + "), but automatic installation failed.\n\n" + ex.Message + "\n\nYou can install it manually from the file explorer.");
            }
        }

        #endregion

        #region Search bar

        private void textBoxSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                RunSearch();
            }
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            RunSearch();
        }

        private void RunSearch()
        {
            string keyword = textBoxSearch.Text.Trim();
            string url;

            if (keyword.Length == 0)
            {
                url = ApiBaseUrl + "/api/applications/todaylist";
            }
            else
            {
                url = ApiBaseUrl + "/api/applications/search?q=" + EncodeUrlComponent(keyword);
            }

            LoadApplications(url);
        }

        private static string EncodeUrlComponent(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            StringBuilder sb = new StringBuilder();

            foreach (byte b in bytes)
            {
                char c = (char)b;
                bool safe = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                            c == '-' || c == '_' || c == '.' || c == '~';
                if (safe)
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('%');
                    sb.Append(((int)b).ToString("X2"));
                }
            }
            return sb.ToString();
        }

        #endregion

        private void menuItemRefresh_Click(object sender, EventArgs e)
        {
            textBoxSearch.Text = "";
            LoadApplications(ApiBaseUrl + "/api/applications/todaylist");
        }

        private void LoadApplications(string url)
        {
            if (isLoading) return;

            isLoading = true;
            labelStatus.Text = "Loading...";
            labelStatus.ForeColor = Theme.TextColorSecondary;
            panelList.Controls.Clear();

            Cursor.Current = Cursors.WaitCursor;
            Cursor.Show();

            Thread loadThread = new Thread(new ThreadStart(delegate
            {
                string jsonResponse = null;
                string error = null;

                try
                {
                    jsonResponse = GetApiData(url);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                this.Invoke(new LoadCompletedDelegate(LoadCompleted), new object[] { jsonResponse, error });
            }));

            loadThread.Start();
        }

        private void LoadCompleted(string jsonResponse, string error)
        {
            isLoading = false;

            Cursor.Current = Cursors.Default;
            Cursor.Hide();

            if (error != null)
            {
                labelStatus.Text = "Failed to load.";
                ShowWarning("Could not reach the server.\n\n" + error);
                return;
            }

            try
            {
                ApiResponse apiResponse = Converter.Deserialize<ApiResponse>(jsonResponse);

                if (!apiResponse.success)
                {
                    labelStatus.Text = "Failed to load.";
                    ShowWarning("Request failed: " + apiResponse.message);
                    return;
                }

                labelStatus.Text = apiResponse.data.Length + " application(s) found.";
                DisplayApplications(apiResponse.data);
            }
            catch (Exception ex)
            {
                labelStatus.Text = "Failed to load.";
                ShowWarning("Could not parse the server response.\n\n" + ex.Message);
            }
        }

        private void ShowWarning(string message)
        {
            MessageBox.Show(message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
        }

        private void DisplayApplications(ApplicationApi[] apps)
        {
            _lastApplications = apps;

            panelList.SuspendLayout();
            panelList.Controls.Clear();

            const int itemHeight = 54;
            const int margin = 8;

            for (int i = 0; i < apps.Length; i++)
            {
                ApplicationApi app = apps[i];
                bool cardPressed = false; // local state captured by the delegates below

                Panel card = new Panel();
                card.Left = 8;
                card.Top = i * (itemHeight + margin) + margin;
                card.Width = panelList.ClientSize.Width - 30;
                card.Height = itemHeight;
                card.BackColor = Theme.BackgroundColor;

                PaintEventHandler cardRoundedPaint = delegate(object s, PaintEventArgs pe)
                {
                    Color surfaceColor = cardPressed ? Theme.CardPressedColor : Theme.CardColor;
                    using (SolidBrush brush = new SolidBrush(surfaceColor))
                    {
                        RoundedRendering.FillRoundedRectangle(pe.Graphics, brush, 0, 0, card.Width, card.Height, 14);
                    }
                };
                card.Paint += cardRoundedPaint;

                PictureBox iconBox = new PictureBox();
                iconBox.Left = 10;
                iconBox.Top = (itemHeight - 32) / 2;
                iconBox.Width = 32;
                iconBox.Height = 32;
                iconBox.BackColor = Theme.CardColor;

                Bitmap iconBitmap = null; // filled once the download finishes, captured by the closure below

                iconBox.Paint += delegate(object s, PaintEventArgs pe)
                {
                    if (iconBitmap == null) return;

                    ImageAttributes attr = new ImageAttributes();
                    attr.SetColorKey(Color.Magenta, Color.Magenta);

                    Rectangle destRect = new Rectangle(0, 0, iconBox.Width, iconBox.Height);
                    pe.Graphics.DrawImage(iconBitmap, destRect, 0, 0, iconBitmap.Width, iconBitmap.Height, GraphicsUnit.Pixel, attr);
                };

                card.Controls.Add(iconBox);

                if (!string.IsNullOrEmpty(app.icon))
                {
                    string iconUrl = ApiBaseUrl + "/icons/" + app.icon;
                    IconLoader.Load(iconUrl, app.id, iconBox, delegate(Bitmap bmp)
                    {
                        iconBitmap = bmp;
                        iconBox.Invalidate();
                    });
                }

                Label lblName = new Label();
                lblName.Text = app.name;
                lblName.Font = Theme.FontNormalBold;
                lblName.ForeColor = Theme.TextColor;
                lblName.BackColor = Theme.CardColor;
                lblName.Left = 14 + 32 + 10;
                lblName.Top = 10;
                lblName.Width = card.Width - 100;
                lblName.Height = 20;

                Label lblSubtext = new Label();
                lblSubtext.Text = "Tap to view details";
                lblSubtext.Font = Theme.FontSmall;
                lblSubtext.ForeColor = Theme.TextColorSecondary;
                lblSubtext.BackColor = Theme.CardColor;
                lblSubtext.Left = lblName.Left;
                lblSubtext.Top = 30;
                lblSubtext.Width = lblName.Width;
                lblSubtext.Height = 16;

                Label lblChevron = new Label();
                lblChevron.Text = "›";
                lblChevron.Font = new Font("Tahoma", 14, FontStyle.Bold);
                lblChevron.ForeColor = Theme.AccentColor;
                lblChevron.BackColor = Theme.CardColor;
                lblChevron.Width = 24;
                lblChevron.Height = itemHeight - 20;
                lblChevron.Left = card.Width - 30;
                lblChevron.Top = 10;
                lblChevron.TextAlign = ContentAlignment.TopCenter;

                MouseEventHandler onPress = delegate(object s, MouseEventArgs me)
                {
                    cardPressed = true;
                    card.Invalidate();

                    lblName.BackColor = Theme.CardPressedColor;
                    lblSubtext.BackColor = Theme.CardPressedColor;
                    lblChevron.BackColor = Theme.CardPressedColor;
                };
                MouseEventHandler onRelease = delegate(object s, MouseEventArgs ev)
                {
                    cardPressed = false;
                    card.Invalidate();

                    lblName.BackColor = Theme.CardColor;
                    lblSubtext.BackColor = Theme.CardColor;
                    lblChevron.BackColor = Theme.CardColor;
                };
                EventHandler openDetails = delegate(object s, EventArgs ev)
                {
                    OpenDetailsPage(app);
                };

                card.MouseDown += onPress;
                card.MouseUp += onRelease;

                card.Click += openDetails;
                lblName.Click += openDetails;
                lblSubtext.Click += openDetails;
                lblChevron.Click += openDetails;

                card.Controls.Add(lblName);
                card.Controls.Add(lblSubtext);
                card.Controls.Add(lblChevron);
                panelList.Controls.Add(card);
            }

            panelList.ResumeLayout();
        }

        private void OpenDetailsPage(ApplicationApi app)
        {
            _currentApp = app;

            labelDetailsName.Text = app.name;
            labelDetailsName.BackColor = Theme.CardColor;
            labelDetailsId.Text = "ID: " + app.id;
            labelDetailsId.BackColor = Theme.CardColor;

            progressDownload.Visible = false;
            progressDownload.Value = 0;
            labelDownloadStatus.Visible = false;
            buttonDownload.Enabled = true;
            buttonDownload.Text = "Download";

            this.SuspendLayout();
            panelList.Visible = false;
            panelListHeader.Visible = false;
            panelSearch.Visible = false;
            panelUpdate.Visible = false;
            labelStatus.Visible = false;
            panelDetails.Visible = true;
            this.ResumeLayout();
        }

        private void buttonDownload_Click(object sender, EventArgs e)
        {
            if (isDownloading || _currentApp == null) return;

            isDownloading = true;
            buttonDownload.Enabled = false;
            buttonDownload.Text = "Downloading...";
            progressDownload.Visible = true;
            progressDownload.Value = 0;
            labelDownloadStatus.Visible = true;
            labelDownloadStatus.Text = "Starting...";

            string fileName = _currentApp.name + ".cab";
            string cabUrl = ApiBaseUrl + "/cabs/" + EncodeUrlComponent(_currentApp.name) + ".cab";

            Thread downloadThread = new Thread(new ThreadStart(delegate
            {
                string filePath = null;
                string error = null;

                try
                {
                    filePath = DownloadFile(cabUrl, fileName);
                }
                catch (Exception ex)
                {
                    error = ex.Message + cabUrl;
                }

                this.Invoke(new DownloadCompletedDelegate(DownloadCompleted), new object[] { filePath, error });
            }));

            downloadThread.Start();
        }

        private string DownloadFile(string url, string fileName)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                long totalSize = response.ContentLength;

                string fullPath = Path.Combine(GetApplicationFolder(), fileName);

                using (Stream networkStream = response.GetResponseStream())
                using (FileStream fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[4096];
                    long totalRead = 0;
                    int read;

                    while ((read = networkStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fileStream.Write(buffer, 0, read);
                        totalRead += read;

                        if (totalSize > 0)
                        {
                            int percentage = (int)((totalRead * 100L) / totalSize);
                            this.Invoke(new DownloadProgressDelegate(DownloadProgressChanged), new object[] { percentage });
                        }
                    }
                }

                return fullPath;
            }
        }

        private void DownloadProgressChanged(int percentage)
        {
            if (percentage < 0) percentage = 0;
            if (percentage > 100) percentage = 100;
            progressDownload.Value = percentage;
            labelDownloadStatus.Text = percentage + " %";
        }

        private void DownloadCompleted(string filePath, string error)
        {
            isDownloading = false;
            buttonDownload.Enabled = true;
            buttonDownload.Text = "Download";

            if (error != null)
            {
                labelDownloadStatus.Text = "Download failed.";
                ShowWarning("The download failed.\n\n" + error);
                return;
            }

            progressDownload.Value = 100;
            labelDownloadStatus.Text = "Installing...";

            LaunchInstallation(filePath);
        }

        private void LaunchInstallation(string filePath)
        {
            try
            {
                Process.Start(new ProcessStartInfo(filePath, ""));
                labelDownloadStatus.Text = "Installation started.";
            }
            catch (Exception ex)
            {
                labelDownloadStatus.Text = "Downloaded, but installation could not start.";
                ShowWarning("The file was downloaded (" + filePath + "), but automatic installation failed.\n\n" + ex.Message + "\n\nYou can install it manually from the file explorer.");
            }
        }

        private void buttonBack_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            panelDetails.Visible = false;
            panelList.Visible = true;
            panelListHeader.Visible = true;
            panelSearch.Visible = true;
            panelUpdate.Visible = !string.IsNullOrEmpty(_availableUpdateVersion) && CompareVersions(_availableUpdateVersion, GetApplicationVersion()) > 0;
            labelStatus.Visible = true;
            LayoutControls();
            this.ResumeLayout();
        }

        private string GetApiData(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private string GetApplicationVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return v.Major + "." + v.Minor + "." + v.Build;
        }

        private static string GetApplicationFolder()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().GetName().CodeBase;
            return Path.GetDirectoryName(assemblyPath);
        }
    }

    public class ApplicationApi
    {
        public string id;
        public string name;
        public string provider;
        public string icon;
    }

    public class ApiResponse
    {
        public bool success;
        public string message;
        public ApplicationApi[] data;
    }

    public class AppVersionResponse
    {
        public bool success;
        public string version;
        public string publishedAt;
    }
}