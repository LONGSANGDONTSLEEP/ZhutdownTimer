using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ZhutdownTimer
{
    internal enum PowerAction
    {
        Shutdown,
        Restart,
        Sleep,
        Hibernate,
        Lock,
        SignOut
    }

    internal enum ScheduleMode
    {
        Countdown,
        Clock
    }

    internal enum RepeatMode
    {
        Once,
        Daily,
        Weekdays,
        Weekends
    }

    internal enum AppLanguage
    {
        Chinese,
        English
    }

    internal enum AppTheme
    {
        System,
        Light,
        Dark
    }

    [DataContract]
    internal sealed class AppSettings
    {
        [DataMember] public AppLanguage Language = AppLanguage.Chinese;
        [DataMember] public AppTheme Theme = AppTheme.System;
        [DataMember] public bool AlwaysOnTop = true;
        [DataMember] public bool ForceClose;
        [DataMember] public bool PreventSleep = true;
        [DataMember] public bool StartWithWindows;
        [DataMember] public bool StartMinimized;
        [DataMember] public int ConfirmationSeconds = 15;
        [DataMember] public PowerAction LastAction = PowerAction.Shutdown;
        [DataMember] public ScheduleMode LastScheduleMode = ScheduleMode.Countdown;
        [DataMember] public RepeatMode LastRepeatMode = RepeatMode.Once;
        [DataMember] public long LastDurationSeconds = 3600;
        [DataMember] public long LastClockTicks = TimeSpan.TicksPerHour * 23;
    }

    [DataContract]
    internal sealed class ActiveSchedule
    {
        [DataMember] public long StartedLocalTicks;
        [DataMember] public long TargetLocalTicks;
        [DataMember] public long TimeOfDayTicks;
        [DataMember] public PowerAction Action;
        [DataMember] public RepeatMode Repeat;
        [DataMember] public bool ForceClose;
        [DataMember] public bool PreventSleep;
        [DataMember] public int ConfirmationSeconds;

        public DateTime StartedLocal
        {
            get { return new DateTime(StartedLocalTicks, DateTimeKind.Local); }
        }

        public DateTime TargetLocal
        {
            get { return new DateTime(TargetLocalTicks, DateTimeKind.Local); }
            set { TargetLocalTicks = value.Ticks; }
        }

        public TimeSpan TimeOfDay
        {
            get { return TimeSpan.FromTicks(TimeOfDayTicks); }
        }
    }

    [DataContract]
    internal sealed class HistoryEntry
    {
        [DataMember] public long TimestampLocalTicks;
        [DataMember] public PowerAction Action;
        [DataMember] public string ResultCode;
        [DataMember] public string Details;

        public DateTime TimestampLocal
        {
            get { return new DateTime(TimestampLocalTicks, DateTimeKind.Local); }
        }
    }

    [DataContract]
    internal sealed class PersistedState
    {
        [DataMember] public AppSettings Settings = new AppSettings();
        [DataMember] public ActiveSchedule ActiveSchedule;
        [DataMember] public List<HistoryEntry> History = new List<HistoryEntry>();
    }

    internal sealed class LaunchOptions
    {
        public int? Seconds;
        public TimeSpan? ClockTime;
        public PowerAction? Action;
        public RepeatMode? Repeat;
        public AppLanguage? Language;
        public AppTheme? Theme;
        public bool Start;
        public bool Background;
        public bool Force;
        public bool DryRun;
        public bool SelfTest;
        public bool RenderPreview;

        public static LaunchOptions Parse(string[] args)
        {
            var result = new LaunchOptions();
            for (int index = 0; index < args.Length; index++)
            {
                string arg = args[index].ToLowerInvariant();
                if (arg == "/selftest" || arg == "--self-test") result.SelfTest = true;
                else if (arg == "--render-preview") result.RenderPreview = true;
                else if (arg == "/start" || arg == "--start") result.Start = true;
                else if (arg == "/background" || arg == "--background") result.Background = true;
                else if (arg == "/force" || arg == "--force") result.Force = true;
                else if (arg == "/dryrun" || arg == "--dry-run") result.DryRun = true;
                else if ((arg == "/seconds" || arg == "--seconds") && index + 1 < args.Length)
                {
                    int value;
                    if (int.TryParse(args[++index], out value) && value > 0) result.Seconds = value;
                }
                else if ((arg == "/time" || arg == "--time") && index + 1 < args.Length)
                {
                    TimeSpan value;
                    if (TimeSpan.TryParse(args[++index], out value) && value >= TimeSpan.Zero && value < TimeSpan.FromDays(1)) result.ClockTime = value;
                }
                else if ((arg == "/action" || arg == "--action") && index + 1 < args.Length)
                {
                    PowerAction value;
                    if (Enum.TryParse(args[++index], true, out value)) result.Action = value;
                }
                else if ((arg == "/repeat" || arg == "--repeat") && index + 1 < args.Length)
                {
                    RepeatMode value;
                    if (Enum.TryParse(args[++index], true, out value)) result.Repeat = value;
                }
                else if ((arg == "/lang" || arg == "--lang") && index + 1 < args.Length)
                {
                    string value = args[++index].ToLowerInvariant();
                    if (value == "zh" || value == "zh-cn") result.Language = AppLanguage.Chinese;
                    if (value == "en" || value == "en-us") result.Language = AppLanguage.English;
                }
                else if ((arg == "/theme" || arg == "--theme") && index + 1 < args.Length)
                {
                    AppTheme value;
                    if (Enum.TryParse(args[++index], true, out value)) result.Theme = value;
                }
            }
            return result;
        }
    }
}
