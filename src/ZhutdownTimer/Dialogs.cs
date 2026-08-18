using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ZhutdownTimer
{
    internal sealed class ExecutionPromptForm : Form
    {
        private readonly Timer timer = new Timer();
        private readonly Label countdownLabel = new Label();
        private readonly Label bodyLabel = new Label();
        private int secondsLeft;

        public ExecutionPromptForm(PowerAction action, int seconds, ThemePalette palette)
        {
            secondsLeft = Math.Max(1, seconds);
            Text = string.Format(L.T("ConfirmTitle"), L.Action(action));
            ClientSize = new Size(500, 300);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = true;
            Icon = AssetLoader.LoadAppIcon();
            Font = new Font("Microsoft YaHei UI", 9F);

            var title = new Label
            {
                Text = string.Format(L.T("ConfirmTitle"), L.Action(action)),
                Font = new Font(Font.FontFamily, 18F, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 58
            };
            countdownLabel.Font = new Font("Consolas", 42F, FontStyle.Bold);
            countdownLabel.AutoSize = false;
            countdownLabel.TextAlign = ContentAlignment.MiddleCenter;
            countdownLabel.Dock = DockStyle.Top;
            countdownLabel.Height = 82;
            bodyLabel.AutoSize = false;
            bodyLabel.TextAlign = ContentAlignment.TopCenter;
            bodyLabel.Dock = DockStyle.Top;
            bodyLabel.Height = 55;

            var buttons = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(22, 8, 22, 10), ColumnCount = 2 };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            var skip = new ModernButton { Text = L.T("SkipThisTime"), Dock = DockStyle.Fill, Margin = new Padding(0, 0, 6, 0), Palette = palette };
            var execute = new ModernButton { Text = L.T("ExecuteNow"), Dock = DockStyle.Fill, Margin = new Padding(6, 0, 0, 0), Primary = true, Palette = palette };
            skip.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            execute.Click += delegate { DialogResult = DialogResult.OK; Close(); };
            buttons.Controls.Add(skip, 0, 0);
            buttons.Controls.Add(execute, 1, 0);

            Controls.Add(buttons);
            Controls.Add(bodyLabel);
            Controls.Add(countdownLabel);
            Controls.Add(title);
            ThemeApplier.Apply(this, palette);
            NativeTheme.ApplyDarkTitleBar(this, palette.IsDark);
            title.ForeColor = palette.Text;
            countdownLabel.ForeColor = palette.Danger;
            bodyLabel.ForeColor = palette.Muted;

            timer.Interval = 1000;
            timer.Tick += delegate
            {
                secondsLeft--;
                UpdateText();
                if (secondsLeft <= 0)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
            Shown += delegate { UpdateText(); timer.Start(); Activate(); };
            FormClosed += delegate { timer.Stop(); timer.Dispose(); };
        }

        private void UpdateText()
        {
            countdownLabel.Text = secondsLeft.ToString();
            bodyLabel.Text = string.Format(L.T("ConfirmBody"), secondsLeft);
        }
    }

    internal sealed class HistoryForm : Form
    {
        private readonly SettingsStore store;
        private readonly ListView list = new ListView();
        private readonly ThemePalette palette;

        public HistoryForm(SettingsStore store, ThemePalette palette)
        {
            this.store = store;
            this.palette = palette;
            Text = L.T("HistoryTitle");
            ClientSize = new Size(760, 440);
            MinimumSize = new Size(640, 360);
            StartPosition = FormStartPosition.CenterParent;
            Icon = AssetLoader.LoadAppIcon();
            Font = new Font("Microsoft YaHei UI", 9F);

            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.GridLines = false;
            list.HideSelection = false;
            list.Columns.Add(L.T("Time"), 155);
            list.Columns.Add(L.T("Action"), 110);
            list.Columns.Add(L.T("Result"), 100);
            list.Columns.Add(L.T("Details"), 340);

            var buttons = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 64, Padding = new Padding(16, 10, 16, 10), ColumnCount = 4 };
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var open = new ModernButton { Text = L.T("OpenDataFolder"), Width = 145, Palette = palette };
            var clear = new ModernButton { Text = L.T("ClearHistory"), Width = 115, Danger = true, Palette = palette, Margin = new Padding(10, 0, 0, 0) };
            var close = new ModernButton { Text = L.T("Close"), Width = 100, Palette = palette };
            open.Click += delegate { Process.Start("explorer.exe", "\"" + AppPaths.DataDirectory + "\""); };
            clear.Click += delegate
            {
                if (MessageBox.Show(this, L.T("ClearHistoryConfirm"), L.T("Question"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    store.ClearHistory();
                    RefreshItems();
                }
            };
            close.Click += delegate { Close(); };
            buttons.Controls.Add(open, 0, 0);
            buttons.Controls.Add(clear, 1, 0);
            buttons.Controls.Add(close, 3, 0);

            var padding = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
            padding.Controls.Add(list);
            Controls.Add(padding);
            Controls.Add(buttons);
            ThemeApplier.Apply(this, palette);
            NativeTheme.ApplyDarkTitleBar(this, palette.IsDark);
            padding.BackColor = palette.Window;
            list.BackColor = palette.Surface;
            list.ForeColor = palette.Text;
            RefreshItems();
        }

        private void RefreshItems()
        {
            list.BeginUpdate();
            list.Items.Clear();
            foreach (HistoryEntry entry in store.State.History)
            {
                string details = string.IsNullOrEmpty(entry.Details) ? string.Empty : L.T(entry.Details);
                var item = new ListViewItem(entry.TimestampLocal.ToString("yyyy-MM-dd HH:mm:ss"));
                item.SubItems.Add(L.Action(entry.Action));
                item.SubItems.Add(L.Result(entry.ResultCode));
                item.SubItems.Add(details);
                list.Items.Add(item);
            }
            list.EndUpdate();
        }
    }

    internal sealed class AboutForm : Form
    {
        public AboutForm(ThemePalette palette)
        {
            Text = L.T("AboutTitle");
            ClientSize = new Size(500, 390);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Icon = AssetLoader.LoadAppIcon();
            Font = new Font("Microsoft YaHei UI", 9F);

            var logo = new PictureBox { Image = AssetLoader.LoadLogo(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(88, 88), Location = new Point(206, 24) };
            var title = new Label { Text = L.T("AppTitle"), Font = new Font(Font.FontFamily, 20F, FontStyle.Bold), AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
            title.SetBounds(30, 120, 440, 44);
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionLabel = new Label { Text = string.Format(L.T("Version"), version.ToString(3)), AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
            versionLabel.SetBounds(30, 164, 440, 24);
            var body = new Label { Text = L.T("AboutBody"), AutoSize = false, TextAlign = ContentAlignment.TopCenter };
            body.SetBounds(50, 202, 400, 76);

            var source = new ModernButton { Text = L.T("SourceCode"), Palette = palette, Primary = true };
            source.SetBounds(85, 292, 205, 42);
            var close = new ModernButton { Text = L.T("Close"), Palette = palette };
            close.SetBounds(305, 292, 110, 42);
            source.Click += delegate { Process.Start("https://github.com/LONGSANGDONTSLEEP/ZhutdownTimer"); };
            close.Click += delegate { Close(); };

            Controls.Add(logo);
            Controls.Add(title);
            Controls.Add(versionLabel);
            Controls.Add(body);
            Controls.Add(source);
            Controls.Add(close);
            ThemeApplier.Apply(this, palette);
            NativeTheme.ApplyDarkTitleBar(this, palette.IsDark);
            versionLabel.ForeColor = palette.Muted;
            body.ForeColor = palette.Muted;
        }
    }
}
