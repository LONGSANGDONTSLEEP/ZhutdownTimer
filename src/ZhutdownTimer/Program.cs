using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ZhutdownTimer
{
    internal static class Program
    {
        private static Mutex instanceMutex;

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

            if (options.RenderPreview)
            {
                RenderPreview(options);
                return;
            }

            bool createdNew;
            instanceMutex = new Mutex(true, @"Local\ZhutdownTimer-1F3450C9-726B-4B8B-BC25-3572CE8DF835", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("Zhutdown Timer is already running. Check the system tray.\n\n定时电源助手已经在运行，请检查系统托盘。",
                    "Zhutdown Timer", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                Logger.Error("Unhandled UI exception", e.Exception);
                MessageBox.Show(e.Exception.Message, L.T("ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                Logger.Error("Unhandled application exception", e.ExceptionObject as Exception);
            };

            try
            {
                var store = new SettingsStore();
                Application.Run(new MainForm(store, options));
            }
            catch (Exception ex)
            {
                Logger.Error("Application startup failed", ex);
                MessageBox.Show(ex.Message, "Zhutdown Timer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (instanceMutex != null) instanceMutex.Dispose();
            }
        }

        private static void RunSelfTest()
        {
            if (TimerLogic.FormatRemaining(TimeSpan.FromSeconds(3661)) != "01:01:01") Environment.Exit(1);
            DateTime friday = new DateTime(2026, 8, 21, 23, 0, 0);
            DateTime weekday = TimerLogic.NormalizeOccurrence(friday, new TimeSpan(22, 0, 0), RepeatMode.Weekdays);
            if (weekday.DayOfWeek != DayOfWeek.Monday) Environment.Exit(2);
            DateTime weekend = TimerLogic.NormalizeOccurrence(friday, new TimeSpan(22, 0, 0), RepeatMode.Weekends);
            if (weekend.DayOfWeek != DayOfWeek.Saturday) Environment.Exit(8);
            if (TimerLogic.ProgressPercent(friday, friday.AddHours(2), friday.AddHours(1)) != 50) Environment.Exit(3);
            LaunchOptions parsed = LaunchOptions.Parse(new[] { "--seconds", "30", "--action", "restart", "--repeat", "weekdays", "--start" });
            if (parsed.Action != PowerAction.Restart || parsed.Repeat != RepeatMode.Weekdays || parsed.Seconds != 30 || !parsed.Start) Environment.Exit(4);

            string temporaryDirectory = Path.Combine(Path.GetTempPath(), "ZhutdownTimer-selftest-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SettingsStore(temporaryDirectory);
                store.State.Settings.Language = AppLanguage.English;
                store.AddHistory(PowerAction.Lock, "executed", string.Empty);
                var reloaded = new SettingsStore(temporaryDirectory);
                if (reloaded.State.Settings.Language != AppLanguage.English || reloaded.State.History.Count != 1) Environment.Exit(5);

                using (Image logo = AssetLoader.LoadLogo())
                {
                    if (logo.Width < 256 || logo.Height < 256) Environment.Exit(6);
                }
                using (var form = new MainForm(reloaded, new LaunchOptions()))
                {
                    if (string.IsNullOrEmpty(form.Text)) Environment.Exit(7);
                    form.RunUiSmokeTest();
                }
                ThemePalette palette = ThemePalette.Create(false);
                using (var prompt = new ExecutionPromptForm(PowerAction.Lock, 5, palette))
                    if (string.IsNullOrEmpty(prompt.Text)) Environment.Exit(9);
                using (var history = new HistoryForm(reloaded, palette))
                    if (string.IsNullOrEmpty(history.Text)) Environment.Exit(10);
                using (var about = new AboutForm(palette))
                    if (string.IsNullOrEmpty(about.Text)) Environment.Exit(11);
            }
            finally
            {
                try { if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true); }
                catch { }
            }
            Console.WriteLine("Self-test passed.");
        }

        private static void RenderPreview(LaunchOptions options)
        {
            string temporaryDirectory = Path.Combine(Path.GetTempPath(), "ZhutdownTimer-preview-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new SettingsStore(temporaryDirectory);
                store.State.Settings.Language = options.Language.HasValue ? options.Language.Value : AppLanguage.Chinese;
                store.State.Settings.Theme = options.Theme.HasValue ? options.Theme.Value : AppTheme.Light;
                using (var form = new MainForm(store, options))
                {
                    form.Show();
                    Application.DoEvents();
                    using (var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height))
                    {
                        using (Graphics graphics = Graphics.FromImage(bitmap))
                        {
                            graphics.Clear(form.BackColor);
                        }
                        foreach (Control control in form.Controls)
                            control.DrawToBitmap(bitmap, control.Bounds);
                        form.DrawPreviewOverlays(bitmap);
                        string suffix = store.State.Settings.Language == AppLanguage.English ? "en" : "zh";
                        if (suffix == "zh")
                        {
                            using (Graphics titleGraphics = Graphics.FromImage(bitmap))
                            using (Font titleFont = new Font("Microsoft YaHei UI", 20.5F, FontStyle.Regular, GraphicsUnit.Point))
                            using (var titleBrush = new SolidBrush(ThemePalette.Create(ThemeDetector.UseDark(store.State.Settings.Theme)).Text))
                            using (var titleFormat = new StringFormat(StringFormat.GenericTypographic))
                            {
                                titleGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                                titleFormat.FormatFlags |= StringFormatFlags.NoWrap;
                                using (var titlePath = new System.Drawing.Drawing2D.GraphicsPath())
                                {
                                    float emSize = titleFont.SizeInPoints * titleGraphics.DpiY / 72F;
                                    titlePath.AddString("定时电源助手", titleFont.FontFamily, (int)titleFont.Style,
                                        emSize, new PointF(104, 8), titleFormat);
                                    titleGraphics.FillPath(titleBrush, titlePath);
                                }
                            }
                        }
                        string path = Path.Combine(Environment.CurrentDirectory, "ui-preview-" + suffix + ".png");
                        bitmap.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                        Console.WriteLine(path);
                    }
                }
            }
            finally
            {
                try { if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true); }
                catch { }
            }
        }
    }
}
