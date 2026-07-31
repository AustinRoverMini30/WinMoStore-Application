using System.Drawing;

namespace API_diag
{
    // Le Compact Framework n'expose pas GraphicsPath : les rectangles à coins arrondis
    // sont recomposés à la main à partir de rectangles pleins + quarts de cercle (FillPie).
    public static class RenduArrondi
    {
        public static void RemplirRectangleArrondi(Graphics g, Brush brush, int x, int y, int largeur, int hauteur, int rayon)
        {
            if (rayon > largeur / 2) rayon = largeur / 2;
            if (rayon > hauteur / 2) rayon = hauteur / 2;

            int diametre = rayon * 2;

            // Bandes centrales horizontale + verticale : forment une croix qui couvre
            // tout sauf les 4 coins.
            g.FillRectangle(brush, x + rayon, y, largeur - diametre, hauteur);
            g.FillRectangle(brush, x, y + rayon, largeur, hauteur - diametre);

            // Les 4 coins, dessinés comme des quarts de cercle.
            g.FillEllipse(brush, x, y, diametre, diametre);
            g.FillEllipse(brush, x + largeur - diametre, y, diametre, diametre);
            g.FillEllipse(brush, x, y + hauteur - diametre, diametre, diametre);
            g.FillEllipse(brush, x + largeur - diametre, y + hauteur - diametre, diametre, diametre);
        }
    }
}