using System.Drawing;

namespace API_diag
{
    // The Compact Framework doesn't expose GraphicsPath: rounded-corner rectangles
    // are recreated by hand from solid rectangles + full circles (used as corner fillers).
    public static class RoundedRendering
    {
        public static void FillRoundedRectangle(Graphics g, Brush brush, int x, int y, int width, int height, int radius)
        {
            if (radius > width / 2) radius = width / 2;
            if (radius > height / 2) radius = height / 2;

            int diameter = radius * 2;

            // Horizontal + vertical center bands: form a cross covering everything
            // except the 4 corners.
            g.FillRectangle(brush, x + radius, y, width - diameter, height);
            g.FillRectangle(brush, x, y + radius, width, height - diameter);

            // The 4 corners, drawn as full circles (overlap with the bands above is harmless).
            g.FillEllipse(brush, x, y, diameter, diameter);
            g.FillEllipse(brush, x + width - diameter, y, diameter, diameter);
            g.FillEllipse(brush, x, y + height - diameter, diameter, diameter);
            g.FillEllipse(brush, x + width - diameter, y + height - diameter, diameter, diameter);
        }
    }
}