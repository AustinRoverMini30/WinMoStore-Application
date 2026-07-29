using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

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
        public static readonly Color CouleurSanteInconnue = Color.FromArgb(160, 160, 160);
        public static readonly Color CouleurSanteOk = Color.FromArgb(76, 175, 80);
        public static readonly Color CouleurSanteKo = Color.FromArgb(220, 53, 69);

        public static readonly Font PoliceEntete = new Font("Tahoma", 10, FontStyle.Bold);
        public static readonly Font PoliceTitreDetail = new Font("Tahoma", 13, FontStyle.Bold);
        public static readonly Font PoliceNormale = new Font("Tahoma", 10, FontStyle.Regular);
        public static readonly Font PoliceNormaleGrasse = new Font("Tahoma", 10, FontStyle.Bold);
        public static readonly Font PolicePetite = new Font("Tahoma", 8, FontStyle.Regular);
        public static readonly Font PoliceBouton = new Font("Tahoma", 10, FontStyle.Bold);
    }
}
