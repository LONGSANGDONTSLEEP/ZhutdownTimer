using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ZhutdownTimer
{
    internal enum PowerAction
    {
        Shutdown,
        Restart,
        Sleep,
        Hibernate,
        Lock
    }

    internal sealed class LaunchOptions
    {
        public int? Seconds;
        public PowerAction Action = PowerAction.Shutdown;
        public bool Start;
        public bool Background;
        public bool SelfTest;

        public static LaunchOptions Parse(string[] args)
        {
            var result = new LaunchOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLowerInvariant();
                if (arg == "/selftest" || arg == "--self-test") result.SelfTest = true;
                else if (arg == "/start" || arg == "--start") result.Start = true;
                else if (arg == "/background" || arg == "--background") result.Background = true;
                else if ((arg == "/seconds" || arg == "--seconds") && i + 1 < args.Length)
                {
                    int seconds;
                    if (int.TryParse(args[++i], out seconds) && seconds > 0) result.Seconds = seconds;
                }
                else if ((arg == "/action" || arg == "--action") && i + 1 < args.Length)
                {
                    PowerAction action;
                    if (Enum.TryParse(args[++i], true, out action)) result.Action = action;
                }
            }
            return result;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            LaunchOptions options = LaunchOptions.Parse(args);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (options.SelfTest)
            {
                RunSelfTest();
                return;
            }

            Application.Run(new MainForm(options));
        }

        private static void RunSelfTest()
        {
            if (TimerLogic.FormatRemaining(TimeSpan.FromSeconds(3661)) != "01:01:01") Environment.Exit(1);
            DateTime now = new DateTime(2026, 8, 19, 23, 0, 0);
            if (TimerLogic.NextOccurrence(now, new TimeSpan(22, 0, 0)).Date != now.AddDays(1).Date) Environment.Exit(2);
            if (LaunchOptions.Parse(new[] { "--seconds", "30", "--action", "restart", "--start" }).Action != PowerAction.Restart) Environment.Exit(3);
            using (var form = new MainForm(new LaunchOptions()))
            {
                if (form.Text != "定时电源助手") Environment.Exit(4);
            }
            Console.WriteLine("Self-test passed.");
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly ComboBox actionBox = new ComboBox();
        private readonly RadioButton countdownMode = new RadioButton();
        private readonly RadioButton clockMode = new RadioButton();
        private readonly NumericUpDown hours = new NumericUpDown();
        private readonly NumericUpDown minutes = new NumericUpDown();
        private readonly NumericUpDown seconds = new NumericUpDown();
        private readonly DateTimePicker clock = new DateTimePicker();
        private readonly CheckBox forceClose = new CheckBox();
        private readonly CheckBox alwaysOnTop = new CheckBox();
        private readonly CheckBox runInBackground = new CheckBox();
        private readonly Button startButton = new Button();
        private readonly Button cancelButton = new Button();
        private readonly Label remainingLabel = new Label();
        private readonly Label statusLabel = new Label();
        private readonly Timer timer = new Timer();
        private readonly NotifyIcon trayIcon = new NotifyIcon();
        private readonly ToolStripMenuItem trayShow = new ToolStripMenuItem("显示主窗口");
        private readonly ToolStripMenuItem trayCancel = new ToolStripMenuItem("取消倒计时");
        private DateTime targetTime;
        private bool running;
        private bool allowExit;

        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint executionState);

        private const uint EsContinuous = 0x80000000;
        private const uint EsSystemRequired = 0x00000001;

        public MainForm(LaunchOptions options)
        {
            Text = "定时电源助手";
            ClientSize = new Size(520, 500);
            MinimumSize = new Size(536, 539);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Microsoft YaHei UI", 9F);
            Icon = SystemIcons.Application;
            BackColor = Color.FromArgb(246, 248, 252);

            BuildInterface();
            BuildTrayMenu();

            timer.Interval = 250;
            timer.Tick += OnTimerTick;
            FormClosing += OnFormClosing;
            Resize += OnResize;

            if (options.Seconds.HasValue)
            {
                int value = options.Seconds.Value;
                hours.Value = Math.Min(hours.Maximum, value / 3600);
                minutes.Value = (value % 3600) / 60;
                seconds.Value = value % 60;
            }
            actionBox.SelectedIndex = (int)options.Action;
            runInBackground.Checked = options.Background;
            if (options.Start) StartCountdown(null, EventArgs.Empty);
        }

        private void BuildInterface()
        {
            var title = new Label
            {
                Text = "定时电源助手",
                Font = new Font(Font.FontFamily, 22F, FontStyle.Bold),
                ForeColor = Color.FromArgb(31, 41, 55),
                Location = new Point(30, 22),
                AutoSize = true
            };
            var subtitle = new Label
            {
                Text = "到点执行关机、重启、睡眠等操作",
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(33, 66),
                AutoSize = true
            };
            Controls.Add(title);
            Controls.Add(subtitle);

            AddCaption("执行操作", 30, 105);
            actionBox.DropDownStyle = ComboBoxStyle.DropDownList;
            actionBox.Items.AddRange(new object[] { "关机", "重启", "睡眠", "休眠", "锁定" });
            actionBox.SelectedIndex = 0;
            actionBox.SetBounds(30, 130, 460, 32);
            Controls.Add(actionBox);

            countdownMode.Text = "倒计时";
            countdownMode.Checked = true;
            countdownMode.SetBounds(30, 178, 85, 25);
            clockMode.Text = "指定时刻";
            clockMode.SetBounds(125, 178, 100, 25);
            countdownMode.CheckedChanged += delegate { UpdateModeControls(); };
            clockMode.CheckedChanged += delegate { UpdateModeControls(); };
            Controls.Add(countdownMode);
            Controls.Add(clockMode);

            ConfigureNumber(hours, 30, 215, 168, "小时", 1);
            ConfigureNumber(minutes, 180, 215, 59, "分钟", 0);
            ConfigureNumber(seconds, 330, 215, 59, "秒", 0);

            clock.Format = DateTimePickerFormat.Custom;
            clock.CustomFormat = "HH:mm:ss";
            clock.ShowUpDown = true;
            clock.Value = DateTime.Now.AddHours(1);
            clock.SetBounds(30, 215, 460, 30);
            clock.Visible = false;
            Controls.Add(clock);

            forceClose.Text = "强制关闭未响应的程序（可能丢失未保存内容）";
            forceClose.SetBounds(30, 270, 440, 25);
            alwaysOnTop.Text = "倒计时期间窗口置顶";
            alwaysOnTop.Checked = true;
            alwaysOnTop.SetBounds(30, 300, 220, 25);
            runInBackground.Text = "启动后隐藏到系统托盘";
            runInBackground.SetBounds(270, 300, 220, 25);
            Controls.Add(forceClose);
            Controls.Add(alwaysOnTop);
            Controls.Add(runInBackground);

            remainingLabel.Text = "00:00:00";
            remainingLabel.Font = new Font("Consolas", 28F, FontStyle.Bold);
            remainingLabel.ForeColor = Color.FromArgb(37, 99, 235);
            remainingLabel.TextAlign = ContentAlignment.MiddleCenter;
            remainingLabel.SetBounds(30, 338, 460, 56);
            Controls.Add(remainingLabel);

            statusLabel.Text = "尚未启动";
            statusLabel.ForeColor = Color.FromArgb(100, 116, 139);
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.SetBounds(30, 394, 460, 24);
            Controls.Add(statusLabel);

            startButton.Text = "开始倒计时";
            startButton.BackColor = Color.FromArgb(37, 99, 235);
            startButton.ForeColor = Color.White;
            startButton.FlatStyle = FlatStyle.Flat;
            startButton.FlatAppearance.BorderSize = 0;
            startButton.SetBounds(30, 435, 300, 40);
            startButton.Click += StartCountdown;
            cancelButton.Text = "取消";
            cancelButton.Enabled = false;
            cancelButton.SetBounds(345, 435, 145, 40);
            cancelButton.Click += CancelCountdown;
            Controls.Add(startButton);
            Controls.Add(cancelButton);
            AcceptButton = startButton;
        }

        private void AddCaption(string text, int x, int y)
        {
            Controls.Add(new Label { Text = text, ForeColor = Color.FromArgb(71, 85, 105), Location = new Point(x, y), AutoSize = true });
        }

        private void ConfigureNumber(NumericUpDown control, int x, int y, int maximum, string suffix, int initial)
        {
            control.Minimum = 0;
            control.Maximum = maximum;
            control.Value = initial;
            control.TextAlign = HorizontalAlignment.Center;
            control.SetBounds(x, y, 105, 30);
            Controls.Add(control);
            Controls.Add(new Label { Text = suffix, Location = new Point(x + 110, y + 5), AutoSize = true });
        }

        private void BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();
            trayShow.Click += delegate { ShowFromTray(); };
            trayCancel.Enabled = false;
            trayCancel.Click += CancelCountdown;
            var exit = new ToolStripMenuItem("退出");
            exit.Click += delegate { ExitApplication(); };
            menu.Items.Add(trayShow);
            menu.Items.Add(trayCancel);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exit);
            trayIcon.Text = "定时电源助手";
            trayIcon.Icon = SystemIcons.Application;
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { ShowFromTray(); };
        }

        private void UpdateModeControls()
        {
            hours.Visible = minutes.Visible = seconds.Visible = countdownMode.Checked;
            foreach (Control control in Controls)
            {
                if (control is Label && (control.Text == "小时" || control.Text == "分钟" || control.Text == "秒"))
                    control.Visible = countdownMode.Checked;
            }
            clock.Visible = clockMode.Checked;
        }

        private void StartCountdown(object sender, EventArgs e)
        {
            if (running) return;
            try
            {
                targetTime = countdownMode.Checked
                    ? DateTime.Now.Add(TimerLogic.ClampDuration(hours.Value, minutes.Value, seconds.Value))
                    : TimerLogic.NextOccurrence(DateTime.Now, clock.Value.TimeOfDay);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                MessageBox.Show(ex.Message, "无法启动", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            running = true;
            timer.Start();
            SetThreadExecutionState(EsContinuous | EsSystemRequired);
            SetControlsEnabled(false);
            TopMost = alwaysOnTop.Checked;
            statusLabel.Text = "计划执行：" + targetTime.ToString("yyyy-MM-dd HH:mm:ss") + " · " + actionBox.SelectedItem;
            trayIcon.Text = "定时电源助手 - " + actionBox.SelectedItem;
            trayIcon.ShowBalloonTip(2500, "倒计时已启动", statusLabel.Text, ToolTipIcon.Info);
            if (runInBackground.Checked) HideToTray();
            OnTimerTick(null, EventArgs.Empty);
        }

        private void CancelCountdown(object sender, EventArgs e)
        {
            if (!running) return;
            StopTimer();
            remainingLabel.Text = "00:00:00";
            statusLabel.Text = "倒计时已取消";
            trayIcon.ShowBalloonTip(1800, "已取消", "不会执行电源操作。", ToolTipIcon.Info);
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            TimeSpan remaining = targetTime - DateTime.Now;
            if (remaining <= TimeSpan.Zero)
            {
                timer.Stop();
                remainingLabel.Text = "00:00:00";
                ExecuteSelectedAction();
                return;
            }

            remainingLabel.Text = TimerLogic.FormatRemaining(remaining);
            if (remaining.TotalMinutes < 1) remainingLabel.ForeColor = Color.FromArgb(220, 38, 38);
            else if (remaining.TotalMinutes < 10) remainingLabel.ForeColor = Color.FromArgb(234, 88, 12);
            else remainingLabel.ForeColor = Color.FromArgb(37, 99, 235);
            trayIcon.Text = ("剩余 " + TimerLogic.FormatRemaining(remaining) + " - 定时电源助手").Substring(0, Math.Min(63, ("剩余 " + TimerLogic.FormatRemaining(remaining) + " - 定时电源助手").Length));
        }

        private void ExecuteSelectedAction()
        {
            PowerAction action = (PowerAction)actionBox.SelectedIndex;
            try
            {
                switch (action)
                {
                    case PowerAction.Shutdown:
                        StartSystemCommand("/s /t 0" + (forceClose.Checked ? " /f" : ""));
                        break;
                    case PowerAction.Restart:
                        StartSystemCommand("/r /t 0" + (forceClose.Checked ? " /f" : ""));
                        break;
                    case PowerAction.Sleep:
                        Application.SetSuspendState(PowerState.Suspend, forceClose.Checked, false);
                        break;
                    case PowerAction.Hibernate:
                        Application.SetSuspendState(PowerState.Hibernate, forceClose.Checked, false);
                        break;
                    case PowerAction.Lock:
                        LockWorkStation();
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("执行失败：" + ex.Message, "定时电源助手", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                StopTimer();
            }
        }

        private static void StartSystemCommand(string arguments)
        {
            Process.Start(new ProcessStartInfo("shutdown.exe", arguments) { UseShellExecute = false, CreateNoWindow = true });
        }

        private void SetControlsEnabled(bool enabled)
        {
            actionBox.Enabled = countdownMode.Enabled = clockMode.Enabled = enabled;
            hours.Enabled = minutes.Enabled = seconds.Enabled = clock.Enabled = enabled;
            forceClose.Enabled = alwaysOnTop.Enabled = runInBackground.Enabled = enabled;
            startButton.Enabled = enabled;
            cancelButton.Enabled = !enabled;
            trayCancel.Enabled = !enabled;
        }

        private void StopTimer()
        {
            timer.Stop();
            running = false;
            SetThreadExecutionState(EsContinuous);
            TopMost = false;
            SetControlsEnabled(true);
            trayIcon.Text = "定时电源助手";
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

        private void OnResize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) HideToTray();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (allowExit) return;
            if (running)
            {
                DialogResult result = MessageBox.Show("倒计时仍在运行。要取消倒计时并退出吗？", "确认退出", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
                StopTimer();
            }
            trayIcon.Visible = false;
        }

        private void ExitApplication()
        {
            allowExit = false;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
                trayIcon.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
