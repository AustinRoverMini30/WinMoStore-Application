using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;
using CodeBetter.Json;

namespace API_diag
{
    public partial class WinMoStore : Form
    {
        //private const string ApiBaseUrl = "http://192.168.1.102:3000";
        private const string ApiBaseUrl = "http://vps-f5acc160.vps.ovh.net:3000";

        // --- Page liste ---
        private Panel panelHeaderListe;
        private Panel logoMark;
        private Label labelLogo;
        private Label labelHeaderListe;
        private Label labelSousTitreListe;
        private Panel pastilleSante;
        private Panel panelRecherche;
        private Panel panelPilleRecherche;
        private TextBox textBoxRecherche;
        private Button buttonRechercher;
        private Label label1;
        private Panel panel1;
        private MainMenu mainMenu1;
        private MenuItem menuItem1;
        private MenuItem menuItem2;

        // --- Page détails ---
        private Panel panelDetails;
        private Panel panelHeaderDetails;
        private Label labelHeaderDetails;
        private Panel carteInfo;
        private Label labelNomDetails;
        private Label labelIdDetails;
        private Button buttonTelecharger;
        private Button buttonRetour;
        private ProgressBar progressTelechargement;
        private Label labelStatutTelechargement;

        private const int hauteurEntete = 54;
        private bool chargementEnCours = false;
        private bool telechargementEnCours = false;
        private bool verificationSanteEnCours = false;

        private ApplicationApi _appCourante;
        private ApplicationApi[] _dernieresApplications;
        private Color _couleurSante = Theme.CouleurSanteInconnue;
        private System.Windows.Forms.Timer minuterieSante;

        private delegate void ChargementTermineDelegate(string jsonResponse, string erreur);
        private delegate void ProgressionTelechargementDelegate(int pourcentage);
        private delegate void TelechargementTermineDelegate(string cheminFichier, string erreur);
        private delegate void SanteVerifieeDelegate(bool enLigne);

        public WinMoStore()
        {
            CreerControles();
            PositionnerControles();
            this.Resize += new EventHandler(WinMoStore_Resize);
            InitialiserSurveillanceSante();
        }

        private void WinMoStore_Resize(object sender, EventArgs e)
        {
            PositionnerControles();

            if (_dernieresApplications != null)
            {
                AfficherApplications(_dernieresApplications);
            }
        }

        private void CreerControles()
        {
            this.Text = "WinMo Store";
            this.BackColor = Theme.CouleurFond;
            this.WindowState = FormWindowState.Maximized;

            menuItem1 = new MenuItem();
            menuItem1.Text = "Actualiser";
            menuItem1.Click += new EventHandler(menuItem1_Click);

            menuItem2 = new MenuItem();
            menuItem2.Text = "Quitter";
            menuItem2.Click += new EventHandler(menuItem2_Click);

            mainMenu1 = new MainMenu();
            mainMenu1.MenuItems.Add(menuItem1);
            mainMenu1.MenuItems.Add(menuItem2);
            this.Menu = mainMenu1;

            // ===================== PAGE LISTE =====================

            panelHeaderListe = new Panel();
            panelHeaderListe.BackColor = Theme.CouleurFond; // plus de bandeau plein : header à plat, façon appli moderne

            // "Logo" fait maison : carré arrondi accent avec une initiale, en l'absence d'icône réelle.
            logoMark = new Panel();
            logoMark.BackColor = Theme.CouleurFond;
            logoMark.Paint += new PaintEventHandler(LogoMark_Paint);

            labelLogo = new Label();
            labelLogo.Text = "W";
            labelLogo.Font = Theme.PoliceLogo;
            labelLogo.ForeColor = Theme.CouleurTexteClair;
            labelLogo.BackColor = Theme.CouleurAccent; // même couleur que le fond dessiné, pas de bord visible
            labelLogo.TextAlign = ContentAlignment.TopCenter;
            logoMark.Controls.Add(labelLogo);

            labelHeaderListe = new Label();
            labelHeaderListe.Text = "WinMo Store";
            labelHeaderListe.ForeColor = Theme.CouleurTexte;
            labelHeaderListe.Font = Theme.PoliceEntete;

            labelSousTitreListe = new Label();
            labelSousTitreListe.Text = "Applications disponibles";
            labelSousTitreListe.ForeColor = Theme.CouleurTexteSecondaire;
            labelSousTitreListe.Font = Theme.PoliceSousTitre;

            pastilleSante = new Panel();
            pastilleSante.BackColor = Theme.CouleurFond;
            pastilleSante.Paint += new PaintEventHandler(Pastille_Paint);

            panelHeaderListe.Controls.Add(logoMark);
            panelHeaderListe.Controls.Add(labelHeaderListe);
            panelHeaderListe.Controls.Add(labelSousTitreListe);
            panelHeaderListe.Controls.Add(pastilleSante);

            // --- Barre de recherche en forme de pilule ---
            panelRecherche = new Panel();
            panelRecherche.BackColor = Theme.CouleurFond;

            panelPilleRecherche = new Panel();
            panelPilleRecherche.BackColor = Theme.CouleurFond; // blend avec le fond pour les coins découpés
            panelPilleRecherche.Paint += new PaintEventHandler(PilleRecherche_Paint);

            textBoxRecherche = new TextBox();
            textBoxRecherche.Font = Theme.PoliceNormale;
            textBoxRecherche.ForeColor = Theme.CouleurTexte;
            textBoxRecherche.BackColor = Theme.CouleurCarte;
            try { textBoxRecherche.BorderStyle = BorderStyle.None; }
            catch { /* ignoré si non supporté par la ROM */ }
            textBoxRecherche.KeyDown += new KeyEventHandler(textBoxRecherche_KeyDown);
            panelPilleRecherche.Controls.Add(textBoxRecherche);

            buttonRechercher = new Button();
            buttonRechercher.Text = "→";
            buttonRechercher.Font = Theme.PoliceBouton;
            buttonRechercher.ForeColor = Theme.CouleurTexteClair;
            buttonRechercher.BackColor = Theme.CouleurAccent;
            buttonRechercher.Click += new EventHandler(buttonRechercher_Click);

            panelRecherche.Controls.Add(panelPilleRecherche);
            panelRecherche.Controls.Add(buttonRechercher);

            label1 = new Label();
            label1.ForeColor = Theme.CouleurTexteSecondaire;
            label1.Font = Theme.PolicePetite;
            label1.Text = "Appuyez sur Actualiser pour charger la liste.";

            panel1 = new Panel();
            panel1.BackColor = Theme.CouleurFond;
            panel1.AutoScroll = true;

            // ===================== PAGE DÉTAILS =====================

            panelDetails = new Panel();
            panelDetails.BackColor = Theme.CouleurFond;
            panelDetails.Visible = false;
            panelDetails.AutoScroll = true;

            panelHeaderDetails = new Panel();
            panelHeaderDetails.BackColor = Theme.CouleurFond;

            labelHeaderDetails = new Label();
            labelHeaderDetails.Text = "Détails de l'application";
            labelHeaderDetails.ForeColor = Theme.CouleurTexte;
            labelHeaderDetails.Font = Theme.PoliceEntete;
            panelHeaderDetails.Controls.Add(labelHeaderDetails);

            carteInfo = new Panel();
            carteInfo.BackColor = Theme.CouleurFond;
            carteInfo.Paint += new PaintEventHandler(CarteInfo_Paint);

            labelNomDetails = new Label();
            labelNomDetails.Font = Theme.PoliceTitreDetail;
            labelNomDetails.ForeColor = Theme.CouleurTexte;

            labelIdDetails = new Label();
            labelIdDetails.Font = Theme.PolicePetite;
            labelIdDetails.ForeColor = Theme.CouleurTexteSecondaire;

            carteInfo.Controls.Add(labelNomDetails);
            carteInfo.Controls.Add(labelIdDetails);

            buttonTelecharger = new Button();
            buttonTelecharger.Text = "Télécharger";
            buttonTelecharger.Font = Theme.PoliceBouton;
            buttonTelecharger.ForeColor = Theme.CouleurTexteClair;
            buttonTelecharger.BackColor = Theme.CouleurAccent;
            buttonTelecharger.Click += new EventHandler(buttonTelecharger_Click);

            progressTelechargement = new ProgressBar();
            progressTelechargement.Minimum = 0;
            progressTelechargement.Maximum = 100;
            progressTelechargement.Visible = false;

            labelStatutTelechargement = new Label();
            labelStatutTelechargement.Font = Theme.PolicePetite;
            labelStatutTelechargement.ForeColor = Theme.CouleurTexteSecondaire;
            labelStatutTelechargement.Visible = false;

            buttonRetour = new Button();
            buttonRetour.Text = "‹ Retour";
            buttonRetour.Font = Theme.PoliceNormale;
            buttonRetour.ForeColor = Theme.CouleurTexte;
            buttonRetour.BackColor = Theme.CouleurCarte;
            buttonRetour.Click += new EventHandler(buttonRetour_Click);

            panelDetails.Controls.Add(buttonRetour);
            panelDetails.Controls.Add(labelStatutTelechargement);
            panelDetails.Controls.Add(progressTelechargement);
            panelDetails.Controls.Add(buttonTelecharger);
            panelDetails.Controls.Add(carteInfo);
            panelDetails.Controls.Add(panelHeaderDetails);

            this.Controls.Add(panelDetails);
            this.Controls.Add(panel1);
            this.Controls.Add(label1);
            this.Controls.Add(panelRecherche);
            this.Controls.Add(panelHeaderListe);
        }

        private void PositionnerControles()
        {
            int largeur = this.ClientSize.Width;
            int hauteur = this.ClientSize.Height;

            // ===== PAGE LISTE =====
            panelHeaderListe.Left = 0;
            panelHeaderListe.Top = 0;
            panelHeaderListe.Width = largeur;
            panelHeaderListe.Height = hauteurEntete;

            logoMark.Left = 10;
            logoMark.Top = (hauteurEntete - 32) / 2;
            logoMark.Width = 32;
            logoMark.Height = 32;

            labelLogo.Left = 6;
            labelLogo.Top = 6;
            labelLogo.Width = 20;
            labelLogo.Height = 20;

            pastilleSante.Width = 12;
            pastilleSante.Height = 12;
            pastilleSante.Left = largeur - 22;
            pastilleSante.Top = (hauteurEntete - 12) / 2;

            labelHeaderListe.Left = logoMark.Left + logoMark.Width + 10;
            labelHeaderListe.Top = 6;
            labelHeaderListe.Width = pastilleSante.Left - labelHeaderListe.Left - 6;
            labelHeaderListe.Height = 20;

            labelSousTitreListe.Left = labelHeaderListe.Left;
            labelSousTitreListe.Top = labelHeaderListe.Top + labelHeaderListe.Height;
            labelSousTitreListe.Width = labelHeaderListe.Width;
            labelSousTitreListe.Height = 16;

            panelRecherche.Left = 0;
            panelRecherche.Top = panelHeaderListe.Top + panelHeaderListe.Height;
            panelRecherche.Width = largeur;
            panelRecherche.Height = 44;

            buttonRechercher.Width = 44;
            buttonRechercher.Height = 32;
            buttonRechercher.Left = panelRecherche.Width - buttonRechercher.Width - 8;
            buttonRechercher.Top = (panelRecherche.Height - buttonRechercher.Height) / 2;

            panelPilleRecherche.Left = 8;
            panelPilleRecherche.Top = (panelRecherche.Height - 32) / 2;
            panelPilleRecherche.Width = buttonRechercher.Left - 8 - 8;
            panelPilleRecherche.Height = 32;

            textBoxRecherche.Left = 14;
            textBoxRecherche.Top = (panelPilleRecherche.Height - 20) / 2;
            textBoxRecherche.Width = panelPilleRecherche.Width - 24;
            textBoxRecherche.Height = 20;

            label1.Left = 8;
            label1.Top = panelRecherche.Top + panelRecherche.Height + 4;
            label1.Width = largeur - 16;
            label1.Height = 18;

            panel1.Left = 0;
            panel1.Top = label1.Top + label1.Height + 2;
            panel1.Width = largeur;
            panel1.Height = hauteur - panel1.Top;

            // ===== PAGE DÉTAILS =====
            panelDetails.Left = 0;
            panelDetails.Top = 0;
            panelDetails.Width = largeur;
            panelDetails.Height = hauteur;

            panelHeaderDetails.Left = 0;
            panelHeaderDetails.Top = 0;
            panelHeaderDetails.Width = panelDetails.Width;
            panelHeaderDetails.Height = hauteurEntete - 20;

            labelHeaderDetails.Left = 8;
            labelHeaderDetails.Top = 10;
            labelHeaderDetails.Width = panelHeaderDetails.Width - 16;
            labelHeaderDetails.Height = 22;

            carteInfo.Left = 10;
            carteInfo.Top = panelHeaderDetails.Height + 12;
            carteInfo.Width = panelDetails.Width - 20;
            carteInfo.Height = 90;

            labelNomDetails.Left = 14;
            labelNomDetails.Top = 12;
            labelNomDetails.Width = carteInfo.Width - 28;
            labelNomDetails.Height = 40;

            labelIdDetails.Left = 14;
            labelIdDetails.Top = labelNomDetails.Top + labelNomDetails.Height;
            labelIdDetails.Width = carteInfo.Width - 28;
            labelIdDetails.Height = 30;

            buttonTelecharger.Left = 10;
            buttonTelecharger.Top = carteInfo.Top + carteInfo.Height + 16;
            buttonTelecharger.Width = panelDetails.Width - 20;
            buttonTelecharger.Height = 42;

            progressTelechargement.Left = 10;
            progressTelechargement.Top = buttonTelecharger.Top + buttonTelecharger.Height + 8;
            progressTelechargement.Width = panelDetails.Width - 20;
            progressTelechargement.Height = 18;

            labelStatutTelechargement.Left = 10;
            labelStatutTelechargement.Top = progressTelechargement.Top + progressTelechargement.Height + 4;
            labelStatutTelechargement.Width = panelDetails.Width - 20;
            labelStatutTelechargement.Height = 16;

            buttonRetour.Left = 10;
            buttonRetour.Top = labelStatutTelechargement.Top + labelStatutTelechargement.Height + 8;
            buttonRetour.Width = panelDetails.Width - 20;
            buttonRetour.Height = 36;
        }

        // Fond arrondi accent du logo maison ("W").
        private void LogoMark_Paint(object sender, PaintEventArgs pe)
        {
            Panel p = (Panel)sender;
            using (SolidBrush brush = new SolidBrush(Theme.CouleurAccent))
            {
                RenduArrondi.RemplirRectangleArrondi(pe.Graphics, brush, 0, 0, p.Width, p.Height, 10);
            }
        }

        // Pilule de recherche : coins totalement arrondis (rayon = moitié de la hauteur).
        private void PilleRecherche_Paint(object sender, PaintEventArgs pe)
        {
            Panel p = (Panel)sender;
            using (SolidBrush brush = new SolidBrush(Theme.CouleurCarte))
            {
                RenduArrondi.RemplirRectangleArrondi(pe.Graphics, brush, 0, 0, p.Width, p.Height, p.Height / 2);
            }
        }

        // Carte "surface élevée" à coins arrondis, utilisée sur la page détails.
        private void CarteInfo_Paint(object sender, PaintEventArgs pe)
        {
            Panel p = (Panel)sender;
            using (SolidBrush brush = new SolidBrush(Theme.CouleurCarte))
            {
                RenduArrondi.RemplirRectangleArrondi(pe.Graphics, brush, 0, 0, p.Width, p.Height, 14);
            }
        }

        private void Pastille_Paint(object sender, PaintEventArgs pe)
        {
            Panel p = (Panel)sender;
            using (SolidBrush brush = new SolidBrush(_couleurSante))
            {
                pe.Graphics.FillEllipse(brush, 0, 0, p.Width - 1, p.Height - 1);
            }
        }

        #region Surveillance de /api/health

        private void InitialiserSurveillanceSante()
        {
            minuterieSante = new System.Windows.Forms.Timer();
            minuterieSante.Interval = 15000;
            minuterieSante.Tick += new EventHandler(minuterieSante_Tick);
            minuterieSante.Enabled = true;

            VerifierSante();
        }

        private void minuterieSante_Tick(object sender, EventArgs e)
        {
            VerifierSante();
        }

        private void VerifierSante()
        {
            if (verificationSanteEnCours) return;
            verificationSanteEnCours = true;

            Thread threadSante = new Thread(new ThreadStart(delegate
            {
                bool enLigne = false;
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ApiBaseUrl + "/api/health");
                    request.Method = "GET";
                    request.Timeout = 5000;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        enLigne = (response.StatusCode == HttpStatusCode.OK);
                    }
                }
                catch
                {
                    enLigne = false;
                }

                this.Invoke(new SanteVerifieeDelegate(SanteVerifiee), new object[] { enLigne });
            }));

            threadSante.IsBackground = true;
            threadSante.Start();
        }

        private void SanteVerifiee(bool enLigne)
        {
            verificationSanteEnCours = false;
            _couleurSante = enLigne ? Theme.CouleurSanteOk : Theme.CouleurSanteKo;
            pastilleSante.Invalidate();
        }

        #endregion

        #region Barre de recherche

        private void textBoxRecherche_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                LancerRecherche();
            }
        }

        private void buttonRechercher_Click(object sender, EventArgs e)
        {
            LancerRecherche();
        }

        private void LancerRecherche()
        {
            string motCle = textBoxRecherche.Text.Trim();
            string url;

            if (motCle.Length == 0)
            {
                url = ApiBaseUrl + "/api/applications/todaylist";
            }
            else
            {
                url = ApiBaseUrl + "/api/applications/search?q=" + EncoderComposantUrl(motCle);
            }

            ChargerApplications(url);
        }

        private static string EncoderComposantUrl(string valeur)
        {
            if (string.IsNullOrEmpty(valeur)) return string.Empty;

            byte[] octets = Encoding.UTF8.GetBytes(valeur);
            StringBuilder sb = new StringBuilder();

            foreach (byte b in octets)
            {
                char c = (char)b;
                bool sur = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                           c == '-' || c == '_' || c == '.' || c == '~';
                if (sur)
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

        private void menuItem1_Click(object sender, EventArgs e)
        {
            textBoxRecherche.Text = "";
            ChargerApplications(ApiBaseUrl + "/api/applications/todaylist");
        }

        private void ChargerApplications(string url)
        {
            if (chargementEnCours) return;

            chargementEnCours = true;
            label1.Text = "Chargement...";
            label1.ForeColor = Theme.CouleurTexteSecondaire;
            panel1.Controls.Clear();

            Cursor.Current = Cursors.WaitCursor;
            Cursor.Show();

            Thread threadChargement = new Thread(new ThreadStart(delegate
            {
                string jsonResponse = null;
                string erreur = null;

                try
                {
                    jsonResponse = GetApiData(url);
                }
                catch (Exception ex)
                {
                    erreur = ex.Message;
                }

                this.Invoke(new ChargementTermineDelegate(ChargementTermine), new object[] { jsonResponse, erreur });
            }));

            threadChargement.Start();
        }

        private void ChargementTermine(string jsonResponse, string erreur)
        {
            chargementEnCours = false;

            Cursor.Current = Cursors.Default;
            Cursor.Hide();

            if (erreur != null)
            {
                label1.Text = "Échec du chargement.";
                AfficherAvertissement("Impossible de contacter le serveur.\n\n" + erreur);
                return;
            }

            try
            {
                ApiResponse apiResponse = Converter.Deserialize<ApiResponse>(jsonResponse);

                if (!apiResponse.success)
                {
                    label1.Text = "Échec du chargement.";
                    AfficherAvertissement("Échec de la requête : " + apiResponse.message);
                    return;
                }

                label1.Text = apiResponse.data.Length + " application(s) trouvée(s).";
                AfficherApplications(apiResponse.data);
            }
            catch (Exception ex)
            {
                label1.Text = "Échec du chargement.";
                AfficherAvertissement("Impossible de lire la réponse du serveur.\n\n" + ex.Message);
            }
        }

        private void AfficherAvertissement(string message)
        {
            MessageBox.Show(message, "Attention", MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
        }

        private void AfficherApplications(ApplicationApi[] apps)
        {
            _dernieresApplications = apps;

            panel1.SuspendLayout();
            panel1.Controls.Clear();

            const int hauteurItem = 54;
            const int marge = 8;

            for (int i = 0; i < apps.Length; i++)
            {
                ApplicationApi app = apps[i];
                bool carteEnfoncee = false; // état local capturé par les délégués ci-dessous

                Panel carte = new Panel();
                carte.Left = 8;
                carte.Top = i * (hauteurItem + marge) + marge;
                carte.Width = panel1.ClientSize.Width - 30;
                carte.Height = hauteurItem;
                carte.BackColor = Theme.CouleurFond; // blend avec le fond pour les coins découpés

                PaintEventHandler carteArrondiePaint = delegate(object s, PaintEventArgs pe)
                {
                    Color couleurSurface = carteEnfoncee ? Theme.CouleurCartePressee : Theme.CouleurCarte;
                    using (SolidBrush brush = new SolidBrush(couleurSurface))
                    {
                        RenduArrondi.RemplirRectangleArrondi(pe.Graphics, brush, 0, 0, carte.Width, carte.Height, 14);
                    }
                };
                carte.Paint += carteArrondiePaint;

                Label lblNom = new Label();
                lblNom.Text = app.name;
                lblNom.Font = Theme.PoliceNormaleGrasse;
                lblNom.ForeColor = Theme.CouleurTexte;
                lblNom.BackColor = Theme.CouleurCarte;
                lblNom.Left = 14;
                lblNom.Top = 10;
                lblNom.Width = carte.Width - 44;
                lblNom.Height = 20;

                Label lblSousTexte = new Label();
                lblSousTexte.Text = "Toucher pour voir les détails";
                lblSousTexte.Font = Theme.PolicePetite;
                lblSousTexte.ForeColor = Theme.CouleurTexteSecondaire;
                lblSousTexte.BackColor = Theme.CouleurCarte;
                lblSousTexte.Left = 14;
                lblSousTexte.Top = 30;
                lblSousTexte.Width = carte.Width - 44;
                lblSousTexte.Height = 16;

                Label lblChevron = new Label();
                lblChevron.Text = "›";
                lblChevron.Font = new Font("Tahoma", 14, FontStyle.Bold);
                lblChevron.ForeColor = Theme.CouleurAccent;
                lblChevron.BackColor = Theme.CouleurCarte;
                lblChevron.Width = 24;
                lblChevron.Height = hauteurItem - 20;
                lblChevron.Left = carte.Width - 30;
                lblChevron.Top = 10;
                lblChevron.TextAlign = ContentAlignment.TopCenter;

                MouseEventHandler surAppui = delegate(object s, MouseEventArgs me)
                {
                    carteEnfoncee = true;
                    carte.Invalidate();

                    lblNom.BackColor = Theme.CouleurCartePressee;
                    lblSousTexte.BackColor = Theme.CouleurCartePressee;
                    lblChevron.BackColor = Theme.CouleurCartePressee;
                };
                MouseEventHandler surRelache = delegate(object s, MouseEventArgs ev)
                {
                    carteEnfoncee = false;
                    carte.Invalidate();

                    lblNom.BackColor = Theme.CouleurCarte;
                    lblSousTexte.BackColor = Theme.CouleurCarte;
                    lblChevron.BackColor = Theme.CouleurCarte;
                };
                EventHandler ouvrirDetails = delegate(object s, EventArgs ev)
                {
                    OuvrirPageDetails(app);
                };

                carte.MouseDown += surAppui;
                carte.MouseUp += surRelache;

                carte.Click += ouvrirDetails;
                lblNom.Click += ouvrirDetails;
                lblSousTexte.Click += ouvrirDetails;
                lblChevron.Click += ouvrirDetails;

                carte.Controls.Add(lblNom);
                carte.Controls.Add(lblSousTexte);
                carte.Controls.Add(lblChevron);
                panel1.Controls.Add(carte);
            }

            panel1.ResumeLayout();
        }

        private void OuvrirPageDetails(ApplicationApi app)
        {
            _appCourante = app;

            labelNomDetails.Text = app.name;
            labelNomDetails.BackColor = Theme.CouleurCarte;
            labelIdDetails.Text = "ID : " + app.id;
            labelIdDetails.BackColor = Theme.CouleurCarte;

            progressTelechargement.Visible = false;
            progressTelechargement.Value = 0;
            labelStatutTelechargement.Visible = false;
            buttonTelecharger.Enabled = true;
            buttonTelecharger.Text = "Télécharger";

            this.SuspendLayout();
            panel1.Visible = false;
            panelHeaderListe.Visible = false;
            panelRecherche.Visible = false;
            label1.Visible = false;
            panelDetails.Visible = true;
            this.ResumeLayout();
        }

        private void buttonTelecharger_Click(object sender, EventArgs e)
        {
            if (telechargementEnCours || _appCourante == null) return;

            telechargementEnCours = true;
            buttonTelecharger.Enabled = false;
            buttonTelecharger.Text = "Téléchargement...";
            progressTelechargement.Visible = true;
            progressTelechargement.Value = 0;
            labelStatutTelechargement.Visible = true;
            labelStatutTelechargement.Text = "Démarrage...";

            string nomFichier = _appCourante.name + ".cab";
            string urlCab = ApiBaseUrl + "/cabs/" + EncoderComposantUrl(_appCourante.name) + ".cab";

            Thread threadTelechargement = new Thread(new ThreadStart(delegate
            {
                string cheminFichier = null;
                string erreur = null;

                try
                {
                    cheminFichier = TelechargerFichier(urlCab, nomFichier);
                }
                catch (Exception ex)
                {
                    erreur = ex.Message + urlCab;
                }

                this.Invoke(new TelechargementTermineDelegate(TelechargementTermine), new object[] { cheminFichier, erreur });
            }));

            threadTelechargement.Start();
        }

        private string TelechargerFichier(string url, string nomFichier)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                long tailleTotale = response.ContentLength;

                string dossierDestination = "\\My Documents";
                if (!Directory.Exists(dossierDestination))
                {
                    dossierDestination = "\\";
                }
                string cheminComplet = Path.Combine(dossierDestination, nomFichier);

                using (Stream fluxReseau = response.GetResponseStream())
                using (FileStream fluxFichier = new FileStream(cheminComplet, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[4096];
                    long totalLu = 0;
                    int lu;

                    while ((lu = fluxReseau.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fluxFichier.Write(buffer, 0, lu);
                        totalLu += lu;

                        if (tailleTotale > 0)
                        {
                            int pourcentage = (int)((totalLu * 100L) / tailleTotale);
                            this.Invoke(new ProgressionTelechargementDelegate(MettreAJourProgression), new object[] { pourcentage });
                        }
                    }
                }

                return cheminComplet;
            }
        }

        private void MettreAJourProgression(int pourcentage)
        {
            if (pourcentage < 0) pourcentage = 0;
            if (pourcentage > 100) pourcentage = 100;
            progressTelechargement.Value = pourcentage;
            labelStatutTelechargement.Text = pourcentage + " %";
        }

        private void TelechargementTermine(string cheminFichier, string erreur)
        {
            telechargementEnCours = false;
            buttonTelecharger.Enabled = true;
            buttonTelecharger.Text = "Télécharger";

            if (erreur != null)
            {
                labelStatutTelechargement.Text = "Échec du téléchargement.";
                AfficherAvertissement("Le téléchargement a échoué.\n\n" + erreur);
                return;
            }

            progressTelechargement.Value = 100;
            labelStatutTelechargement.Text = "Installation en cours...";

            LancerInstallation(cheminFichier);
        }

        private void LancerInstallation(string cheminFichier)
        {
            try
            {
                Process.Start(new ProcessStartInfo(cheminFichier, ""));
                labelStatutTelechargement.Text = "Installation lancée.";
            }
            catch (Exception ex)
            {
                labelStatutTelechargement.Text = "Téléchargé, mais l'installation n'a pas pu démarrer.";
                AfficherAvertissement("Le fichier a été téléchargé (" + cheminFichier + "), mais son installation automatique a échoué.\n\n" + ex.Message + "\n\nVous pouvez l'installer manuellement depuis l'explorateur de fichiers.");
            }
        }

        private void buttonRetour_Click(object sender, EventArgs e)
        {
            this.SuspendLayout();
            panelDetails.Visible = false;
            panel1.Visible = true;
            panelHeaderListe.Visible = true;
            panelRecherche.Visible = true;
            label1.Visible = true;
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

        private void menuItem2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }

    public class ApplicationApi
    {
        public string id;
        public string name;
        public string provider;
    }

    public class ApiResponse
    {
        public bool success;
        public string message;
        public ApplicationApi[] data;
    }
}