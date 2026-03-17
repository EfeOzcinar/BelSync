using System.Drawing;

namespace BelSync
{
    public enum AppTheme { Light, Dark }

    public static class Theme
    {
        public static AppTheme Current = AppTheme.Light;
        public static bool IsDark => Current == AppTheme.Dark;

        // ── Backgrounds ────────────────────────────────────────────────
        public static Color FormBg => IsDark ? Color.FromArgb(18, 18, 28) : Color.FromArgb(248, 249, 252);
        public static Color TopBarBg => IsDark ? Color.FromArgb(26, 27, 46) : Color.FromArgb(30, 42, 68);
        public static Color CardBg => IsDark ? Color.FromArgb(28, 29, 50) : Color.White;
        public static Color InputBg => IsDark ? Color.FromArgb(36, 37, 60) : Color.FromArgb(252, 253, 255);
        public static Color SummaryBg => IsDark ? Color.FromArgb(28, 29, 50) : Color.FromArgb(250, 252, 255);
        public static Color RollbackBg => IsDark ? Color.FromArgb(50, 22, 28) : Color.FromArgb(255, 243, 245);
        public static Color GridEven => IsDark ? Color.FromArgb(28, 29, 50) : Color.White;
        public static Color GridOdd => IsDark ? Color.FromArgb(33, 34, 58) : Color.FromArgb(249, 251, 255);
        public static Color GridSelect => IsDark ? Color.FromArgb(52, 100, 200) : Color.FromArgb(66, 133, 244);
        public static Color BorderColor => IsDark ? Color.FromArgb(50, 52, 80) : Color.FromArgb(226, 230, 240);
        public static Color HeaderBg => IsDark ? Color.FromArgb(22, 23, 42) : Color.FromArgb(44, 62, 80);
        public static Color TabBg => IsDark ? Color.FromArgb(28, 29, 50) : Color.White;

        // ── Text ───────────────────────────────────────────────────────
        public static Color TextPrimary => IsDark ? Color.FromArgb(225, 226, 245) : Color.FromArgb(33, 37, 50);
        public static Color TextSecondary => IsDark ? Color.FromArgb(130, 135, 170) : Color.FromArgb(108, 117, 140);
        public static Color TextHeader => Color.White;
        public static Color TextRollback => IsDark ? Color.FromArgb(255, 140, 150) : Color.FromArgb(180, 30, 50);

        // ── Accents ────────────────────────────────────────────────────
        public static Color AccentBlue => Color.FromArgb(66, 133, 244);
        public static Color AccentGreen => Color.FromArgb(52, 199, 120);
        public static Color AccentOrange => Color.FromArgb(255, 171, 64);
        public static Color AccentRed => Color.FromArgb(239, 83, 80);
        public static Color AccentGray => IsDark ? Color.FromArgb(90, 95, 130) : Color.FromArgb(150, 160, 180);

        // ── Status row colors ──────────────────────────────────────────
        public static Color RowInserted => AccentGreen;
        public static Color RowUpdated => AccentOrange;
        public static Color RowSkipped => AccentGray;

        // ── Button hover ───────────────────────────────────────────────
        public static Color BtnBlueHover => Color.FromArgb(50, 115, 220);
        public static Color BtnGreenHover => Color.FromArgb(40, 175, 100);
        public static Color BtnRedHover => Color.FromArgb(210, 60, 60);
        public static Color BtnGrayHover => Color.FromArgb(120, 128, 160);
        public static Color TopBtnHover => Color.FromArgb(55, 70, 110);
    }
}