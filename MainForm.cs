using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace BelSync
{
    public class MainForm : Form
    {
        // Controls
        private Panel pnlTop, pnlBody, pnlSummary, pnlRollback, pnlGridHeader, pnlGrid;
        private Label lblTitle, lblSchema, lblTable, lblKeyCol, lblStatus;
        private Label lblTotalK, lblTotalV, lblInsK, lblInsV, lblSkpK, lblSkpV;
        private Label lblRollbackInfo, lblRowCount, lblGridTitle;
        private ComboBox cboSchema, cboTable, cboKeyCol, cboLang;
        private TabControl tabJson;
        private TabPage tabFile, tabPaste;
        private TextBox txtPath, txtPaste, txtSearch, txtWebConfigPath;
        private Button btnBrowse, btnPreview, btnSync, btnClear, btnRollback;
        private Button btnTheme, btnSettings, btnSavePreset, btnLoadPreset;
        private DataGridView dgv;
        private ProgressBar pgBar;
        private List<(string Key, string Value, RowStatus Status)> _allRows = new List<(string, string, RowStatus)>();

        // State
        private AppSettings _cfg;
        private long _oidFrom, _oidTo;
        private string _rbSchema, _rbTable;
        private List<(string Key, string Value)> _pairs;

        public MainForm()
        {
            _cfg = AppSettings.Load();
            if (Enum.TryParse(_cfg.Language, out AppLanguage lg)) Lang.Current = lg;
            Theme.Current = _cfg.Theme == "Dark" ? AppTheme.Dark : AppTheme.Light;
            OracleHelper.Configure(_cfg);
            this.SuspendLayout();
            Build();
            this.ResumeLayout(false);
            this.PerformLayout();
            ApplyTheme();
            LoadSchemasAsync();
        }

        private void Build()
        {
            this.Text = "BelSync";
            this.Size = new Size(1100, 800);
            this.MinimumSize = new Size(960, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9f);
            this.AutoScaleMode = AutoScaleMode.Font;

            // ── TOP BAR ───────────────────────────────────────────────
            pnlTop = new Panel { Dock = DockStyle.Top, Height = 52 };

            lblTitle = new Label
            {
                Text = "BelSync",
                Font = new Font("Segoe UI", 15f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 13)
            };

            // Subtle version label
            var lblVer = new Label
            {
                Text = "v2.0",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(120, 140, 180),
                AutoSize = true,
                Location = new Point(105, 22)
            };

            cboLang = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(560, 14),
                Size = new Size(115, 24),
                Font = new Font("Segoe UI", 8.5f)
            };
            cboLang.Items.AddRange(new object[] { "🌐 English", "🌐 Türkçe", "🌐 العربية" });
            cboLang.SelectedIndex = (int)Lang.Current;
            cboLang.SelectedIndexChanged += (s, e) => {
                Lang.Current = (AppLanguage)cboLang.SelectedIndex;
                _cfg.Language = Lang.Current.ToString(); _cfg.Save(); RefreshText();
            };

            btnTheme = TopBtn("", new Point(686, 11), 130);
            btnSettings = TopBtn("⚙  " + Lang.Get("settings"), new Point(826, 11), 130);
            btnTheme.Click += (s, e) => { Theme.Current = Theme.IsDark ? AppTheme.Light : AppTheme.Dark; _cfg.Theme = Theme.IsDark ? "Dark" : "Light"; _cfg.Save(); ApplyTheme(); };
            btnSettings.Click += (s, e) => OpenSettings();

            pnlTop.Controls.AddRange(new Control[] { lblTitle, lblVer, cboLang, btnTheme, btnSettings });

            // ── STATUS BAR ────────────────────────────────────────────
            var pnlStatus = new Panel { Dock = DockStyle.Bottom, Height = 26 };
            lblStatus = new Label { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8.5f), Padding = new Padding(12, 5, 0, 0), Text = "Ready" };
            pnlStatus.Controls.Add(lblStatus);

            // ── BODY ──────────────────────────────────────────────────
            pnlBody = new Panel { Dock = DockStyle.Fill, Padding = new Padding(14, 10, 14, 6) };

            // ── CARD 1: Selections ────────────────────────────────────
            var card1 = Card(0, 68);

            lblSchema = FieldLbl("SCHEMA", new Point(14, 8));
            cboSchema = Cbo(new Point(14, 26), 200);
            cboSchema.SelectedIndexChanged += CboSchema_Changed;

            lblTable = FieldLbl("TABLE", new Point(228, 8));
            cboTable = Cbo(new Point(228, 26), 270);
            cboTable.Enabled = false;
            cboTable.SelectedIndexChanged += CboTable_Changed;

            lblKeyCol = FieldLbl("KEY COLUMN", new Point(512, 8));
            cboKeyCol = Cbo(new Point(512, 26), 200);
            cboKeyCol.Enabled = false;

            btnSavePreset = Btn("💾  " + Lang.Get("savePreset"), new Point(726, 22), 150, Theme.AccentBlue);
            btnLoadPreset = Btn("📁  " + Lang.Get("loadPreset"), new Point(884, 22), 150, Theme.AccentGray);
            btnSavePreset.Click += BtnSavePreset_Click;
            btnLoadPreset.Click += BtnLoadPreset_Click;

            card1.Controls.AddRange(new Control[] {
                lblSchema, cboSchema, lblTable, cboTable,
                lblKeyCol, cboKeyCol, btnSavePreset, btnLoadPreset
            });

            // ── CARD 2: JSON Input ────────────────────────────────────
            var card2 = Card(78, 148);
            var lblJson = FieldLbl("JSON DATA", new Point(14, 8));

            tabJson = new TabControl { Location = new Point(8, 26), Size = new Size(1040, 110), Font = new Font("Segoe UI", 9f) };

            tabFile = new TabPage(Lang.Get("uploadFile"));
            txtPath = new TextBox { Location = new Point(10, 20), Size = new Size(800, 26), ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
            btnBrowse = Btn("📂  " + Lang.Get("browse"), new Point(820, 18), 110, Theme.AccentBlue);
            btnBrowse.Click += BtnBrowse_Click;
            tabFile.Controls.AddRange(new Control[] { txtPath, btnBrowse });

            tabPaste = new TabPage(Lang.Get("pasteJson"));
            txtPaste = new TextBox { Location = new Point(6, 6), Size = new Size(1026, 68), Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9f), BorderStyle = BorderStyle.None };
            tabPaste.Controls.Add(txtPaste);

            // WebConfig tab
            var tabWebConfig = new TabPage("🔧 Web.config");
            txtWebConfigPath = new TextBox { Location = new Point(10, 20), Size = new Size(800, 26), ReadOnly = true, BorderStyle = BorderStyle.FixedSingle };
            var btnBrowseWebConfig = Btn("📂  Browse...", new Point(820, 18), 110, Theme.AccentBlue);
            btnBrowseWebConfig.Click += BtnBrowseWebConfig_Click;
            var lblWebConfigNote = new Label
            {
                Text = "Reads <appSettings> keys from Web.config — inserts missing keys only.",
                Location = new Point(10, 48),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.TextSecondary
            };
            tabWebConfig.Controls.AddRange(new Control[] { txtWebConfigPath, btnBrowseWebConfig, lblWebConfigNote });

            tabJson.TabPages.AddRange(new TabPage[] { tabFile, tabPaste, tabWebConfig });
            card2.Controls.AddRange(new Control[] { lblJson, tabJson });

            // ── ACTION ROW ────────────────────────────────────────────
            var pnlAct = new Panel { Location = new Point(0, 236), Size = new Size(1068, 46), BackColor = Color.Transparent, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            btnPreview = Btn("🔍  " + Lang.Get("preview"), new Point(0, 5), 120, Theme.AccentBlue);
            btnSync = Btn("💾  " + Lang.Get("sync"), new Point(128, 5), 140, Theme.AccentGreen);
            btnClear = Btn("🗑  " + Lang.Get("clear"), new Point(276, 5), 90, Theme.AccentGray);
            btnRollback = Btn("↩  " + Lang.Get("rollback"), new Point(374, 5), 120, Theme.AccentRed);
            btnSync.Enabled = false;
            btnRollback.Enabled = false;
            btnRollback.Visible = true;
            btnPreview.Click += BtnPreview_Click;
            btnSync.Click += BtnSync_Click;
            btnClear.Click += BtnClear_Click;
            btnRollback.Click += BtnRollback_Click;

            // Progress bar sits BELOW the buttons — never overlaps them
            pgBar = new ProgressBar { Location = new Point(0, 38), Size = new Size(1068, 5), Style = ProgressBarStyle.Marquee, Visible = false, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            pnlAct.Controls.AddRange(new Control[] { btnPreview, btnSync, btnClear, btnRollback, pgBar });

            // ── SUMMARY CARD ──────────────────────────────────────────
            pnlSummary = Card(276, 52);
            pnlSummary.Visible = false;

            lblTotalK = SumKey("TOTAL", new Point(16, 6));
            lblTotalV = SumVal("0", new Point(16, 24), Theme.AccentBlue);
            lblInsK = SumKey("INSERTED", new Point(160, 6));
            lblInsV = SumVal("0", new Point(160, 24), Theme.AccentGreen);
            lblSkpK = SumKey("SKIPPED", new Point(304, 6));
            lblSkpV = SumVal("0", new Point(304, 24), Theme.AccentGray);
            pnlSummary.Controls.AddRange(new Control[] {
                lblTotalK, lblTotalV, lblInsK, lblInsV, lblSkpK, lblSkpV
            });

            // ── ROLLBACK BAR ──────────────────────────────────────────
            pnlRollback = new Panel { Location = new Point(0, 336), Size = new Size(1068, 0), Visible = false };
            lblRollbackInfo = new Label { Location = new Point(0, 0), Size = new Size(600, 18), Font = new Font("Segoe UI", 8.5f), Visible = false };

            // ── GRID HEADER ───────────────────────────────────────────
            pnlGridHeader = new Panel
            {
                Location = new Point(0, 384),
                Size = new Size(1068, 34),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            lblGridTitle = new Label
            {
                Text = "Results",
                Location = new Point(0, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Theme.TextSecondary
            };

            lblRowCount = new Label
            {
                Text = "",
                Location = new Point(80, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Theme.AccentBlue
            };

            // Search box
            var lblSearch = new Label
            {
                Text = "🔎",
                Location = new Point(750, 8),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Theme.TextSecondary
            };
            txtSearch = new TextBox
            {
                Location = new Point(772, 5),
                Size = new Size(200, 22),
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f),
                Text = "Search...",
                ForeColor = Color.Gray
            };
            txtSearch.GotFocus += (s, e) => { if (txtSearch.Text == "Search...") { txtSearch.Text = ""; txtSearch.ForeColor = Theme.TextPrimary; } };
            txtSearch.LostFocus += (s, e) => { if (string.IsNullOrEmpty(txtSearch.Text)) { txtSearch.Text = "Search..."; txtSearch.ForeColor = Color.Gray; } };
            txtSearch.TextChanged += TxtSearch_Changed;

            pnlGridHeader.Controls.AddRange(new Control[] { lblGridTitle, lblRowCount, lblSearch, txtSearch });

            // ── DATA GRID ─────────────────────────────────────────────
            pnlGrid = new Panel
            {
                Location = new Point(0, 420),
                Size = new Size(1068, 290),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.None
            };

            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                EnableHeadersVisualStyles = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                RowTemplate = { Height = 28 },
                ScrollBars = ScrollBars.Vertical,
                MultiSelect = false
            };
            BuildGrid();
            pnlGrid.Controls.Add(dgv);

            pnlBody.Controls.AddRange(new Control[] {
                card1, card2, pnlAct, pnlSummary, pnlRollback,
                pnlGridHeader, pnlGrid
            });

            this.Controls.Add(pnlBody);
            this.Controls.Add(pnlTop);
            this.Controls.Add(pnlStatus);
        }

        // ── Search filter ──────────────────────────────────────────────
        private void TxtSearch_Changed(object sender, EventArgs e)
        {
            string q = (txtSearch.Text == "Search..." ? "" : txtSearch.Text.Trim()).ToLower();
            dgv.Rows.Clear();
            var filtered = string.IsNullOrEmpty(q)
                ? _allRows
                : _allRows.Where(r =>
                    r.Key.ToLower().Contains(q) ||
                    r.Value.ToLower().Contains(q)).ToList();

            foreach (var r in filtered)
            {
                string txt = r.Status == RowStatus.Inserted ? Lang.Get("statusInserted") : Lang.Get("statusSkipped");
                int idx = dgv.Rows.Add(r.Key, r.Value, txt);
                var row = dgv.Rows[idx];
                row.DefaultCellStyle.ForeColor = r.Status == RowStatus.Inserted ? Theme.RowInserted : Theme.RowSkipped;
                if (r.Status == RowStatus.Inserted)
                    row.DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            }
            lblRowCount.Text = $"({filtered.Count} rows)";
        }

        // ── Grid setup ─────────────────────────────────────────────────
        private void BuildGrid()
        {
            dgv.Columns.Clear();
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Key", HeaderText = Lang.Get("colKey"), FillWeight = 35, MinimumWidth = 150 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Value", HeaderText = Lang.Get("colValue"), FillWeight = 40, MinimumWidth = 150 });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = Lang.Get("colStatus"), FillWeight = 25, MinimumWidth = 120 });

            dgv.BackgroundColor = Theme.CardBg;
            dgv.GridColor = Theme.BorderColor;
            dgv.ColumnHeadersHeight = 32;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Theme.HeaderBg;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 5, 10, 5);
            dgv.DefaultCellStyle.BackColor = Theme.GridEven;
            dgv.DefaultCellStyle.ForeColor = Theme.TextPrimary;
            dgv.DefaultCellStyle.Padding = new Padding(10, 2, 10, 2);
            dgv.DefaultCellStyle.SelectionBackColor = Theme.GridSelect;
            dgv.DefaultCellStyle.SelectionForeColor = Color.White;
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Theme.GridOdd;
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = Theme.TextPrimary;
        }

        // ── Theme ──────────────────────────────────────────────────────
        private void ApplyTheme()
        {
            this.BackColor = Theme.FormBg;
            pnlTop.BackColor = Theme.TopBarBg;
            pnlBody.BackColor = Theme.FormBg;
            lblStatus.BackColor = Theme.CardBg;
            lblStatus.ForeColor = Theme.TextSecondary;

            foreach (var l in new[] { lblSchema, lblTable, lblKeyCol })
                l.ForeColor = Theme.TextSecondary;
            foreach (var l in new[] { lblTotalK, lblInsK, lblSkpK })
                l.ForeColor = Theme.TextSecondary;
            lblGridTitle.ForeColor = Theme.TextSecondary;
            lblRowCount.ForeColor = Theme.AccentBlue;

            foreach (var c in new[] { cboSchema, cboTable, cboKeyCol })
            { c.BackColor = Theme.InputBg; c.ForeColor = Theme.TextPrimary; c.Invalidate(); }
            cboLang.BackColor = Theme.InputBg;
            cboLang.ForeColor = Theme.IsDark ? Color.White : Theme.TextPrimary;

            txtPath.BackColor = Theme.InputBg; txtPath.ForeColor = Theme.TextPrimary;
            txtPaste.BackColor = Theme.InputBg; txtPaste.ForeColor = Theme.TextPrimary;
            txtSearch.BackColor = Theme.InputBg; txtSearch.ForeColor = Theme.TextPrimary;

            tabJson.BackColor = Theme.CardBg;
            tabFile.BackColor = Theme.CardBg;
            tabPaste.BackColor = Theme.CardBg;
            txtWebConfigPath.BackColor = Theme.InputBg;
            txtWebConfigPath.ForeColor = Theme.TextPrimary;

            foreach (Control c in pnlBody.Controls)
                if (c is Panel p) p.BackColor = Theme.CardBg;

            pnlSummary.BackColor = Theme.SummaryBg;
            pnlGridHeader.BackColor = Theme.FormBg;

            BuildGrid();
            // Recolor existing rows
            foreach (DataGridViewRow row in dgv.Rows)
            {
                row.DefaultCellStyle.BackColor = dgv.Rows.IndexOf(row) % 2 == 0 ? Theme.GridEven : Theme.GridOdd;
                row.DefaultCellStyle.ForeColor = row.DefaultCellStyle.ForeColor; // keep status color
            }

            btnTheme.Text = Theme.IsDark ? "☀  Light Mode" : "🌙  Dark Mode";
            btnTheme.BackColor = Theme.IsDark ? Color.FromArgb(130, 40, 75) : Color.FromArgb(175, 45, 85);
        }

        // ── Text refresh ───────────────────────────────────────────────
        private void RefreshText()
        {
            tabFile.Text = Lang.Get("uploadFile");
            tabPaste.Text = Lang.Get("pasteJson");
            btnBrowse.Text = "📂  " + Lang.Get("browse");
            btnPreview.Text = "🔍  " + Lang.Get("preview");
            btnSync.Text = "💾  " + Lang.Get("sync");
            btnClear.Text = "🗑  " + Lang.Get("clear");
            btnSettings.Text = "⚙  " + Lang.Get("settings");
            btnSavePreset.Text = "💾  " + Lang.Get("savePreset");
            btnLoadPreset.Text = "📁  " + Lang.Get("loadPreset");
            btnRollback.Text = "↩  " + Lang.Get("rollback");
            lblTotalK.Text = Lang.Get("total").ToUpper();
            lblInsK.Text = Lang.Get("inserted").ToUpper();
            lblSkpK.Text = Lang.Get("skipped").ToUpper();
            lblStatus.Text = Lang.Get("ready");
            btnTheme.Text = Theme.IsDark ? "☀  Light Mode" : "🌙  Dark Mode";
            BuildGrid();
        }

        // ── Settings / Presets ─────────────────────────────────────────
        private void OpenSettings()
        {
            using (var f = new SettingsForm(_cfg))
                if (f.ShowDialog(this) == DialogResult.OK)
                { _cfg = f.Settings; _cfg.Save(); OracleHelper.Configure(_cfg); LoadSchemasAsync(); }
        }

        private void BtnSavePreset_Click(object sender, EventArgs e)
        {
            if (cboSchema.SelectedIndex <= 0) { Warn(Lang.Get("noSchema")); return; }
            _cfg.LastSchema = cboSchema.SelectedItem?.ToString() ?? "";
            _cfg.LastTable = cboTable.SelectedItem?.ToString() ?? "";
            _cfg.LastKeyCol = cboKeyCol.SelectedItem?.ToString() ?? "";
            _cfg.Save(); Status(Lang.Get("presetSaved"));
        }

        private void BtnLoadPreset_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(_cfg.LastSchema))
            { int i = cboSchema.FindStringExact(_cfg.LastSchema); if (i >= 0) cboSchema.SelectedIndex = i; }
        }

        // ── DB Loading ─────────────────────────────────────────────────
        private async void LoadSchemasAsync()
        {
            Status(Lang.Get("loadingSchemas")); Busy(true);
            cboSchema.Items.Clear(); cboSchema.Items.Add(Lang.Get("selectSchema")); cboSchema.SelectedIndex = 0;
            try
            {
                var list = await Task.Run(() => OracleHelper.GetSchemas());
                foreach (var s in list) cboSchema.Items.Add(s);
                if (!string.IsNullOrEmpty(_cfg.LastSchema)) { int i = cboSchema.FindStringExact(_cfg.LastSchema); if (i >= 0) cboSchema.SelectedIndex = i; }
                Status($"Ready — {list.Count} schemas");
            }
            catch (Exception ex) { Status($"Error: {ex.Message}"); Warn(ex.Message); }
            finally { Busy(false); }
        }

        private async void CboSchema_Changed(object sender, EventArgs e)
        {
            cboTable.Items.Clear(); cboTable.Items.Add(Lang.Get("selectTable")); cboTable.SelectedIndex = 0; cboTable.Enabled = false;
            cboKeyCol.Items.Clear(); cboKeyCol.Items.Add(Lang.Get("selectKeyCol")); cboKeyCol.SelectedIndex = 0; cboKeyCol.Enabled = false;
            btnSync.Enabled = false;
            if (cboSchema.SelectedIndex <= 0) return;
            string schema = cboSchema.SelectedItem.ToString(); _cfg.LastSchema = schema;
            Status(Lang.Get("loadingTables")); Busy(true);
            try
            {
                var list = await Task.Run(() => OracleHelper.GetTables(schema));
                foreach (var t in list) cboTable.Items.Add(t);
                cboTable.Enabled = true;
                if (!string.IsNullOrEmpty(_cfg.LastTable)) { int i = cboTable.FindStringExact(_cfg.LastTable); if (i >= 0) cboTable.SelectedIndex = i; }
                Status($"{list.Count} tables");
            }
            catch (Exception ex) { Status($"Error: {ex.Message}"); Warn($"{Lang.Get("connError")}: {ex.Message}"); }
            finally { Busy(false); }
        }

        private async void CboTable_Changed(object sender, EventArgs e)
        {
            cboKeyCol.Items.Clear(); cboKeyCol.Items.Add(Lang.Get("selectKeyCol")); cboKeyCol.SelectedIndex = 0; cboKeyCol.Enabled = false;
            btnSync.Enabled = false;
            if (cboTable.SelectedIndex <= 0) return;
            string schema = cboSchema.SelectedItem.ToString();
            string table = cboTable.SelectedItem.ToString(); _cfg.LastTable = table;
            Status(Lang.Get("loadingCols")); Busy(true);
            try
            {
                var list = await Task.Run(() => OracleHelper.GetColumns(schema, table));
                foreach (var c in list) cboKeyCol.Items.Add(c.Name);
                cboKeyCol.Enabled = true;
                if (!string.IsNullOrEmpty(_cfg.LastKeyCol)) { int i = cboKeyCol.FindStringExact(_cfg.LastKeyCol); if (i >= 0) cboKeyCol.SelectedIndex = i; }
                Status($"{list.Count} columns");
            }
            catch (Exception ex) { Status($"Error: {ex.Message}"); }
            finally { Busy(false); }
        }

        // ── Browse ─────────────────────────────────────────────────────
        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (var d = new OpenFileDialog { Filter = "JSON (*.json)|*.json|All (*.*)|*.*" })
                if (d.ShowDialog() == DialogResult.OK) txtPath.Text = d.FileName;
        }

        private void BtnBrowseWebConfig_Click(object sender, EventArgs e)
        {
            using (var d = new OpenFileDialog { Filter = "Web.config|Web.config|Config files (*.config)|*.config|All (*.*)|*.*" })
                if (d.ShowDialog() == DialogResult.OK) txtWebConfigPath.Text = d.FileName;
        }

        private string GetJson() => null; // not used directly anymore

        private List<(string Key, string Value)> GetPairs()
        {
            // WebConfig tab
            if (tabJson.SelectedTab != null && tabJson.SelectedTab.Text.Contains("Web.config"))
            {
                if (string.IsNullOrEmpty(txtWebConfigPath.Text)) { Warn("Please select a Web.config file."); return null; }
                try { return OracleHelper.ParseWebConfig(txtWebConfigPath.Text); }
                catch (Exception ex) { Warn($"Failed to parse Web.config: {ex.Message}"); return null; }
            }
            // JSON file tab
            if (tabJson.SelectedTab == tabFile)
            {
                if (string.IsNullOrEmpty(txtPath.Text)) { Warn(Lang.Get("noFile")); return null; }
                try { return OracleHelper.FlattenJson(JToken.Parse(File.ReadAllText(txtPath.Text))); }
                catch (Exception ex) { Warn($"{Lang.Get("invalidJson")}: {ex.Message}"); return null; }
            }
            // Paste tab
            if (string.IsNullOrWhiteSpace(txtPaste.Text)) { Warn(Lang.Get("noJson")); return null; }
            try { return OracleHelper.FlattenJson(JToken.Parse(txtPaste.Text.Trim())); }
            catch (Exception ex) { Warn($"{Lang.Get("invalidJson")}: {ex.Message}"); return null; }
        }

        private bool Check()
        {
            if (cboSchema.SelectedIndex <= 0) { Warn(Lang.Get("noSchema")); return false; }
            if (cboTable.SelectedIndex <= 0) { Warn(Lang.Get("noTable")); return false; }
            if (cboKeyCol.SelectedIndex <= 0) { Warn(Lang.Get("noKeyCol")); return false; }
            return true;
        }

        // ── Preview ────────────────────────────────────────────────────
        private async void BtnPreview_Click(object sender, EventArgs e)
        {
            if (!Check()) return;
            var pairs = GetPairs(); if (pairs == null) return;
            string schema = cboSchema.SelectedItem.ToString();
            string table = cboTable.SelectedItem.ToString();
            string keyCol = cboKeyCol.SelectedItem.ToString();
            _cfg.LastKeyCol = keyCol; _cfg.Save();
            Status(Lang.Get("previewing")); Busy(true); Freeze(true);
            dgv.Rows.Clear(); _allRows.Clear();
            pnlSummary.Visible = pnlRollback.Visible = false;
            lblRowCount.Text = "";
            try
            {
                var r = await Task.Run(() => OracleHelper.Sync(schema, table, keyCol, pairs, true));
                _pairs = pairs;
                foreach (var row in r.Rows) AddRow(row.Key, row.Value, row.Status);
                UpdateSummary(r); pnlSummary.Visible = true;
                btnSync.Enabled = r.Inserted > 0;
                lblRowCount.Text = $"({r.Total} rows)";
                RepositionGrid();
                Status(string.Format(Lang.Get("previewDone"), r.Inserted, r.Skipped));
                if (r.Inserted == 0)
                    MessageBox.Show(Lang.Get("upToDate"), "BelSync", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { Status($"Error: {ex.Message}"); MessageBox.Show(ex.Message, Lang.Get("error"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { Busy(false); Freeze(false); }
        }

        // ── Sync ───────────────────────────────────────────────────────
        private async void BtnSync_Click(object sender, EventArgs e)
        {
            if (!Check() || _pairs == null) return;
            string schema = cboSchema.SelectedItem.ToString();
            string table = cboTable.SelectedItem.ToString();
            string keyCol = cboKeyCol.SelectedItem.ToString();
            if (MessageBox.Show(string.Format(Lang.Get("confirmSync"), _pairs.Count, schema, table),
                Lang.Get("confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            Status(Lang.Get("syncing")); Busy(true); Freeze(true);
            try
            {
                var r = await Task.Run(() => OracleHelper.Sync(schema, table, keyCol, _pairs, false));
                dgv.Rows.Clear(); _allRows.Clear();
                foreach (var row in r.Rows) AddRow(row.Key, row.Value, row.Status);
                UpdateSummary(r); pnlSummary.Visible = true; btnSync.Enabled = false;
                lblRowCount.Text = $"({r.Total} rows)";
                if (r.Inserted > 0)
                {
                    _oidFrom = r.RollbackOidFrom; _oidTo = r.RollbackOidTo;
                    _rbSchema = schema; _rbTable = table;
                    btnRollback.Enabled = true;
                    Status($"{r.Inserted} inserted · {r.Skipped} skipped  |  ↩ Rollback available");
                }
                else
                {
                    btnRollback.Enabled = false;
                    Status($"{r.Inserted} inserted · {r.Skipped} skipped");
                }
                RepositionGrid();
                MessageBox.Show(string.Format(Lang.Get("syncDone"), r.Inserted, r.Skipped), "BelSync", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { Status($"Error: {ex.Message}"); MessageBox.Show(ex.Message, Lang.Get("error"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { Busy(false); Freeze(false); }
        }

        // ── Rollback ───────────────────────────────────────────────────
        private async void BtnRollback_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(string.Format(Lang.Get("confirmRollback"), _oidTo - _oidFrom, _rbSchema, _rbTable),
                Lang.Get("confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            Busy(true); btnRollback.Enabled = false;
            try
            {
                int n = await Task.Run(() => OracleHelper.Rollback(_rbSchema, _rbTable, _oidFrom, _oidTo));
                btnRollback.Enabled = false;
                RepositionGrid();
                Status(string.Format(Lang.Get("rollbackDone"), n));
                MessageBox.Show(string.Format(Lang.Get("rollbackDone"), n), "BelSync", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { Status($"Error: {ex.Message}"); MessageBox.Show(ex.Message, Lang.Get("error"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { Busy(false); btnRollback.Enabled = true; }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            dgv.Rows.Clear(); _allRows.Clear();
            pnlSummary.Visible = false;
            btnSync.Enabled = btnRollback.Enabled = false;
            _pairs = null; lblRowCount.Text = ""; txtSearch.Text = "";
            RepositionGrid();
            Status(Lang.Get("ready"));
        }

        // ── Helpers ────────────────────────────────────────────────────
        private void AddRow(string key, string value, RowStatus status)
        {
            string txt = status == RowStatus.Inserted ? Lang.Get("statusInserted")
                       : Lang.Get("statusSkipped");
            int idx = dgv.Rows.Add(key, value, txt);
            var row = dgv.Rows[idx];
            Color fg = status == RowStatus.Inserted ? Theme.RowInserted : Theme.RowSkipped;
            row.DefaultCellStyle.ForeColor = fg;
            if (status == RowStatus.Inserted)
                row.DefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            _allRows.Add((key, value, status));
        }

        private void UpdateSummary(SyncResult r)
        {
            lblTotalV.Text = r.Total.ToString();
            lblInsV.Text = r.Inserted.ToString();
            lblSkpV.Text = r.Skipped.ToString();
        }

        // Reposition grid header and grid based on visible panels
        private void RepositionGrid()
        {
            const int BASE = 282;
            const int GAP = 4;
            int y = BASE;
            pnlSummary.Top = y;
            if (pnlSummary.Visible) y += pnlSummary.Height + GAP;
            pnlGridHeader.Top = y;
            pnlGrid.Top = y + pnlGridHeader.Height;
        }

        private void Status(string msg) => lblStatus.Text = msg;
        private void Busy(bool b) => pgBar.Visible = b;
        private void Freeze(bool f) { btnPreview.Enabled = !f; btnClear.Enabled = !f; }
        private void Warn(string msg) => MessageBox.Show(msg, "BelSync", MessageBoxButtons.OK, MessageBoxIcon.Warning);

        // ── Control factories ─────────────────────────────────────────
        private Panel Card(int top, int height)
        {
            var p = new Panel
            {
                Location = new Point(0, top),
                Size = new Size(1068, height),
                BackColor = Theme.CardBg,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            return p;
        }

        private Label FieldLbl(string text, Point loc) => new Label
        {
            Text = text,
            Location = loc,
            AutoSize = true,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Theme.TextSecondary
        };

        private ComboBox Cbo(Point loc, int width)
        {
            var c = new ComboBox
            {
                Location = loc,
                Size = new Size(width, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.InputBg,
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                DropDownWidth = Math.Max(width, 350)
            };
            return c;
        }

        private Button Btn(string text, Point loc, int width, Color color)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(width, 28),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(6, 0, 0, 0)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(color, 0.1f);
            return b;
        }

        private Button TopBtn(string text, Point loc, int width)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(width, 30),
                BackColor = Color.FromArgb(175, 45, 85),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(155, 35, 70);
            return b;
        }

        private Label SumKey(string text, Point loc) => new Label
        {
            Text = text,
            Location = loc,
            AutoSize = true,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Theme.TextSecondary
        };

        private Label SumVal(string text, Point loc, Color color) => new Label
        {
            Text = text,
            Location = loc,
            AutoSize = true,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = color
        };
    }
}