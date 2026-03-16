using System;
using System.Drawing;
using System.Windows.Forms;

namespace BelSync
{
    public class SettingsForm : Form
    {
        private TextBox txtHost, txtPort, txtService, txtAdminUser;
        private Button  btnTest, btnSave, btnCancel;
        private Label   lblResult;
        public AppSettings Settings { get; private set; }

        public SettingsForm(AppSettings current)
        {
            Settings = current;
            BuildUI();
            txtHost.Text      = current.Host;
            txtPort.Text      = current.Port;
            txtService.Text   = current.Service;
            txtAdminUser.Text = current.AdminUser;
            ApplyTheme();
        }

        private void BuildUI()
        {
            this.Text            = Lang.Get("settings");
            this.Size            = new Size(440, 360);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox     = false;
            this.StartPosition   = FormStartPosition.CenterParent;
            this.Font            = new Font("Segoe UI", 9f);

            int y = 18;
            txtHost      = Field("Host / IP:",       ref y);
            txtPort      = Field("Port:",            ref y);
            txtService   = Field("Service Name:",    ref y);
            txtAdminUser = Field("Admin Schema:",    ref y);

            lblResult = new Label {
                Location  = new Point(20, y + 2),
                Size      = new Size(390, 24),
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.Gray
            };

            btnTest = MBtn("Test Connection", new Point(20, y + 32), 140, Theme.AccentBlue);
            btnSave = MBtn("Save",            new Point(240, y + 32), 80,  Theme.AccentGreen);
            btnCancel = MBtn("Cancel",        new Point(330, y + 32), 80,  Theme.AccentGray);
            btnSave.DialogResult   = DialogResult.OK;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnTest.Click  += BtnTest_Click;
            btnSave.Click  += (s, e) => {
                Settings.Host = txtHost.Text.Trim(); Settings.Port = txtPort.Text.Trim();
                Settings.Service = txtService.Text.Trim(); Settings.AdminUser = txtAdminUser.Text.Trim();
            };

            this.Controls.AddRange(new Control[] { lblResult, btnTest, btnSave, btnCancel });
            this.AcceptButton = btnSave;
            this.CancelButton = btnCancel;
        }

        private TextBox Field(string label, ref int y)
        {
            var lbl = new Label { Text = label.ToUpper(), Location = new Point(20, y), AutoSize = true,
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold), ForeColor = Theme.TextSecondary };
            var txt = new TextBox { Location = new Point(20, y + 18), Size = new Size(390, 24),
                BackColor = Theme.InputBg, ForeColor = Theme.TextPrimary, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.AddRange(new Control[] { lbl, txt });
            y += 56;
            return txt;
        }

        private Button MBtn(string text, Point loc, int width, Color color)
        {
            var b = new Button { Text = text, Location = loc, Size = new Size(width, 30),
                BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold), Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }

        private void BtnTest_Click(object sender, EventArgs e)
        {
            lblResult.Text = "Testing..."; lblResult.ForeColor = Color.Gray;
            try
            {
                OracleHelper.Configure(new AppSettings {
                    Host = txtHost.Text.Trim(), Port = txtPort.Text.Trim(),
                    Service = txtService.Text.Trim(), AdminUser = txtAdminUser.Text.Trim()
                });
                OracleHelper.GetSchemas();
                lblResult.Text      = "✓ Connection successful!";
                lblResult.ForeColor = Theme.AccentGreen;
            }
            catch (Exception ex)
            {
                lblResult.Text      = $"✗ {ex.Message}";
                lblResult.ForeColor = Theme.AccentRed;
            }
        }

        private void ApplyTheme()
        {
            this.BackColor = Theme.FormBg;
            foreach (Control c in this.Controls)
            {
                if (c is Label l) l.ForeColor = Theme.TextSecondary;
                if (c is TextBox t) { t.BackColor = Theme.InputBg; t.ForeColor = Theme.TextPrimary; }
            }
        }
    }
}
