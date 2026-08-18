using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;

namespace ZhutdownTimer
{
    internal sealed class SettingsStore
    {
        private readonly string statePath;

        public SettingsStore() : this(AppPaths.DataDirectory)
        {
        }

        internal SettingsStore(string dataDirectory)
        {
            Directory.CreateDirectory(dataDirectory);
            statePath = Path.Combine(dataDirectory, "state.json");
            State = Load();
        }

        public PersistedState State { get; private set; }

        private PersistedState Load()
        {
            if (!File.Exists(statePath)) return new PersistedState();
            try
            {
                using (FileStream stream = File.OpenRead(statePath))
                {
                    var serializer = new DataContractJsonSerializer(typeof(PersistedState));
                    var state = serializer.ReadObject(stream) as PersistedState;
                    if (state == null) return new PersistedState();
                    if (state.Settings == null) state.Settings = new AppSettings();
                    if (state.History == null) state.History = new List<HistoryEntry>();
                    return state;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("State load failed", ex);
                try
                {
                    string backup = statePath + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    File.Copy(statePath, backup, false);
                }
                catch { }
                return new PersistedState();
            }
        }

        public void Save()
        {
            string temporaryPath = statePath + ".tmp";
            using (FileStream stream = File.Create(temporaryPath))
            {
                var serializer = new DataContractJsonSerializer(typeof(PersistedState));
                serializer.WriteObject(stream, State);
                stream.Flush(true);
            }

            if (File.Exists(statePath))
            {
                try
                {
                    File.Replace(temporaryPath, statePath, statePath + ".bak", true);
                    return;
                }
                catch (PlatformNotSupportedException) { }
                catch (IOException) { }
                File.Copy(temporaryPath, statePath, true);
                File.Delete(temporaryPath);
            }
            else
            {
                File.Move(temporaryPath, statePath);
            }
        }

        public void AddHistory(PowerAction action, string resultCode, string details)
        {
            State.History.Insert(0, new HistoryEntry
            {
                TimestampLocalTicks = DateTime.Now.Ticks,
                Action = action,
                ResultCode = resultCode,
                Details = details ?? string.Empty
            });
            if (State.History.Count > 100)
                State.History.RemoveRange(100, State.History.Count - 100);
            Save();
        }

        public void ClearHistory()
        {
            State.History.Clear();
            Save();
        }
    }

    internal static class AppPaths
    {
        public static readonly string DataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ZhutdownTimer");

        public static readonly string LogDirectory = Path.Combine(DataDirectory, "logs");
    }

    internal static class Logger
    {
        private static readonly object Sync = new object();

        public static void Info(string message) { Write("INFO", message, null); }
        public static void Error(string message, Exception exception) { Write("ERROR", message, exception); }

        private static void Write(string level, string message, Exception exception)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(AppPaths.LogDirectory);
                    string path = Path.Combine(AppPaths.LogDirectory, DateTime.Now.ToString("yyyy-MM") + ".log");
                    var line = new StringBuilder();
                    line.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    line.Append(" [").Append(level).Append("] ").Append(message);
                    if (exception != null) line.Append(" | ").Append(exception);
                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }
    }

    internal static class StartupManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "ZhutdownTimer";

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (key == null) throw new InvalidOperationException("Windows startup registry key is unavailable.");
                if (enabled)
                    key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\" --background");
                else
                    key.DeleteValue(ValueName, false);
            }
        }

        public static bool IsEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKey, false))
            {
                return key != null && key.GetValue(ValueName) != null;
            }
        }
    }

    internal static class PowerController
    {
        [DllImport("user32.dll")]
        private static extern bool LockWorkStation();

        [DllImport("kernel32.dll")]
        private static extern uint SetThreadExecutionState(uint executionState);

        private const uint EsContinuous = 0x80000000;
        private const uint EsSystemRequired = 0x00000001;

        public static void KeepAwake(bool enabled)
        {
            SetThreadExecutionState(enabled ? EsContinuous | EsSystemRequired : EsContinuous);
        }

        public static void Execute(PowerAction action, bool forceClose, bool dryRun)
        {
            if (dryRun)
            {
                Logger.Info("Dry run: " + action);
                return;
            }

            switch (action)
            {
                case PowerAction.Shutdown:
                    StartShutdown("/s /t 0" + (forceClose ? " /f" : string.Empty));
                    break;
                case PowerAction.Restart:
                    StartShutdown("/r /t 0" + (forceClose ? " /f" : string.Empty));
                    break;
                case PowerAction.SignOut:
                    StartShutdown("/l" + (forceClose ? " /f" : string.Empty));
                    break;
                case PowerAction.Sleep:
                    if (!Application.SetSuspendState(PowerState.Suspend, forceClose, false))
                        throw new InvalidOperationException("Windows rejected the sleep request.");
                    break;
                case PowerAction.Hibernate:
                    if (!Application.SetSuspendState(PowerState.Hibernate, forceClose, false))
                        throw new InvalidOperationException("Windows rejected the hibernation request.");
                    break;
                case PowerAction.Lock:
                    if (!LockWorkStation())
                        throw new InvalidOperationException("Windows rejected the lock request.");
                    break;
            }
        }

        private static void StartShutdown(string arguments)
        {
            Process.Start(new ProcessStartInfo("shutdown.exe", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
    }

    internal static class ThemeDetector
    {
        public static bool UseDark(AppTheme theme)
        {
            if (theme == AppTheme.Dark) return true;
            if (theme == AppTheme.Light) return false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key == null ? null : key.GetValue("AppsUseLightTheme");
                    return value is int && (int)value == 0;
                }
            }
            catch { return false; }
        }
    }

    internal static class NativeTheme
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

        public static void ApplyDarkTitleBar(Form form, bool dark)
        {
            try
            {
                int value = dark ? 1 : 0;
                int result = DwmSetWindowAttribute(form.Handle, 20, ref value, sizeof(int));
                if (result != 0) DwmSetWindowAttribute(form.Handle, 19, ref value, sizeof(int));
            }
            catch { }
        }
    }

    internal static class AssetLoader
    {
        public static Image LoadLogo()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ZhutdownTimer.AppIcon.png");
            if (stream == null) return SystemIcons.Application.ToBitmap();
            using (stream)
            using (var source = new Bitmap(stream))
                return new Bitmap(source);
        }

        public static Icon LoadAppIcon()
        {
            try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { return SystemIcons.Application; }
        }
    }
}
