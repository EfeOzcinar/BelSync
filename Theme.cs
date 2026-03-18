using System.Drawing;

namespace BelSync
{
    public enum AppTheme { Light, Dark }

    public static class Theme
    {
        public static AppTheme Current = AppTheme.Light;
        public static bool IsDark => Current == AppTheme.Dark;

        // Custom color overrides — set by ThemePickerForm
        public static Color? CustomFormBg = null;
        public static Color? CustomTopBar = null;
        public static Color? CustomAccent = null;

        public static void ApplyCustom(Color formBg, Color topBar, Color accent)
        {
            CustomFormBg = formBg;
            CustomTopBar = topBar;
            CustomAccent = accent;
        }

        public static void ClearCustom()
        {
            CustomFormBg = CustomTopBar = CustomAccent = null;
        }

        // ── Backgrounds ────────────────────────────────────────────────
        public static Color FormBg => CustomFormBg.HasValue ? CustomFormBg.Value : (IsDark ? Color.FromArgb(32, 18, 28) : Color.FromArgb(255, 245, 248));
        public static Color TopBarBg => CustomTopBar.HasValue ? CustomTopBar.Value : (IsDark ? Color.FromArgb(60, 20, 45) : Color.FromArgb(194, 48, 100));
        public static Color CardBg => IsDark ? Color.FromArgb(50, 24, 40) : Color.White;
        public static Color InputBg => IsDark ? Color.FromArgb(60, 30, 50) : Color.FromArgb(255, 250, 252);
        public static Color SummaryBg => IsDark ? Color.FromArgb(50, 24, 40) : Color.FromArgb(255, 245, 250);
        public static Color RollbackBg => IsDark ? Color.FromArgb(70, 20, 35) : Color.FromArgb(255, 235, 242);
        public static Color GridEven => IsDark ? Color.FromArgb(50, 24, 40) : Color.White;
        public static Color GridOdd => IsDark ? Color.FromArgb(58, 28, 46) : Color.FromArgb(255, 248, 252);
        public static Color GridSelect => CustomAccent.HasValue ? CustomAccent.Value : (IsDark ? Color.FromArgb(180, 60, 110) : Color.FromArgb(220, 80, 130));
        public static Color BorderColor => IsDark ? Color.FromArgb(100, 40, 70) : Color.FromArgb(248, 200, 218);
        public static Color HeaderBg => CustomTopBar.HasValue ? CustomTopBar.Value : (IsDark ? Color.FromArgb(45, 15, 35) : Color.FromArgb(194, 48, 100));
        public static Color TabBg => IsDark ? Color.FromArgb(50, 24, 40) : Color.White;

        // ── Text ───────────────────────────────────────────────────────
        public static Color TextPrimary => IsDark ? Color.FromArgb(255, 220, 235) : Color.FromArgb(80, 20, 50);
        public static Color TextSecondary => IsDark ? Color.FromArgb(200, 140, 170) : Color.FromArgb(180, 80, 120);
        public static Color TextHeader => Color.White;
        public static Color TextRollback => IsDark ? Color.FromArgb(255, 150, 180) : Color.FromArgb(180, 30, 80);

        // ── Accents ────────────────────────────────────────────────────
        public static Color AccentBlue => CustomAccent.HasValue ? CustomAccent.Value : Color.FromArgb(220, 80, 130);
        public static Color AccentGreen => Color.FromArgb(52, 199, 120);  // keep green for inserted
        public static Color AccentOrange => Color.FromArgb(255, 171, 64);
        public static Color AccentRed => Color.FromArgb(239, 83, 80);
        public static Color AccentGray => IsDark ? Color.FromArgb(120, 70, 95) : Color.FromArgb(180, 120, 150);

        // ── Status row colors ──────────────────────────────────────────
        public static Color RowInserted => AccentGreen;
        public static Color RowUpdated => AccentOrange;
        public static Color RowSkipped => AccentGray;

        // ── Button hover ───────────────────────────────────────────────
        public static Color BtnBlueHover => Color.FromArgb(190, 55, 105);
        public static Color BtnGreenHover => Color.FromArgb(40, 175, 100);
        public static Color BtnRedHover => Color.FromArgb(210, 60, 60);
        public static Color BtnGrayHover => Color.FromArgb(150, 90, 120);
        public static Color TopBtnHover => Color.FromArgb(220, 70, 120);
    }
}