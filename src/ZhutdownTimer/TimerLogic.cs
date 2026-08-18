using System;

namespace ZhutdownTimer
{
    internal static class TimerLogic
    {
        public static DateTime NextOccurrence(DateTime now, TimeSpan timeOfDay, RepeatMode repeat)
        {
            return NormalizeOccurrence(now, timeOfDay, repeat);
        }

        public static DateTime NormalizeOccurrence(DateTime now, TimeSpan timeOfDay, RepeatMode repeat)
        {
            DateTime target = now.Date.Add(timeOfDay);
            if (target <= now)
                target = target.AddDays(1);

            while (!IsAllowedDay(target.DayOfWeek, repeat))
                target = target.AddDays(1);

            return target;
        }

        public static bool IsAllowedDay(DayOfWeek day, RepeatMode repeat)
        {
            if (repeat == RepeatMode.Weekdays)
                return day != DayOfWeek.Saturday && day != DayOfWeek.Sunday;
            if (repeat == RepeatMode.Weekends)
                return day == DayOfWeek.Saturday || day == DayOfWeek.Sunday;
            return true;
        }

        public static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            int hours = (int)remaining.TotalHours;
            return string.Format("{0:00}:{1:00}:{2:00}", hours, remaining.Minutes, remaining.Seconds);
        }

        public static TimeSpan ClampDuration(decimal hours, decimal minutes, decimal seconds)
        {
            long totalSeconds = (long)hours * 3600L + (long)minutes * 60L + (long)seconds;
            if (totalSeconds < 1)
                throw new ArgumentOutOfRangeException("duration", "倒计时至少需要 1 秒。 ");
            return TimeSpan.FromSeconds(totalSeconds);
        }

        public static DateTime CalculateTarget(DateTime now, ScheduleMode mode, TimeSpan duration, TimeSpan timeOfDay, RepeatMode repeat)
        {
            if (mode == ScheduleMode.Countdown)
                return now.Add(duration);
            return NormalizeOccurrence(now, timeOfDay, repeat);
        }

        public static int ProgressPercent(DateTime started, DateTime target, DateTime now)
        {
            double total = (target - started).TotalMilliseconds;
            if (total <= 0) return 100;
            double elapsed = (now - started).TotalMilliseconds;
            return Math.Max(0, Math.Min(100, (int)Math.Round(elapsed / total * 100.0)));
        }
    }
}
