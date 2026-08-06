using System.Drawing;

namespace API_diag
{
    public static class Theme
    {
        // Dark mode: near-black background, slightly lighter "elevated" surfaces,
        // vivid purple accent for a modern look on a WQVGA screen.
        public static readonly Color BackgroundColor = Color.FromArgb(16, 16, 18);
        public static readonly Color CardColor = Color.FromArgb(28, 28, 32);
        public static readonly Color CardPressedColor = Color.FromArgb(42, 42, 48);
        public static readonly Color BorderColor = Color.FromArgb(50, 50, 56);
        public static readonly Color AccentColor = Color.FromArgb(124, 92, 255);
        public static readonly Color AccentColorDark = Color.FromArgb(94, 68, 200);
        public static readonly Color TextColor = Color.FromArgb(240, 240, 242);
        public static readonly Color TextColorSecondary = Color.FromArgb(148, 148, 155);
        public static readonly Color TextColorLight = Color.White;
        public static readonly Color HealthColorUnknown = Color.FromArgb(90, 90, 96);
        public static readonly Color HealthColorOk = Color.FromArgb(52, 199, 89);
        public static readonly Color HealthColorKo = Color.FromArgb(255, 69, 58);

        public static readonly Font FontHeader = new Font("Tahoma", 11, FontStyle.Bold);
        public static readonly Font FontSubtitle = new Font("Tahoma", 8, FontStyle.Regular);
        public static readonly Font FontDetailsTitle = new Font("Tahoma", 13, FontStyle.Bold);
        public static readonly Font FontNormal = new Font("Tahoma", 10, FontStyle.Regular);
        public static readonly Font FontNormalBold = new Font("Tahoma", 10, FontStyle.Bold);
        public static readonly Font FontSmall = new Font("Tahoma", 8, FontStyle.Regular);
        public static readonly Font FontButton = new Font("Tahoma", 10, FontStyle.Bold);
        public static readonly Font FontLogo = new Font("Tahoma", 12, FontStyle.Bold);
    }
}