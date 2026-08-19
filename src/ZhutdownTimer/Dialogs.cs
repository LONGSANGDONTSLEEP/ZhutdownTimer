using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace ZhutdownTimer
{
    internal sealed class ExecutionPromptForm : FrostedForm
    {
        private readonly Timer timer = new Timer();
        private readonly GlassLabel countdownLabel = new GlassLabel();
        private readonly GlassLabel bodyLabel = new GlassLabel();
        private int secondsLeft;

        public ExecutionPromptForm(PowerAction action, int seconds, ThemePalette palette)
        {
            secondsLeft = Math.Max(1, seconds);
            Text = string.Format(L.T("ConfirmTitle"), L.Action(action));
            ClientSize = new Size(520, 330);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ShowInTaskbar = true;
            Icon = AssetLoader.LoadAppIcon();
            Font = UiFonts.Create(10F, FontStyle.Regular);
            GlassPalette = palette;

            var title = new GlassLabel
            {
                Text = string.Format(L.T("ConfirmTitle"), L.Action(action)),
                Font = UiFonts.Create(18F, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 66
            };
            countdownLabel.Font = new Font("Cascadia Mono", 38F, FontStyle.Bold);
            countdownLabel.AutoSize = false;
            countdownLabel.TextAlign = ContentAlignment.MiddleCenter;
            countdownLabel.Dock = DockStyle.Top;
            countdownLabel.Height = 88;
            bodyLabel.AutoSize = false;
            bodyLabel.TextAlign = ContentAlignment.TopCenter;
            bodyLabel.Dock = DockStyle.Top;
            bodyLabel.Height = 64;

            var buttons = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 74, Padding = new Padding(24, 10, 24, 14), ColumnCount = 2, BackColor = Color.Transparent };
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
            NativeTheme.ApplyWindowEffects(this, palette.IsDark);
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

    internal sealed class HistoryForm : FrostedForm
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
            Font = UiFonts.Create(9.6F, FontStyle.Regular);
            GlassPalette = palette;

            list.Dock = DockStyle.Fill;
            list.View = View.Details;
            list.FullRowSelect = true;
            list.GridLines = false;
            list.HideSelection = false;
            list.Columns.Add(L.T("Time"), 155);
            list.Columns.Add(L.T("Action"), 110);
            list.Columns.Add(L.T("Result"), 100);
            list.Columns.Add(L.T("Details"), 340);

            var buttons = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 72, Padding = new Padding(18, 10, 18, 14), ColumnCount = 4, BackColor = Color.Transparent };
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
            NativeTheme.ApplyWindowEffects(this, palette.IsDark);
            padding.BackColor = Color.Transparent;
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

    internal sealed class AboutForm : FrostedForm
    {
        public AboutForm(ThemePalette palette)
        {
            Text = L.T("AboutTitle");
            ClientSize = new Size(520, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Icon = AssetLoader.LoadAppIcon();
            Font = UiFonts.Create(10F, FontStyle.Regular);
            GlassPalette = palette;

            var logo = new PictureBox { Image = AssetLoader.LoadLogo(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(92, 92), Location = new Point(214, 24) };
            var title = new GlassLabel { Text = L.T("AppTitle"), Font = UiFonts.Create(20F, FontStyle.Bold), AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
            title.SetBounds(30, 126, 460, 48);
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            var versionLabel = new GlassLabel { Text = string.Format(L.T("Version"), version.ToString(3)), AutoSize = false, TextAlign = ContentAlignment.MiddleCenter };
            versionLabel.SetBounds(30, 176, 460, 28);
            var body = new GlassLabel { Text = L.T("AboutBody"), AutoSize = false, TextAlign = ContentAlignment.TopCenter };
            body.SetBounds(50, 216, 420, 88);

            var source = new ModernButton { Text = L.T("SourceCode"), Palette = palette, Primary = true };
            source.SetBounds(88, 326, 214, 48);
            var close = new ModernButton { Text = L.T("Close"), Palette = palette };
            close.SetBounds(316, 326, 116, 48);
            source.Click += delegate { Process.Start("https://github.com/LONGSANGDONTSLEEP/ZhutdownTimer"); };
            close.Click += delegate { Close(); };

            Controls.Add(logo);
            Controls.Add(title);
            Controls.Add(versionLabel);
            Controls.Add(body);
            Controls.Add(source);
            Controls.Add(close);
            ThemeApplier.Apply(this, palette);
            NativeTheme.ApplyWindowEffects(this, palette.IsDark);
            versionLabel.ForeColor = palette.Muted;
            body.ForeColor = palette.Muted;
        }
    }
}
