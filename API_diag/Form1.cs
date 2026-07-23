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
using CodeBetter.Json;

namespace API_diag
{
    public partial class WinMoStore : Form
    {
        public WinMoStore()
        {
            InitializeComponent();
        }

        private void menuItem1_Click(object sender, EventArgs e)
        {
            string apiUrl = "http://192.168.1.116:3000/api/applications";
            string jsonResponse = GetApiData(apiUrl);

            if (jsonResponse.StartsWith("Erreur"))
            {
                label1.Text = jsonResponse;
                return;
            }

            try
            {
                ApiResponse apiResponse = Converter.Deserialize<ApiResponse>(jsonResponse);

                if (!apiResponse.success)
                {
                    label1.Text = "Échec API : " + apiResponse.message;
                    return;
                }

                AfficherApplications(apiResponse.data);
            }
            catch (Exception ex)
            {
                label1.Text = "Erreur de parsing JSON : " + ex.Message;
            }
        }

        // Crée un panel + label pour chaque application, empilés verticalement dans panel1
        private void AfficherApplications(ApplicationApi[] apps)
        {
            panel1.SuspendLayout();

            // Vide le contenu précédent (utile si on rafraîchit la liste plusieurs fois)
            panel1.Controls.Clear();

            const int hauteurItem = 40;   // hauteur de chaque sous-panel
            const int marge = 2;          // espace vertical entre chaque item

            for (int i = 0; i < apps.Length; i++)
            {
                ApplicationApi app = apps[i];

                Panel itemPanel = new Panel();
                itemPanel.Name = "panelApp" + i;
                itemPanel.Left = 0;
                itemPanel.Top = i * (hauteurItem + marge);
                itemPanel.Width = panel1.ClientSize.Width;
                itemPanel.Height = hauteurItem;
                itemPanel.BackColor = Color.Blue;

                Label lbl = new Label();
                lbl.Name = "labelApp" + i;
                lbl.Text = app.name;
                lbl.Left = 5;
                lbl.Top = 5;
                lbl.Width = itemPanel.Width - 10;
                lbl.Height = hauteurItem - 10;
                lbl.ForeColor = Color.White;

                itemPanel.Controls.Add(lbl);
                panel1.Controls.Add(itemPanel);
            }

            panel1.ResumeLayout();
        }

        private string GetApiData(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                    {
                        string result = reader.ReadToEnd();
                        return result;
                    }
                }
            }
            catch (WebException webEx)
            {
                return "Erreur : " + webEx.Message;
            }
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            this.Close();
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