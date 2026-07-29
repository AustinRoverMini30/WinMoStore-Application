using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
    public static class Theme
    {
        public static readonly Color CouleurFond = Color.FromArgb(232, 232, 232);
        public static readonly Color CouleurCarte = Color.White;
        public static readonly Color CouleurCartePressee = Color.FromArgb(225, 238, 250);
        public static readonly Color CouleurBordure = Color.FromArgb(205, 205, 205);
        public static readonly Color CouleurAccent = Color.FromArgb(0, 122, 204);
        public static readonly Color CouleurAccentFonce = Color.FromArgb(0, 92, 160);
        public static readonly Color CouleurTexte = Color.FromArgb(40, 40, 40);
        public static readonly Color CouleurTexteSecondaire = Color.FromArgb(130, 130, 130);
        public static readonly Color CouleurTexteClair = Color.White;

        public static readonly Font PoliceEntete = new Font("Tahoma", 10, FontStyle.Bold);
        public static readonly Font PoliceTitreDetail = new Font("Tahoma", 13, FontStyle.Bold);
        public static readonly Font PoliceNormale = new Font("Tahoma", 10, FontStyle.Regular);
        public static readonly Font PoliceNormaleGrasse = new Font("Tahoma", 10, FontStyle.Bold);
        public static readonly Font PolicePetite = new Font("Tahoma", 8, FontStyle.Regular);
        public static readonly Font PoliceBouton = new Font("Tahoma", 10, FontStyle.Bold);
    }

    public partial class WinMoStore : Form
    {
        private const string ApiBaseUrl = "http://192.168.1.33:3000";

        // --- Page liste ---
        private Panel panelHeaderListe;
        private Label labelHeaderListe;
        private Label label1;
        private Panel panel1;
        private MainMenu mainMenu1;
        private MenuItem menuItem1;
        private MenuItem menuItem2;

        // --- Page détails ---
        private Panel panelDetails;
        private Panel panelHeaderDetails;
        private Label labelHeaderDetails;
        private Label labelNomDetails;
        private Label labelIdDetails;
        private Button buttonTelecharger;
        private Button buttonRetour;
        private ProgressBar progressTelechargement;
        private Label labelStatutTelechargement;

        private const int hauteurEntete = 28;
        private bool chargementEnCours = false;
        private bool telechargementEnCours = false;

        private ApplicationApi _appCourante;

        private delegate void ChargementTermineDelegate(string jsonResponse, string erreur);
        private delegate void ProgressionTelechargementDelegate(int pourcentage);
        private delegate void TelechargementTermineDelegate(string cheminFichier, string erreur);

        public WinMoStore()
        {
            InitialiserComposants();
        }

        private void InitialiserComposants()
        {
            this.Text = "WinMo Store";
            this.ClientSize = new Size(240, 268);
            this.BackColor = Theme.CouleurFond;

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
            panelHeaderListe.Left = 0;
            panelHeaderListe.Top = 0;
            panelHeaderListe.Width = this.ClientSize.Width;
            panelHeaderListe.Height = hauteurEntete;
            panelHeaderListe.BackColor = Theme.CouleurAccent;

            labelHeaderListe = new Label();
            labelHeaderListe.Text = "Applications disponibles";
            labelHeaderListe.ForeColor = Theme.CouleurTexteClair;
            labelHeaderListe.Font = Theme.PoliceEntete;
            labelHeaderListe.Left = 8;
            labelHeaderListe.Top = 5;
            labelHeaderListe.Width = panelHeaderListe.Width - 16;
            labelHeaderListe.Height = hauteurEntete - 5;
            panelHeaderListe.Controls.Add(labelHeaderListe);

            label1 = new Label();
            label1.Left = 8;
            label1.Top = panelHeaderListe.Top + panelHeaderListe.Height + 4;
            label1.Width = this.ClientSize.Width - 16;
            label1.Height = 18;
            label1.ForeColor = Theme.CouleurTexteSecondaire;
            label1.Font = Theme.PolicePetite;
            label1.Text = "Appuyez sur Actualiser pour charger la liste.";

            panel1 = new Panel();
            panel1.Left = 0;
            panel1.Top = label1.Top + label1.Height + 2;
            panel1.Width = this.ClientSize.Width;
            panel1.Height = this.ClientSize.Height - panel1.Top;
            panel1.BackColor = Theme.CouleurFond;
            panel1.AutoScroll = true;

            // ===================== PAGE DÉTAILS =====================

            panelDetails = new Panel();
            panelDetails.Left = 0;
            panelDetails.Top = 0;
            panelDetails.Width = this.ClientSize.Width;
            panelDetails.Height = this.ClientSize.Height;
            panelDetails.BackColor = Theme.CouleurFond;
            panelDetails.Visible = false;

            panelHeaderDetails = new Panel();
            panelHeaderDetails.Left = 0;
            panelHeaderDetails.Top = 0;
            panelHeaderDetails.Width = panelDetails.Width;
            panelHeaderDetails.Height = hauteurEntete;
            panelHeaderDetails.BackColor = Theme.CouleurAccent;

            labelHeaderDetails = new Label();
            labelHeaderDetails.Text = "Détails de l'application";
            labelHeaderDetails.ForeColor = Theme.CouleurTexteClair;
            labelHeaderDetails.Font = Theme.PoliceEntete;
            labelHeaderDetails.Left = 8;
            labelHeaderDetails.Top = 5;
            labelHeaderDetails.Width = panelHeaderDetails.Width - 16;
            labelHeaderDetails.Height = hauteurEntete - 5;
            panelHeaderDetails.Controls.Add(labelHeaderDetails);

            Panel carteInfo = new Panel();
            carteInfo.Left = 10;
            carteInfo.Top = panelHeaderDetails.Height + 12;
            carteInfo.Width = panelDetails.Width - 20;
            carteInfo.Height = 90;
            carteInfo.BackColor = Theme.CouleurCarte;
            carteInfo.Paint += new PaintEventHandler(CarteAvecBordure_Paint);

            labelNomDetails = new Label();
            labelNomDetails.Left = 10;
            labelNomDetails.Top = 10;
            labelNomDetails.Width = carteInfo.Width - 20;
            labelNomDetails.Height = 40;
            labelNomDetails.Font = Theme.PoliceTitreDetail;
            labelNomDetails.ForeColor = Theme.CouleurTexte;

            labelIdDetails = new Label();
            labelIdDetails.Left = 10;
            labelIdDetails.Top = labelNomDetails.Top + labelNomDetails.Height;
            labelIdDetails.Width = carteInfo.Width - 20;
            labelIdDetails.Height = 30;
            labelIdDetails.Font = Theme.PolicePetite;
            labelIdDetails.ForeColor = Theme.CouleurTexteSecondaire;

            carteInfo.Controls.Add(labelNomDetails);
            carteInfo.Controls.Add(labelIdDetails);

            buttonTelecharger = new Button();
            buttonTelecharger.Text = "Télécharger";
            buttonTelecharger.Font = Theme.PoliceBouton;
            buttonTelecharger.ForeColor = Theme.CouleurTexteClair;
            buttonTelecharger.BackColor = Theme.CouleurAccent;
            buttonTelecharger.Left = 10;
            buttonTelecharger.Top = carteInfo.Top + carteInfo.Height + 16;
            buttonTelecharger.Width = panelDetails.Width - 20;
            buttonTelecharger.Height = 42;
            buttonTelecharger.Click += new EventHandler(buttonTelecharger_Click);

            progressTelechargement = new ProgressBar();
            progressTelechargement.Left = 10;
            progressTelechargement.Top = buttonTelecharger.Top + buttonTelecharger.Height + 8;
            progressTelechargement.Width = panelDetails.Width - 20;
            progressTelechargement.Height = 18;
            progressTelechargement.Minimum = 0;
            progressTelechargement.Maximum = 100;
            progressTelechargement.Visible = false;

            labelStatutTelechargement = new Label();
            labelStatutTelechargement.Left = 10;
            labelStatutTelechargement.Top = progressTelechargement.Top + progressTelechargement.Height + 4;
            labelStatutTelechargement.Width = panelDetails.Width - 20;
            labelStatutTelechargement.Height = 16;
            labelStatutTelechargement.Font = Theme.PolicePetite;
            labelStatutTelechargement.ForeColor = Theme.CouleurTexteSecondaire;
            labelStatutTelechargement.Visible = false;

            buttonRetour = new Button();
            buttonRetour.Text = "‹ Retour";
            buttonRetour.Font = Theme.PoliceNormale;
            buttonRetour.ForeColor = Theme.CouleurTexte;
            buttonRetour.Left = 10;
            buttonRetour.Top = labelStatutTelechargement.Top + labelStatutTelechargement.Height + 8;
            buttonRetour.Width = panelDetails.Width - 20;
            buttonRetour.Height = 36;
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
            this.Controls.Add(panelHeaderListe);
        }

        private void CarteAvecBordure_Paint(object sender, PaintEventArgs pe)
        {
            Panel p = (Panel)sender;
            using (Pen pen = new Pen(Theme.CouleurBordure))
            {
                pe.Graphics.DrawRectangle(pen, 0, 0, p.Width - 1, p.Height - 1);
            }
        }

        private void menuItem1_Click(object sender, EventArgs e)
        {
            if (chargementEnCours) return;

            chargementEnCours = true;
            label1.Text = "Chargement...";
            label1.ForeColor = Theme.CouleurTexteSecondaire;
            panel1.Controls.Clear();

            string apiUrl = ApiBaseUrl + "/api/applications";

            Thread threadChargement = new Thread(new ThreadStart(delegate
            {
                string jsonResponse = null;
                string erreur = null;

                try
                {
                    jsonResponse = GetApiData(apiUrl);
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
            panel1.SuspendLayout();
            panel1.Controls.Clear();

            const int hauteurItem = 52;
            const int marge = 6;

            for (int i = 0; i < apps.Length; i++)
            {
                ApplicationApi app = apps[i];

                Panel carte = new Panel();
                carte.Left = 8;
                carte.Top = i * (hauteurItem + marge) + marge;
                carte.Width = panel1.ClientSize.Width - 16;
                carte.Height = hauteurItem;
                carte.BackColor = Theme.CouleurCarte;
                carte.Paint += new PaintEventHandler(CarteAvecBordure_Paint);

                Label lblNom = new Label();
                lblNom.Text = app.name;
                lblNom.Font = Theme.PoliceNormaleGrasse;
                lblNom.ForeColor = Theme.CouleurTexte;
                lblNom.Left = 10;
                lblNom.Top = 8;
                lblNom.Width = carte.Width - 40;
                lblNom.Height = 20;

                Label lblSousTexte = new Label();
                lblSousTexte.Text = "Toucher pour voir les détails";
                lblSousTexte.Font = Theme.PolicePetite;
                lblSousTexte.ForeColor = Theme.CouleurTexteSecondaire;
                lblSousTexte.Left = 10;
                lblSousTexte.Top = 28;
                lblSousTexte.Width = carte.Width - 40;
                lblSousTexte.Height = 16;

                Label lblChevron = new Label();
                lblChevron.Text = "›";
                lblChevron.Font = new Font("Tahoma", 14, FontStyle.Bold);
                lblChevron.ForeColor = Theme.CouleurAccent;
                lblChevron.Width = 24;
                lblChevron.Height = hauteurItem - 20;
                lblChevron.Left = carte.Width - 28;
                lblChevron.Top = 10;
                lblChevron.TextAlign = ContentAlignment.TopCenter;

                EventHandler ouvrirDetails = delegate(object s, EventArgs ev)
                {
                    OuvrirPageDetails(app);
                };

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
            labelIdDetails.Text = "ID : " + app.id;

            progressTelechargement.Visible = false;
            progressTelechargement.Value = 0;
            labelStatutTelechargement.Visible = false;
            buttonTelecharger.Enabled = true;
            buttonTelecharger.Text = "Télécharger";

            this.SuspendLayout();
            panel1.Visible = false;
            panelHeaderListe.Visible = false;
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
            string urlCab = ApiBaseUrl + "/cabs/" + _appCourante.name + ".cab";

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
                    erreur = ex.Message;
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

                string dossierDestination = "\\My Storage\\My Documents";
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

        // Exécuté sur le thread UI via Invoke une fois le téléchargement terminé.
        // Lance maintenant automatiquement l'installation du .cab.
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

        // Lance l'installeur natif Windows Mobile associé aux fichiers .cab (wceload.exe).
        // Une fois lancé, l'appli n'a plus la main sur la suite : c'est l'OS qui affiche
        // sa propre fenêtre d'installation, et l'appli ne reçoit aucune notification de fin.
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
    }

    public class ApiResponse
    {
        public bool success;
        public string message;
        public ApplicationApi[] data;
    }
}