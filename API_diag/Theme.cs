using System.Drawing;

namespace API_diag
{
    public static class Theme
    {
        // Mode sombre : fond quasi-noir, surfaces "élevées" légèrement plus claires,
        // accent violet vif pour un rendu moderne sur écran WQVGA.
        public static readonly Color CouleurFond = Color.FromArgb(16, 16, 18);
        public static readonly Color CouleurCarte = Color.FromArgb(28, 28, 32);
        public static readonly Color CouleurCartePressee = Color.FromArgb(42, 42, 48);
        public static readonly Color CouleurBordure = Color.FromArgb(50, 50, 56);
        public static readonly Color CouleurAccent = Color.FromArgb(124, 92, 255);
        public static readonly Color CouleurAccentFonce = Color.FromArgb(94, 68, 200);
        public static readonly Color CouleurTexte = Color.FromArgb(240, 240, 242);
        public static readonly Color CouleurTexteSecondaire = Color.FromArgb(148, 148, 155);
        public static readonly Color CouleurTexteClair = Color.White;
        public static readonly Color CouleurSanteInconnue = Color.FromArgb(90, 90, 96);
        public static readonly Color CouleurSanteOk = Color.FromArgb(52, 199, 89);
        public static readonly Color CouleurSanteKo = Color.FromArgb(255, 69, 58);

        public static readonly Font PoliceEntete = new Font("Tahoma", 11, FontStyle.Bold);
        public static readonly Font PoliceSousTitre = new Font("Tahoma", 8, FontStyle.Regular);
        public static readonly Font PoliceTitreDetail = new Font("Tahoma", 13, FontStyle.Bold);
        public static readonly Font PoliceNormale = new Font("Tahoma", 10, FontStyle.Regular);
        public static readonly Font PoliceNormaleGrasse = new Font("Tahoma", 10, FontStyle.Bold);
        public static readonly Font PolicePetite = new Font("Tahoma", 8, FontStyle.Regular);
        public static readonly Font PoliceBouton = new Font("Tahoma", 10, FontStyle.Bold);
        public static readonly Font PoliceLogo = new Font("Tahoma", 12, FontStyle.Bold);
    }
}