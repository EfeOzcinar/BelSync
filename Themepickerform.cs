using System;
using System.Drawing;
using System.Windows.Forms;

namespace BelSync
{
    public class ThemePickerForm : Form
    {
        private TextBox txtPrimary, txtTopBar, txtAccent;
        private Panel pnlPrimary, pnlTopBar, pnlAccent;
        private Button btnApply, btnCancel, btnReset;
        private Label lblPreview;

        // Preset palettes: (Name, Primary/FormBg, TopBar, Accent)
        private static readonly (string Name, string FormBg, string TopBar, string Accent)[] Presets = new[]
        {
            ("🌸 Pink",       "#FFF5F8", "#C23064", "#DC5082"),
            ("🌊 Ocean",      "#F0F7FF", "#1A4A8A", "#2E86DE"),
            ("🌊 Blu",        "#F0F7FF", "#00acfe", "#00acfe"),
            ("🌿 Forest",     "#F0FFF4", "#1A5C3A", "#27AE60"),
            ("🌅 Sunset",     "#FFF8F0", "#C0440A", "#E67E22"),
            ("🌌 Midnight",   "#F5F0FF", "#2C1A5C", "#7B2FBE"),
            ("🖤 Slate",      "#F8F9FA", "#2C3E50", "#3498DB"),
            ("🌺 Rose Gold",  "#FFF9F6", "#A0522D", "#E8956D"),
            ("❄️  Arctic",     "#F0FAFF", "#0C4A6E", "#0EA5E9"),
        };

        public Color SelectedFormBg { get; private set; }
        public Color SelectedTopBar { get; private set; }
        public Color SelectedAccent { get; private set; }

        public ThemePickerForm()
        {
            SelectedFormBg = Theme.IsDark ? Color.FromArgb(32, 18, 28) : Color.FromArgb(255, 245, 248);
            SelectedTopBar = Color.FromArgb(194, 48, 100);
            SelectedAccent = Color.FromArgb(220, 80, 130);
            Build();
        }

        private void Build()
        {
            this.Text = "🎨 Theme Picker";
            this.Size = new Size(520, 460);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Theme.FormBg;
            this.Font = new Font("Segoe UI", 9f);

            // Title
            var lblTitle = new Label
            {
                Text = "Select a preset or enter custom hex colors",
                Location = new Point(16, 14),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextPrimary
            };

            // ── Preset swatches ────────────────────────────────────────
            var lblPresets = new Label
            {
                Text = "PRESETS",
                Location = new Point(16, 40),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary
            };

            int swatchX = 16, swatchY = 58;
            foreach (var preset in Presets)
            {
                var swatch = new Panel
                {
                    Size = new Size(56, 56),
                    Location = new Point(swatchX, swatchY),
                    BackColor = ColorTranslator.FromHtml(preset.TopBar),
                    Cursor = Cursors.Hand,
                    Tag = preset
                };
                // Name label inside swatch
                var swatchLbl = new Label
                {
                    Text = preset.Name.Length > 8 ? preset.Name.Substring(0, 8) : preset.Name,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                    Location = new Point(2, 36),
                    Size = new Size(52, 18),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                swatch.Controls.Add(swatchLbl);

                // Accent strip at bottom of swatch
                var strip = new Panel
                {
                    Size = new Size(56, 8),
                    Location = new Point(0, 48),
                    BackColor = ColorTranslator.FromHtml(preset.Accent)
                };
                swatch.Controls.Add(strip);

                var capturedSwatch = swatch;
                swatch.Click += (s, e) => ApplyPreset(((string Name, string FormBg, string TopBar, string Accent))capturedSwatch.Tag);
                swatchLbl.Click += (s, e) => ApplyPreset(((string Name, string FormBg, string TopBar, string Accent))capturedSwatch.Tag);
                strip.Click += (s, e) => ApplyPreset(((string Name, string FormBg, string TopBar, string Accent))capturedSwatch.Tag);

                this.Controls.Add(swatch);
                swatchX += 62;
                if (swatchX > 16 + 62 * 4) { swatchX = 16; swatchY += 62; }
            }

            // ── Custom hex inputs ──────────────────────────────────────
            int hexY = 210;
            var lblCustom = new Label
            {
                Text = "CUSTOM HEX COLORS",
                Location = new Point(16, hexY),
                AutoSize = true,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary
            };

            // Form Background
            var lbl1 = FieldLabel("Form Background", new Point(16, hexY + 18));
            pnlPrimary = ColorBox(new Point(16, hexY + 36));
            txtPrimary = HexBox(new Point(44, hexY + 36), ColorToHex(SelectedFormBg));
            txtPrimary.TextChanged += (s, e) => UpdatePreview(pnlPrimary, txtPrimary.Text);

            // Top Bar
            var lbl2 = FieldLabel("Top Bar / Header", new Point(200, hexY + 18));
            pnlTopBar = ColorBox(new Point(200, hexY + 36));
            txtTopBar = HexBox(new Point(228, hexY + 36), ColorToHex(SelectedTopBar));
            txtTopBar.TextChanged += (s, e) => UpdatePreview(pnlTopBar, txtTopBar.Text);

            // Accent
            var lbl3 = FieldLabel("Accent / Buttons", new Point(384, hexY + 18));
            pnlAccent = ColorBox(new Point(384, hexY + 36));
            txtAccent = HexBox(new Point(412, hexY + 36), ColorToHex(SelectedAccent));
            txtAccent.TextChanged += (s, e) => UpdatePreview(pnlAccent, txtAccent.Text);

            // ── Preview bar ────────────────────────────────────────────
            lblPreview = new Label
            {
                Location = new Point(16, hexY + 76),
                Size = new Size(472, 40),
                Text = "  BelSync — Preview",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = SelectedTopBar,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // ── Buttons ────────────────────────────────────────────────
            btnReset = MBtn("↺ Reset", new Point(16, hexY + 130), 90, Theme.AccentGray);
            btnApply = MBtn("✓ Apply", new Point(310, hexY + 130), 90, Theme.AccentGreen);
            btnCancel = MBtn("✕ Cancel", new Point(410, hexY + 130), 90, Theme.AccentRed);

            btnReset.Click += (s, e) => { txtPrimary.Text = "#FFF5F8"; txtTopBar.Text = "#C23064"; txtAccent.Text = "#DC5082"; };
            btnApply.Click += (s, e) => { SaveAndApply(); this.DialogResult = DialogResult.OK; this.Close(); };
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] {
                lblTitle, lblPresets, lblCustom,
                lbl1, pnlPrimary, txtPrimary,
                lbl2, pnlTopBar,  txtTopBar,
                lbl3, pnlAccent,  txtAccent,
                lblPreview, btnReset, btnApply, btnCancel
            });
        }

        private void ApplyPreset((string Name, string FormBg, string TopBar, string Accent) preset)
        {
            txtPrimary.Text = preset.FormBg;
            txtTopBar.Text = preset.TopBar;
            txtAccent.Text = preset.Accent;
            UpdatePreview(pnlPrimary, preset.FormBg);
            UpdatePreview(pnlTopBar, preset.TopBar);
            UpdatePreview(pnlAccent, preset.Accent);
        }

        private void UpdatePreview(Panel box, string hex)
        {
            try
            {
                var color = ColorTranslator.FromHtml(hex.StartsWith("#") ? hex : "#" + hex);
                box.BackColor = color;
                if (box == pnlTopBar) { lblPreview.BackColor = color; }
            }
            catch { }
        }

        private void SaveAndApply()
        {
            try { SelectedFormBg = ColorTranslator.FromHtml(txtPrimary.Text.StartsWith("#") ? txtPrimary.Text : "#" + txtPrimary.Text); } catch { }
            try { SelectedTopBar = ColorTranslator.FromHtml(txtTopBar.Text.StartsWith("#") ? txtTopBar.Text : "#" + txtTopBar.Text); } catch { }
            try { SelectedAccent = ColorTranslator.FromHtml(txtAccent.Text.StartsWith("#") ? txtAccent.Text : "#" + txtAccent.Text); } catch { }
        }

        private string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private Label FieldLabel(string text, Point loc) => new Label
        {
            Text = text,
            Location = loc,
            AutoSize = true,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Theme.TextSecondary
        };

        private Panel ColorBox(Point loc) => new Panel
        {
            Location = loc,
            Size = new Size(24, 24),
            BorderStyle = BorderStyle.FixedSingle
        };

        private TextBox HexBox(Point loc, string text) => new TextBox
        {
            Location = loc,
            Size = new Size(120, 24),
            Text = text,
            Font = new Font("Consolas", 9f),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Theme.InputBg,
            ForeColor = Theme.TextPrimary,
            MaxLength = 7
        };

        private Button MBtn(string text, Point loc, int width, Color color)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(width, 30),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }
}