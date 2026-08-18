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
        private readonly RadioButton countdownMode = new RadioButton();
        private readonly RadioButton clockMode = new RadioButton();
        private readonly Panel countdownPanel = new Panel();
        private readonly Panel clockPanel = new Panel();
        private readonly NumericUpDown hours = new NumericUpDown();
        private readonly NumericUpDown minutes = new NumericUpDown();
        private readonly NumericUpDown seconds = new NumericUpDown();
        private readonly Label hoursLabel = new Label();
        private readonly Label minutesLabel = new Label();
        private readonly Label secondsLabel = new Label();
        private readonly DateTimePicker clockPicker = new DateTimePicker();
        private readonly Label repeatCaption = new Label();
        private readonly ModernComboBox repeatBox = new ModernComboBox();
        private readonly Label optionsCaption = new Label();
        private readonly CheckBox forceClose = new CheckBox();
        private readonly CheckBox preventSleep = new CheckBox();
        private readonly CheckBox alwaysOnTop = new CheckBox();
        private readonly CheckBox startWithWindows = new CheckBox();
        private readonly CheckBox startMinimized = new CheckBox();
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
            ClientSize = new Size(980, 720);
            MinimumSize = new Size(900, 680);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
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
            var header = new Panel { Height = 86, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            header.SetBounds(0, 0, ClientSize.Width, 86);
            logo.Image = AssetLoader.LoadLogo();
            logo.SizeMode = PictureBoxSizeMode.Zoom;
            logo.SetBounds(24, 16, 54, 54);
            titleLabel.Font = new Font(Font.FontFamily, 19F, FontStyle.Bold);
            titleLabel.SetBounds(92, 15, 350, 36);
            subtitleLabel.SetBounds(94, 52, 430, 24);

            languageBox.DropDownStyle = ComboBoxStyle.DropDownList;
            languageBox.SetBounds(560, 27, 110, 30);
            themeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            themeBox.SetBounds(680, 27, 120, 30);
            aboutButton.SetBounds(812, 23, 112, 38);

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
                Padding = new Padding(24, 4, 24, 18)
            };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 59F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 41F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.SetBounds(0, 86, ClientSize.Width, ClientSize.Height - 86);
            content.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            scheduleCard.Dock = DockStyle.Fill;
            scheduleCard.Margin = new Padding(0, 0, 10, 0);
            statusCard.Dock = DockStyle.Fill;
            statusCard.Margin = new Padding(10, 0, 0, 0);
            content.Controls.Add(scheduleCard, 0, 0);
            content.Controls.Add(statusCard, 1, 0);

            BuildScheduleCard();
            BuildStatusCard();
            Controls.Add(header);
            Controls.Add(content);
        }

        private void BuildScheduleCard()
        {
            actionCaption.SetBounds(24, 20, 220, 22);
            actionBox.DropDownStyle = ComboBoxStyle.DropDownList;
            actionBox.SetBounds(24, 47, 470, 34);
            actionBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            scheduleCaption.SetBounds(24, 96, 220, 22);
            countdownMode.SetBounds(24, 123, 132, 26);
            clockMode.SetBounds(170, 123, 160, 26);

            countdownPanel.SetBounds(20, 154, 480, 54);
            countdownPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            ConfigureNumber(hours, 4, 5, 168, 1);
            hours.Width = 80;
            hoursLabel.SetBounds(90, 12, 62, 22);
            ConfigureNumber(minutes, 160, 5, 59, 0);
            minutes.Width = 80;
            minutesLabel.SetBounds(246, 12, 72, 22);
            ConfigureNumber(seconds, 326, 5, 59, 0);
            seconds.Width = 80;
            secondsLabel.SetBounds(412, 12, 68, 22);
            countdownPanel.Controls.Add(hours);
            countdownPanel.Controls.Add(hoursLabel);
            countdownPanel.Controls.Add(minutes);
            countdownPanel.Controls.Add(minutesLabel);
            countdownPanel.Controls.Add(seconds);
            countdownPanel.Controls.Add(secondsLabel);

            clockPanel.SetBounds(24, 158, 470, 48);
            clockPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            clockPicker.Format = DateTimePickerFormat.Custom;
            clockPicker.CustomFormat = "HH:mm:ss";
            clockPicker.ShowUpDown = true;
            clockPicker.SetBounds(0, 2, 470, 34);
            clockPicker.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            clockPanel.Controls.Add(clockPicker);

            repeatCaption.SetBounds(24, 217, 220, 22);
            repeatBox.DropDownStyle = ComboBoxStyle.DropDownList;
            repeatBox.SetBounds(24, 244, 220, 34);
            repeatHint.SetBounds(260, 242, 235, 48);
            repeatHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            optionsCaption.SetBounds(24, 299, 250, 22);
            forceClose.SetBounds(24, 328, 245, 26);
            preventSleep.SetBounds(278, 328, 225, 26);
            alwaysOnTop.SetBounds(24, 358, 245, 26);
            startWithWindows.SetBounds(278, 358, 225, 26);
            startMinimized.SetBounds(24, 388, 245, 26);

            confirmCaption.SetBounds(24, 428, 180, 22);
            confirmBox.DropDownStyle = ComboBoxStyle.DropDownList;
            confirmBox.SetBounds(205, 424, 190, 32);

            startButton.Primary = true;
            startButton.SetBounds(24, 484, 310, 44);
            startButton.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
            cancelButton.Danger = true;
            cancelButton.SetBounds(348, 484, 146, 44);
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
            stateBadge.Font = new Font(Font.FontFamily, 9F, FontStyle.Bold);
            stateBadge.TextAlign = ContentAlignment.MiddleCenter;
            stateBadge.SetBounds(24, 28, 220, 32);
            remainingCaption.TextAlign = ContentAlignment.MiddleCenter;
            remainingCaption.SetBounds(24, 94, 300, 24);
            remainingCaption.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            remainingLabel.Font = new Font("Consolas", 26F, FontStyle.Bold);
            remainingLabel.Text = "00:00:00";
            remainingLabel.TextAlign = ContentAlignment.MiddleCenter;
            remainingLabel.SetBounds(24, 118, 300, 76);
            remainingLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progress.SetBounds(32, 204, 284, 9);
            progress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            nextCaption.SetBounds(28, 252, 280, 22);
            actionLabel.Font = new Font(Font.FontFamily, 17F, FontStyle.Bold);
            actionLabel.SetBounds(28, 278, 290, 40);
            actionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            targetLabel.SetBounds(28, 322, 290, 58);
            targetLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            safetyHint.SetBounds(28, 392, 290, 84);
            safetyHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            historyButton.SetBounds(24, 484, 148, 44);
            historyButton.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            minimizeButton.SetBounds(184, 484, 140, 44);
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
            int width = Math.Max(360, scheduleCard.ClientSize.Width);
            actionBox.Width = width - 48;
            countdownPanel.Width = width - 40;
            clockPanel.Width = width - 48;
            clockPicker.Width = clockPanel.Width;
            repeatHint.Width = Math.Max(120, width - 284);
            int buttonY = Math.Max(470, scheduleCard.ClientSize.Height - 68);
            cancelButton.SetBounds(width - 194, buttonY, 170, 44);
            startButton.SetBounds(24, buttonY, Math.Max(180, width - 232), 44);
        }

        private void LayoutStatusCard()
        {
            int width = Math.Max(300, statusCard.ClientSize.Width);
            remainingCaption.SetBounds(16, 94, width - 32, 24);
            remainingLabel.SetBounds(16, 118, width - 32, 76);
            progress.SetBounds(24, 204, width - 48, 9);
            actionLabel.Width = width - 56;
            targetLabel.Width = width - 56;
            safetyHint.Width = width - 56;
            int buttonY = Math.Max(470, statusCard.ClientSize.Height - 68);
            int gap = 12;
            int buttonWidth = Math.Max(110, (width - 48 - gap) / 2);
            historyButton.SetBounds(24, buttonY, buttonWidth, 44);
            minimizeButton.SetBounds(24 + buttonWidth + gap, buttonY, buttonWidth, 44);
        }

        private static void ConfigureNumber(NumericUpDown control, int x, int y, int maximum, int initial)
        {
            control.Minimum = 0;
            control.Maximum = maximum;
            control.Value = initial;
            control.TextAlign = HorizontalAlignment.Center;
            control.SetBounds(x, y, 102, 32);
        }

        private void LayoutHeader(Panel header)
        {
            int width = header.ClientSize.Width;
            aboutButton.SetBounds(width - 136, 23, 112, 38);
            themeBox.SetBounds(width - 270, 27, 120, 30);
            languageBox.SetBounds(width - 394, 27, 110, 30);
            titleLabel.Width = Math.Max(260, width - 510);
            subtitleLabel.Width = Math.Max(320, width - 520);
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
            NativeTheme.ApplyDarkTitleBar(this, palette.IsDark);
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
            Rectangle field = new Rectangle(point.X + 2, point.Y + 2, Math.Max(1, combo.Width - 31), combo.Height - 4);
            using (var background = new SolidBrush(combo.Enabled ? Color.White : Color.FromArgb(245, 245, 245)))
                graphics.FillRectangle(background, field);
            Rectangle bounds = new Rectangle(point.X + 6, point.Y + 2, Math.Max(1, combo.Width - 38), combo.Height - 4);
            Color color = combo.Enabled ? Color.FromArgb(30, 41, 59) : Color.FromArgb(100, 116, 139);
            TextRenderer.DrawText(graphics, combo.GetItemText(combo.SelectedItem), combo.Font, bounds, color,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
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
