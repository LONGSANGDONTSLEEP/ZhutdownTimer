using System;
using System.Drawing;
using System.Windows.Forms;

namespace ZhutdownTimer
{
    internal sealed class MainForm : Form
    {
        private readonly SettingsStore store;
        private readonly LaunchOptions launchOptions;
        private readonly Timer timer = new Timer();
        private readonly NotifyIcon trayIcon = new NotifyIcon();
        private readonly ToolStripMenuItem trayShow = new ToolStripMenuItem();
        private readonly ToolStripMenuItem trayCancel = new ToolStripMenuItem();
        private readonly ToolStripMenuItem trayHistory = new ToolStripMenuItem();
        private readonly ToolStripMenuItem trayExit = new ToolStripMenuItem();

        private readonly PictureBox logo = new PictureBox();
        private readonly Label titleLabel = new Label();
        private readonly Label subtitleLabel = new Label();
        private readonly ModernComboBox languageBox = new ModernComboBox();
        private readonly ModernComboBox themeBox = new ModernComboBox();
        private readonly ModernButton aboutButton = new ModernButton();

        private readonly CardPanel scheduleCard = new CardPanel();
        private readonly CardPanel statusCard = new CardPanel();
        private readonly Label actionCaption = new Label();
        private readonly ModernComboBox actionBox = new ModernComboBox();
        private readonly Label scheduleCaption = new Label();
        private readonly GlassRadioButton countdownMode = new GlassRadioButton();
        private readonly GlassRadioButton clockMode = new GlassRadioButton();
        private readonly Panel countdownPanel = new Panel();
        private readonly Panel clockPanel = new Panel();
        private readonly GlassNumberInput hours = new GlassNumberInput();
        private readonly GlassNumberInput minutes = new GlassNumberInput();
        private readonly GlassNumberInput seconds = new GlassNumberInput();
        private readonly Label hoursLabel = new Label();
        private readonly Label minutesLabel = new Label();
        private readonly Label secondsLabel = new Label();
        private readonly GlassTimeInput clockPicker = new GlassTimeInput();
        private readonly Label repeatCaption = new Label();
        private readonly ModernComboBox repeatBox = new ModernComboBox();
        private readonly Label optionsCaption = new Label();
        private readonly GlassCheckBox forceClose = new GlassCheckBox();
        private readonly GlassCheckBox preventSleep = new GlassCheckBox();
        private readonly GlassCheckBox alwaysOnTop = new GlassCheckBox();
        private readonly GlassCheckBox startWithWindows = new GlassCheckBox();
        private readonly GlassCheckBox startMinimized = new GlassCheckBox();
        private readonly Label confirmCaption = new Label();
        private readonly ModernComboBox confirmBox = new ModernComboBox();
        private readonly Label repeatHint = new Label();
        private readonly ModernButton startButton = new ModernButton();
        private readonly ModernButton cancelButton = new ModernButton();

        private readonly Label stateBadge = new Label();
        private readonly Label remainingCaption = new Label();
        private readonly Label remainingLabel = new Label();
        private readonly FlatProgressBar progress = new FlatProgressBar();
        private readonly Label nextCaption = new Label();
        private readonly Label actionLabel = new Label();
        private readonly Label targetLabel = new Label();
        private readonly Label safetyHint = new Label();
        private readonly ModernButton historyButton = new ModernButton();
        private readonly ModernButton minimizeButton = new ModernButton();

        private ThemePalette palette;
        private ActiveSchedule activeSchedule;
        private bool running;
        private bool initializing = true;
        private bool refreshingLocalization;
        private bool allowExit;
        private bool dryRun;

        public MainForm(SettingsStore store, LaunchOptions options)
        {
            this.store = store;
            launchOptions = options;
            dryRun = options.DryRun;
            if (options.Language.HasValue) store.State.Settings.Language = options.Language.Value;
            if (options.Theme.HasValue) store.State.Settings.Theme = options.Theme.Value;
            L.Language = store.State.Settings.Language;

            Text = L.T("AppTitle");
            ClientSize = new Size(1040, 760);
            MinimumSize = new Size(980, 720);
            StartPosition = FormStartPosition.CenterScreen;
            Font = UiFonts.Create(10F, FontStyle.Regular);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            Icon = AssetLoader.LoadAppIcon();
            KeyPreview = true;

            BuildInterface();
            BuildTray();
            ApplyLocalization();
            ApplySavedSettings();
            ApplyLaunchOptions();
            ApplyLocalization();
            ApplyTheme();
            WireEvents();
            initializing = false;
            UpdateScheduleMode();

            RestoreSchedule();
            if (options.Start && !running) StartSchedule(null, EventArgs.Empty);
            Shown += delegate
            {
                LayoutScheduleCard();
                LayoutStatusCard();
                if (options.Background || (store.State.Settings.StartMinimized && !options.Start))
                    HideToTray();
            };
        }

        private void BuildInterface()
        {
            var header = new Panel { Height = 100, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, BackColor = Color.Transparent };
            header.SetBounds(0, 0, ClientSize.Width, 100);
            logo.Image = AssetLoader.LoadLogo();
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.SetBounds(28, 20, 60, 60);
            titleLabel.Font = UiFonts.Create(21F, FontStyle.Bold);
            titleLabel.SetBounds(104, 17, 390, 42);
            subtitleLabel.Font = UiFonts.Create(9.6F, FontStyle.Regular);
            subtitleLabel.SetBounds(106, 58, 460, 28);

            languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageBox.SetBounds(594, 31, 138, 38);
            themeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            themeBox.SetBounds(742, 31, 130, 38);
            aboutButton.SetBounds(886, 27, 126, 46);

            header.Controls.Add(logo);
            header.Controls.Add(titleLabel);
            header.Controls.Add(subtitleLabel);
            header.Controls.Add(languageBox);
            header.Controls.Add(themeBox);
            header.Controls.Add(aboutButton);
            header.Resize += delegate { LayoutHeader(header); };

            var content = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(26, 4, 26, 22),
                BackColor = Color.Transparent
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 61.5F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38.5F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.SetBounds(0, 100, ClientSize.Width, ClientSize.Height - 100);
            content.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            scheduleCard.Dock = DockStyle.Fill;
            scheduleCard.Margin = new Padding(0, 0, 11, 0);
            statusCard.Dock = DockStyle.Fill;
            statusCard.Margin = new Padding(11, 0, 0, 0);
            content.Controls.Add(scheduleCard, 0, 0);
            content.Controls.Add(statusCard, 1, 0);

            BuildScheduleCard();
            BuildStatusCard();
            Controls.Add(header);
            Controls.Add(content);
        }

        private void BuildScheduleCard()
        {
            actionCaption.SetBounds(28, 22, 240, 28);
            actionBox.DropDownStyle = ComboBoxStyle.DropDownList;
            actionBox.SetBounds(28, 54, 520, 40);
            actionBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            scheduleCaption.SetBounds(28, 108, 240, 28);
            countdownMode.SetBounds(28, 139, 164, 32);
            clockMode.SetBounds(202, 139, 190, 32);

            countdownPanel.SetBounds(24, 176, 530, 48);
            countdownPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ConfigureNumber(hours, 4, 2, 168, 1);
            hours.Width = 92;
            hoursLabel.SetBounds(104, 8, 64, 30);
            ConfigureNumber(minutes, 176, 2, 59, 0);
            minutes.Width = 92;
            minutesLabel.SetBounds(276, 8, 68, 30);
            ConfigureNumber(seconds, 352, 2, 59, 0);
            seconds.Width = 92;
            secondsLabel.SetBounds(452, 8, 68, 30);
            countdownPanel.Controls.Add(hours);
            countdownPanel.Controls.Add(hoursLabel);
            countdownPanel.Controls.Add(minutes);
            countdownPanel.Controls.Add(minutesLabel);
            countdownPanel.Controls.Add(seconds);
            countdownPanel.Controls.Add(secondsLabel);

            clockPanel.SetBounds(28, 178, 520, 46);
            clockPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            clockPicker.SetBounds(0, 0, 520, 40);
            clockPicker.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            clockPanel.Controls.Add(clockPicker);

            repeatCaption.SetBounds(28, 239, 240, 28);
            repeatBox.DropDownStyle = ComboBoxStyle.DropDownList;
            repeatBox.SetBounds(28, 270, 238, 40);
            repeatHint.SetBounds(296, 266, 282, 58);
            repeatHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            optionsCaption.SetBounds(28, 332, 280, 28);
            forceClose.SetBounds(28, 365, 270, 32);
            preventSleep.SetBounds(312, 365, 270, 32);
            alwaysOnTop.SetBounds(28, 402, 270, 32);
            startWithWindows.SetBounds(312, 402, 270, 32);
            startMinimized.SetBounds(28, 439, 270, 32);

            confirmCaption.SetBounds(28, 484, 200, 32);
            confirmBox.DropDownStyle = ComboBoxStyle.DropDownList;
            confirmBox.SetBounds(235, 480, 210, 40);

            startButton.Primary = true;
            startButton.SetBounds(28, 548, 340, 48);
            startButton.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            cancelButton.Danger = true;
            cancelButton.SetBounds(384, 548, 164, 48);
            cancelButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;

            scheduleCard.Controls.Add(actionCaption);
            scheduleCard.Controls.Add(actionBox);
            scheduleCard.Controls.Add(scheduleCaption);
            scheduleCard.Controls.Add(countdownMode);
            scheduleCard.Controls.Add(clockMode);
            scheduleCard.Controls.Add(countdownPanel);
            scheduleCard.Controls.Add(clockPanel);
            scheduleCard.Controls.Add(repeatCaption);
            scheduleCard.Controls.Add(repeatBox);
            scheduleCard.Controls.Add(repeatHint);
            scheduleCard.Controls.Add(optionsCaption);
            scheduleCard.Controls.Add(forceClose);
            scheduleCard.Controls.Add(preventSleep);
            scheduleCard.Controls.Add(alwaysOnTop);
            scheduleCard.Controls.Add(startWithWindows);
            scheduleCard.Controls.Add(startMinimized);
            scheduleCard.Controls.Add(confirmCaption);
            scheduleCard.Controls.Add(confirmBox);
            scheduleCard.Controls.Add(startButton);
            scheduleCard.Controls.Add(cancelButton);
            scheduleCard.Resize += delegate { LayoutScheduleCard(); };
        }

        private void BuildStatusCard()
        {
            stateBadge.Font = UiFonts.Create(9.8F, FontStyle.Bold);
            stateBadge.TextAlign = ContentAlignment.MiddleCenter;
            stateBadge.SetBounds(28, 26, 230, 36);
            remainingCaption.TextAlign = ContentAlignment.MiddleCenter;
            remainingCaption.SetBounds(24, 96, 300, 30);
            remainingCaption.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            remainingLabel.Font = new Font("Cascadia Mono", 24F, FontStyle.Bold);
            remainingLabel.Text = "00:00:00";
            remainingLabel.TextAlign = ContentAlignment.MiddleCenter;
            remainingLabel.SetBounds(24, 126, 300, 78);
            remainingLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progress.SetBounds(32, 218, 284, 9);
            progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            nextCaption.SetBounds(30, 270, 280, 28);
            actionLabel.Font = UiFonts.Create(18F, FontStyle.Bold);
            actionLabel.SetBounds(30, 301, 290, 48);
            actionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            targetLabel.SetBounds(30, 352, 290, 66);
            targetLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            safetyHint.SetBounds(30, 437, 290, 94);
            safetyHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            historyButton.SetBounds(28, 548, 148, 48);
            historyButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            minimizeButton.SetBounds(188, 548, 140, 48);
            minimizeButton.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;

            statusCard.Controls.Add(stateBadge);
            statusCard.Controls.Add(remainingCaption);
            statusCard.Controls.Add(remainingLabel);
            statusCard.Controls.Add(progress);
            statusCard.Controls.Add(nextCaption);
            statusCard.Controls.Add(actionLabel);
            statusCard.Controls.Add(targetLabel);
            statusCard.Controls.Add(safetyHint);
            statusCard.Controls.Add(historyButton);
            statusCard.Controls.Add(minimizeButton);
            statusCard.Resize += delegate { LayoutStatusCard(); };
        }

        private void LayoutScheduleCard()
        {
            int width = Math.Max(520, scheduleCard.ClientSize.Width);
            actionBox.Width = width - 56;
            countdownPanel.Width = width - 48;
            clockPanel.Width = width - 56;
            clockPicker.Width = clockPanel.Width;
            repeatHint.Width = Math.Max(160, width - 320);
            int buttonY = Math.Max(540, scheduleCard.ClientSize.Height - 78);
            cancelButton.SetBounds(width - 192, buttonY, 164, 48);
            startButton.SetBounds(28, buttonY, Math.Max(210, width - 236), 48);
        }

        private void LayoutStatusCard()
        {
            int width = Math.Max(300, statusCard.ClientSize.Width);
            remainingCaption.SetBounds(18, 96, width - 36, 30);
            remainingLabel.SetBounds(18, 126, width - 36, 78);
            progress.SetBounds(28, 218, width - 56, 9);
            stateBadge.Width = Math.Max(180, width - 56);
            actionLabel.Width = width - 60;
            targetLabel.Width = width - 60;
            safetyHint.Width = width - 60;
            int buttonY = Math.Max(540, statusCard.ClientSize.Height - 78);
            int gap = 12;
            int buttonWidth = Math.Max(112, (width - 56 - gap) / 2);
            historyButton.SetBounds(28, buttonY, buttonWidth, 48);
            minimizeButton.SetBounds(28 + buttonWidth + gap, buttonY, buttonWidth, 48);
        }

        private static void ConfigureNumber(GlassNumberInput control, int x, int y, int maximum, int initial)
        {
            control.Minimum = 0;
            control.Maximum = maximum;
            control.Value = initial;
            control.SetBounds(x, y, 102, 40);
        }

        private void LayoutHeader(Panel header)
        {
            int width = header.ClientSize.Width;
            aboutButton.SetBounds(width - 154, 27, 126, 46);
            themeBox.SetBounds(width - 298, 31, 130, 38);
            languageBox.SetBounds(width - 446, 31, 138, 38);
            titleLabel.Width = Math.Max(300, width - 560);
            subtitleLabel.Width = Math.Max(350, width - 570);
        }

        private void BuildTray()
        {
            var menu = new ContextMenuStrip();
            menu.Items.Add(trayShow);
            menu.Items.Add(trayCancel);
            menu.Items.Add(trayHistory);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(trayExit);
            trayIcon.Icon = AssetLoader.LoadAppIcon();
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            trayShow.Click += delegate { ShowFromTray(); };
            trayCancel.Click += CancelSchedule;
            trayHistory.Click += delegate { ShowHistory(); };
            trayExit.Click += delegate { ExitApplication(); };
            trayIcon.DoubleClick += delegate { ShowFromTray(); };
        }

        private void WireEvents()
        {
            timer.Interval = 250;
            timer.Tick += OnTimerTick;
            countdownMode.CheckedChanged += delegate { UpdateScheduleMode(); };
            clockMode.CheckedChanged += delegate { UpdateScheduleMode(); };
            repeatBox.SelectedIndexChanged += delegate { UpdateRepeatHint(); };
            actionBox.SelectedIndexChanged += delegate { if (!running) UpdateStatusText(); };
            startButton.Click += StartSchedule;
            cancelButton.Click += CancelSchedule;
            historyButton.Click += delegate { ShowHistory(); };
            minimizeButton.Click += delegate { HideToTray(); };
            aboutButton.Click += delegate { using (var dialog = new AboutForm(palette)) dialog.ShowDialog(this); };
            languageBox.SelectedIndexChanged += LanguageChanged;
            themeBox.SelectedIndexChanged += ThemeChanged;
            startWithWindows.CheckedChanged += StartupChanged;
            forceClose.CheckedChanged += SaveOptionChanged;
            preventSleep.CheckedChanged += SaveOptionChanged;
            alwaysOnTop.CheckedChanged += SaveOptionChanged;
            startMinimized.CheckedChanged += SaveOptionChanged;
            confirmBox.SelectedIndexChanged += SaveOptionChanged;
            Resize += delegate { if (WindowState == FormWindowState.Minimized) HideToTray(); };
            FormClosing += OnFormClosing;
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.H) { ShowHistory(); e.Handled = true; }
            };
        }

        private void ApplySavedSettings()
        {
            AppSettings settings = store.State.Settings;
            actionBox.SelectedIndex = (int)settings.LastAction;
            countdownMode.Checked = settings.LastScheduleMode == ScheduleMode.Countdown;
            clockMode.Checked = settings.LastScheduleMode == ScheduleMode.Clock;
            long duration = Math.Max(1, settings.LastDurationSeconds);
            hours.Value = Math.Min(hours.Maximum, duration / 3600);
            minutes.Value = (duration % 3600) / 60;
            seconds.Value = duration % 60;
            TimeSpan time = TimeSpan.FromTicks(Math.Max(0, Math.Min(TimeSpan.TicksPerDay - 1, settings.LastClockTicks)));
            clockPicker.Value = DateTime.Today.Add(time);
            repeatBox.SelectedIndex = (int)settings.LastRepeatMode;
            forceClose.Checked = settings.ForceClose;
            preventSleep.Checked = settings.PreventSleep;
            alwaysOnTop.Checked = settings.AlwaysOnTop;
            startWithWindows.Checked = StartupManager.IsEnabled();
            startMinimized.Checked = settings.StartMinimized;
            languageBox.SelectedIndex = (int)settings.Language;
            themeBox.SelectedIndex = (int)settings.Theme;
            SelectConfirmation(settings.ConfirmationSeconds);
        }

        private void ApplyLaunchOptions()
        {
            if (launchOptions.Seconds.HasValue)
            {
                int value = launchOptions.Seconds.Value;
                countdownMode.Checked = true;
                hours.Value = Math.Min(hours.Maximum, value / 3600);
                minutes.Value = (value % 3600) / 60;
                seconds.Value = value % 60;
            }
            if (launchOptions.ClockTime.HasValue)
            {
                clockMode.Checked = true;
                clockPicker.Value = DateTime.Today.Add(launchOptions.ClockTime.Value);
            }
            if (launchOptions.Action.HasValue) actionBox.SelectedIndex = (int)launchOptions.Action.Value;
            if (launchOptions.Repeat.HasValue) repeatBox.SelectedIndex = (int)launchOptions.Repeat.Value;
            if (launchOptions.Force) forceClose.Checked = true;
        }

        private void ApplyLocalization()
        {
            refreshingLocalization = true;
            try
            {
            ApplyTypography();
            Text = L.T("AppTitle");
            titleLabel.Text = L.T("AppTitle");
            subtitleLabel.Text = L.T("Subtitle");
            aboutButton.Text = L.T("About");
            actionCaption.Text = L.T("Action");
            scheduleCaption.Text = L.T("Schedule");
            countdownMode.Text = L.T("Countdown");
            clockMode.Text = L.T("Clock");
            hoursLabel.Text = L.T("Hours");
            minutesLabel.Text = L.T("Minutes");
            secondsLabel.Text = L.T("Seconds");
            repeatCaption.Text = L.T("Repeat");
            optionsCaption.Text = L.T("Options");
            forceClose.Text = L.T("ForceClose");
            preventSleep.Text = L.T("PreventSleep");
            alwaysOnTop.Text = L.T("AlwaysOnTop");
            startWithWindows.Text = L.T("StartWithWindows");
            startMinimized.Text = L.T("StartMinimized");
            confirmCaption.Text = L.T("Confirm");
            startButton.Text = L.T("Start");
            cancelButton.Text = L.T("Cancel");
            remainingCaption.Text = L.T("Remaining");
            nextCaption.Text = L.T("NextAction");
            safetyHint.Text = L.T("SafetyHint");
            historyButton.Text = L.T("History");
            minimizeButton.Text = L.T("Minimize");
            trayShow.Text = L.T("TrayShow");
            trayCancel.Text = L.T("TrayCancel");
            trayHistory.Text = L.T("TrayHistory");
            trayExit.Text = L.T("TrayExit");

            RefreshComboItems();
            UpdateRepeatHint();
            UpdateStatusText();
            }
            finally
            {
                refreshingLocalization = false;
            }
        }

        private void ApplyTypography()
        {
            Font = UiFonts.Create(10F, FontStyle.Regular);
            titleLabel.Font = UiFonts.Create(21F, FontStyle.Bold);
            subtitleLabel.Font = UiFonts.Create(9.6F, FontStyle.Regular);
            actionCaption.Font = scheduleCaption.Font = repeatCaption.Font = optionsCaption.Font = confirmCaption.Font = UiFonts.Create(10.2F, FontStyle.Bold);
            remainingCaption.Font = nextCaption.Font = UiFonts.Create(10F, FontStyle.Regular);
            repeatHint.Font = UiFonts.Create(8.9F, FontStyle.Regular);
            targetLabel.Font = safetyHint.Font = UiFonts.Create(9.2F, FontStyle.Regular);
            stateBadge.Font = UiFonts.Create(9.8F, FontStyle.Bold);
            actionLabel.Font = UiFonts.Create(18F, FontStyle.Bold);
            aboutButton.Font = startButton.Font = cancelButton.Font = historyButton.Font = minimizeButton.Font = UiFonts.Create(10F, FontStyle.Bold);
            languageBox.Font = themeBox.Font = actionBox.Font = repeatBox.Font = confirmBox.Font = UiFonts.Create(10F, FontStyle.Regular);
            countdownMode.Font = clockMode.Font = forceClose.Font = preventSleep.Font = alwaysOnTop.Font = startWithWindows.Font = startMinimized.Font = UiFonts.Create(9.6F, FontStyle.Regular);
            hours.Font = minutes.Font = seconds.Font = clockPicker.Font = UiFonts.Create(10F, FontStyle.Regular);

            remainingLabel.Font = new Font("Cascadia Mono", 24F, FontStyle.Bold);
            remainingLabel.AutoEllipsis = false;
            foreach (Label label in new[] { titleLabel, subtitleLabel, actionCaption, scheduleCaption, hoursLabel, minutesLabel, secondsLabel,
                repeatCaption, repeatHint, optionsCaption, confirmCaption, stateBadge, remainingCaption, nextCaption,
                actionLabel, targetLabel, safetyHint })
            {
                label.AutoEllipsis = true;
                label.UseCompatibleTextRendering = false;
            }
            repeatHint.AutoEllipsis = targetLabel.AutoEllipsis = safetyHint.AutoEllipsis = false;
        }

        private void RefreshComboItems()
        {
            int action = Math.Max(0, actionBox.SelectedIndex);
            int repeat = Math.Max(0, repeatBox.SelectedIndex);
            int confirmation = SelectedConfirmation();
            int language = Math.Max(0, languageBox.SelectedIndex);
            int theme = Math.Max(0, themeBox.SelectedIndex);

            actionBox.Items.Clear();
            foreach (PowerAction item in Enum.GetValues(typeof(PowerAction))) actionBox.Items.Add(L.Action(item));
            actionBox.SelectedIndex = Math.Min(action, actionBox.Items.Count - 1);
            repeatBox.Items.Clear();
            foreach (RepeatMode item in Enum.GetValues(typeof(RepeatMode))) repeatBox.Items.Add(L.Repeat(item));
            repeatBox.SelectedIndex = Math.Min(repeat, repeatBox.Items.Count - 1);
            confirmBox.Items.Clear();
            foreach (int value in ConfirmationValues())
                confirmBox.Items.Add(value == 0 ? L.T("ConfirmOff") : string.Format(L.T("ConfirmSeconds"), value));
            SelectConfirmation(confirmation);
            languageBox.Items.Clear();
            languageBox.Items.Add(L.T("Chinese"));
            languageBox.Items.Add(L.T("English"));
            languageBox.SelectedIndex = Math.Min(language, 1);
            themeBox.Items.Clear();
            foreach (AppTheme item in Enum.GetValues(typeof(AppTheme))) themeBox.Items.Add(L.Theme(item));
            themeBox.SelectedIndex = Math.Min(theme, 2);
        }

        private void ApplyTheme()
        {
            palette = ThemePalette.Create(ThemeDetector.UseDark(store.State.Settings.Theme));
            ThemeApplier.Apply(this, palette);
            NativeTheme.ApplyWindowEffects(this, palette.IsDark);
            titleLabel.ForeColor = palette.Text;
            subtitleLabel.ForeColor = palette.Muted;
            repeatHint.ForeColor = palette.Muted;
            targetLabel.ForeColor = palette.Muted;
            safetyHint.ForeColor = palette.Muted;
            remainingCaption.ForeColor = palette.Muted;
            nextCaption.ForeColor = palette.Muted;
            remainingLabel.ForeColor = palette.Accent;
            actionLabel.ForeColor = palette.Text;
            SetStateBadge();
            aboutButton.Palette = startButton.Palette = cancelButton.Palette = historyButton.Palette = minimizeButton.Palette = palette;
            progress.Palette = palette;
            Invalidate(true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            AtmosphereRenderer.Draw(e.Graphics, ClientRectangle, palette ?? ThemePalette.Create(false));
        }

        private void LanguageChanged(object sender, EventArgs e)
        {
            if (initializing || refreshingLocalization || languageBox.SelectedIndex < 0) return;
            store.State.Settings.Language = (AppLanguage)languageBox.SelectedIndex;
            L.Language = store.State.Settings.Language;
            SaveSettings();
            ApplyLocalization();
            ApplyTheme();
        }

        private void ThemeChanged(object sender, EventArgs e)
        {
            if (initializing || refreshingLocalization || themeBox.SelectedIndex < 0) return;
            store.State.Settings.Theme = (AppTheme)themeBox.SelectedIndex;
            SaveSettings();
            ApplyTheme();
        }

        private void StartupChanged(object sender, EventArgs e)
        {
            if (initializing) return;
            try
            {
                StartupManager.SetEnabled(startWithWindows.Checked);
                store.State.Settings.StartWithWindows = startWithWindows.Checked;
                SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Error("Startup setting failed", ex);
                initializing = true;
                startWithWindows.Checked = !startWithWindows.Checked;
                initializing = false;
                MessageBox.Show(this, ex.Message, L.T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateScheduleMode()
        {
            countdownPanel.Visible = countdownMode.Checked;
            clockPanel.Visible = clockMode.Checked;
            if (!initializing && countdownMode.Checked) repeatBox.SelectedIndex = 0;
            repeatBox.Enabled = !countdownMode.Checked && !running;
            UpdateRepeatHint();
        }

        private void SaveOptionChanged(object sender, EventArgs e)
        {
            if (!initializing && !refreshingLocalization) SaveSettings();
        }

        private void UpdateRepeatHint()
        {
            repeatHint.Text = repeatBox.SelectedIndex <= 0 ? L.T("OneTimeHint") : L.T("RecurringHint");
        }

        private void StartSchedule(object sender, EventArgs e)
        {
            if (running) return;
            try
            {
                DateTime now = DateTime.Now;
                ScheduleMode mode = countdownMode.Checked ? ScheduleMode.Countdown : ScheduleMode.Clock;
                TimeSpan duration = mode == ScheduleMode.Countdown
                    ? TimerLogic.ClampDuration(hours.Value, minutes.Value, seconds.Value)
                    : TimeSpan.Zero;
                RepeatMode repeat = (RepeatMode)Math.Max(0, repeatBox.SelectedIndex);
                DateTime target = TimerLogic.CalculateTarget(now, mode, duration, clockPicker.Value.TimeOfDay, repeat);
                activeSchedule = new ActiveSchedule
                {
                    StartedLocalTicks = now.Ticks,
                    TargetLocalTicks = target.Ticks,
                    TimeOfDayTicks = target.TimeOfDay.Ticks,
                    Action = (PowerAction)Math.Max(0, actionBox.SelectedIndex),
                    Repeat = repeat,
                    ForceClose = forceClose.Checked,
                    PreventSleep = preventSleep.Checked,
                    ConfirmationSeconds = SelectedConfirmation()
                };
                store.State.ActiveSchedule = activeSchedule;
                SaveSettings();
                store.Save();
                BeginRunning(false);
                trayIcon.ShowBalloonTip(2500, L.T("StartedBalloon"), string.Format(L.T("TargetTime"), target.ToString("yyyy-MM-dd HH:mm:ss")), ToolTipIcon.Info);
                Logger.Info("Schedule started: " + activeSchedule.Action + " at " + target.ToString("o"));
            }
            catch (Exception ex)
            {
                Logger.Error("Schedule start failed", ex);
                MessageBox.Show(this, string.Format(L.T("StartFailed"), ex.Message), L.T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RestoreSchedule()
        {
            activeSchedule = store.State.ActiveSchedule;
            if (activeSchedule == null) { UpdateStatusText(); return; }
            if (activeSchedule.TargetLocal <= DateTime.Now)
            {
                if (activeSchedule.Repeat == RepeatMode.Once)
                {
                    store.AddHistory(activeSchedule.Action, "missed", "MissedDetail");
                    store.State.ActiveSchedule = null;
                    store.Save();
                    activeSchedule = null;
                    UpdateStatusText();
                    return;
                }
                store.AddHistory(activeSchedule.Action, "missed", "MissedDetail");
                activeSchedule.StartedLocalTicks = DateTime.Now.Ticks;
                activeSchedule.TargetLocal = TimerLogic.NormalizeOccurrence(DateTime.Now, activeSchedule.TimeOfDay, activeSchedule.Repeat);
                store.Save();
            }
            BeginRunning(true);
        }

        private void BeginRunning(bool restored)
        {
            running = true;
            timer.Start();
            PowerController.KeepAwake(activeSchedule.PreventSleep);
            SetEditingEnabled(false);
            TopMost = alwaysOnTop.Checked;
            stateBadge.Text = restored ? L.T("Restored") : L.T("Running");
            UpdateStatusText();
            OnTimerTick(null, EventArgs.Empty);
        }

        private void CancelSchedule(object sender, EventArgs e)
        {
            if (!running || activeSchedule == null) return;
            PowerAction action = activeSchedule.Action;
            StopRunning(true);
            store.AddHistory(action, "cancelled", "ManualCancel");
            trayIcon.ShowBalloonTip(1800, L.T("CancelledBalloon"), L.T("CancelledBody"), ToolTipIcon.Info);
            Logger.Info("Schedule cancelled by user");
        }

        private void StopRunning(bool clearSchedule)
        {
            timer.Stop();
            PowerController.KeepAwake(false);
            running = false;
            TopMost = false;
            if (clearSchedule)
            {
                activeSchedule = null;
                store.State.ActiveSchedule = null;
                store.Save();
            }
            SetEditingEnabled(true);
            remainingLabel.Text = "00:00:00";
            progress.Value = 0;
            UpdateStatusText();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            if (!running || activeSchedule == null) return;
            DateTime now = DateTime.Now;
            TimeSpan remaining = activeSchedule.TargetLocal - now;
            if (remaining <= TimeSpan.Zero)
            {
                timer.Stop();
                PowerController.KeepAwake(false);
                remainingLabel.Text = "00:00:00";
                progress.Value = 100;
                HandleDueSchedule();
                return;
            }

            remainingLabel.Text = TimerLogic.FormatRemaining(remaining);
            progress.Value = TimerLogic.ProgressPercent(activeSchedule.StartedLocal, activeSchedule.TargetLocal, now);
            remainingLabel.ForeColor = remaining.TotalMinutes < 1 ? palette.Danger : remaining.TotalMinutes < 10 ? Color.FromArgb(234, 88, 12) : palette.Accent;
            string trayText = TimerLogic.FormatRemaining(remaining) + " · " + L.Action(activeSchedule.Action);
            trayIcon.Text = trayText.Substring(0, Math.Min(63, trayText.Length));
        }

        private void HandleDueSchedule()
        {
            PowerAction action = activeSchedule.Action;
            bool shouldExecute = true;
            if (activeSchedule.ConfirmationSeconds > 0)
            {
                ShowFromTray();
                using (var prompt = new ExecutionPromptForm(action, activeSchedule.ConfirmationSeconds, palette))
                    shouldExecute = prompt.ShowDialog(this) == DialogResult.OK;
            }

            if (!shouldExecute)
            {
                store.AddHistory(action, "skipped", "ConfirmationCancel");
                AdvanceOrStop();
                return;
            }

            bool currentDryRun = dryRun;
            store.AddHistory(action, currentDryRun ? "dryRun" : "executed", currentDryRun ? "DryRunDetail" : string.Empty);
            AdvanceOrStop();
            try
            {
                PowerController.Execute(action, forceClose.Checked, currentDryRun);
            }
            catch (Exception ex)
            {
                Logger.Error("Power action failed: " + action, ex);
                store.AddHistory(action, "failed", ex.Message);
                MessageBox.Show(this, string.Format(L.T("ExecuteFailed"), ex.Message), L.T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AdvanceOrStop()
        {
            if (activeSchedule != null && activeSchedule.Repeat != RepeatMode.Once)
            {
                activeSchedule.StartedLocalTicks = DateTime.Now.Ticks;
                activeSchedule.TargetLocal = TimerLogic.NormalizeOccurrence(DateTime.Now, activeSchedule.TimeOfDay, activeSchedule.Repeat);
                store.State.ActiveSchedule = activeSchedule;
                store.Save();
                running = true;
                PowerController.KeepAwake(activeSchedule.PreventSleep);
                timer.Start();
                UpdateStatusText();
            }
            else
            {
                StopRunning(true);
            }
        }

        private void SetEditingEnabled(bool enabled)
        {
            actionBox.Enabled = countdownMode.Enabled = clockMode.Enabled = enabled;
            hours.Enabled = minutes.Enabled = seconds.Enabled = clockPicker.Enabled = enabled;
            repeatBox.Enabled = enabled && clockMode.Checked;
            forceClose.Enabled = preventSleep.Enabled = enabled;
            alwaysOnTop.Enabled = confirmBox.Enabled = enabled;
            startButton.Enabled = enabled;
            cancelButton.Enabled = !enabled;
            trayCancel.Enabled = !enabled;
        }

        private void UpdateStatusText()
        {
            if (running && activeSchedule != null)
            {
                if (string.IsNullOrEmpty(stateBadge.Text) || stateBadge.Text == L.T("Idle")) stateBadge.Text = L.T("Running");
                actionLabel.Text = L.Action(activeSchedule.Action);
                targetLabel.Text = string.Format(L.T("TargetTime"), activeSchedule.TargetLocal.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                stateBadge.Text = L.T("Idle");
                actionLabel.Text = actionBox.SelectedIndex >= 0 ? L.Action((PowerAction)actionBox.SelectedIndex) : L.T("Shutdown");
                targetLabel.Text = L.T("OneTimeHint");
            }
            SetStateBadge();
            trayIcon.Text = L.T("AppTitle").Substring(0, Math.Min(63, L.T("AppTitle").Length));
        }

        private void SetStateBadge()
        {
            if (palette == null) return;
            stateBadge.BackColor = running ? palette.AccentSoft : palette.SurfaceAlt;
            stateBadge.ForeColor = running ? palette.Accent : palette.Muted;
        }

        private void SaveSettings()
        {
            AppSettings settings = store.State.Settings;
            settings.Language = L.Language;
            if (themeBox.SelectedIndex >= 0) settings.Theme = (AppTheme)themeBox.SelectedIndex;
            settings.AlwaysOnTop = alwaysOnTop.Checked;
            settings.ForceClose = forceClose.Checked;
            settings.PreventSleep = preventSleep.Checked;
            settings.StartWithWindows = startWithWindows.Checked;
            settings.StartMinimized = startMinimized.Checked;
            settings.ConfirmationSeconds = SelectedConfirmation();
            if (actionBox.SelectedIndex >= 0) settings.LastAction = (PowerAction)actionBox.SelectedIndex;
            settings.LastScheduleMode = countdownMode.Checked ? ScheduleMode.Countdown : ScheduleMode.Clock;
            if (repeatBox.SelectedIndex >= 0) settings.LastRepeatMode = (RepeatMode)repeatBox.SelectedIndex;
            settings.LastDurationSeconds = (long)hours.Value * 3600L + (long)minutes.Value * 60L + (long)seconds.Value;
            settings.LastClockTicks = clockPicker.Value.TimeOfDay.Ticks;
            store.Save();
        }

        private static int[] ConfirmationValues() { return new[] { 0, 5, 15, 30, 60 }; }

        private int SelectedConfirmation()
        {
            int[] values = ConfirmationValues();
            int index = confirmBox.SelectedIndex;
            if (index < 0 || index >= values.Length) return store.State.Settings.ConfirmationSeconds;
            return values[index];
        }

        private void SelectConfirmation(int secondsValue)
        {
            int[] values = ConfirmationValues();
            int bestIndex = 0;
            int bestDistance = int.MaxValue;
            for (int index = 0; index < values.Length; index++)
            {
                int distance = Math.Abs(values[index] - secondsValue);
                if (distance < bestDistance) { bestDistance = distance; bestIndex = index; }
            }
            confirmBox.SelectedIndex = bestIndex;
        }

        private void ShowHistory()
        {
            using (var dialog = new HistoryForm(store, palette)) dialog.ShowDialog(this);
        }

        private void HideToTray()
        {
            Hide();
            ShowInTaskbar = false;
        }

        private void ShowFromTray()
        {
            ShowInTaskbar = true;
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitApplication()
        {
            if (running && MessageBox.Show(this, L.T("ConfirmExit"), L.T("Question"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            if (running) StopRunning(true);
            SaveSettings();
            allowExit = true;
            Close();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (allowExit || e.CloseReason == CloseReason.WindowsShutDown) return;
            e.Cancel = true;
            HideToTray();
        }

        internal void RunUiSmokeTest()
        {
            languageBox.SelectedIndex = languageBox.SelectedIndex == 0 ? 1 : 0;
            themeBox.SelectedIndex = (int)AppTheme.Dark;
            clockMode.Checked = true;
            repeatBox.SelectedIndex = (int)RepeatMode.Weekdays;
            countdownMode.Checked = true;
            themeBox.SelectedIndex = (int)AppTheme.Light;
        }

        internal void DrawPreviewOverlays(Bitmap bitmap)
        {
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                DrawComboPreview(graphics, languageBox);
                DrawComboPreview(graphics, themeBox);
                DrawComboPreview(graphics, actionBox);
                DrawComboPreview(graphics, repeatBox);
                DrawComboPreview(graphics, confirmBox);
            }
        }

        private void DrawComboPreview(Graphics graphics, ComboBox combo)
        {
            if (combo.SelectedIndex < 0) return;
            Point point = combo.Location;
            Control parent = combo.Parent;
            while (parent != null && parent != this)
            {
                point.Offset(parent.Location);
                parent = parent.Parent;
            }
            ThemePalette p = palette ?? ThemePalette.Create(false);
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            Rectangle field = new Rectangle(point.X, point.Y, Math.Max(1, combo.Width - 1), Math.Max(1, combo.Height - 1));
            using (var outside = new SolidBrush(p.Surface)) graphics.FillRectangle(outside, new Rectangle(point, combo.Size));
            using (var path = UiShape.Rounded(field, 10))
            using (var background = new SolidBrush(p.Field))
            using (var border = new Pen(p.Border))
            {
                graphics.FillPath(background, path);
                graphics.DrawPath(border, path);
            }
            Rectangle bounds = new Rectangle(point.X + 13, point.Y + 1, Math.Max(1, combo.Width - 52), combo.Height - 2);
            Color color = combo.Enabled ? p.Text : p.Muted;
            TextRenderer.DrawText(graphics, combo.GetItemText(combo.SelectedItem), combo.Font, bounds, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            int centerX = point.X + combo.Width - 20;
            int centerY = point.Y + combo.Height / 2;
            using (var pen = new Pen(combo.Enabled ? p.Muted : p.Border, 1.6F))
            {
                graphics.DrawLine(pen, centerX - 4, centerY - 2, centerX, centerY + 2);
                graphics.DrawLine(pen, centerX, centerY + 2, centerX + 4, centerY - 2);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                PowerController.KeepAwake(false);
                timer.Dispose();
                trayIcon.Visible = false;
                trayIcon.Dispose();
                if (logo.Image != null) logo.Image.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
