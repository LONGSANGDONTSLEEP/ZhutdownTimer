using System;

namespace ZhutdownTimer
{
    internal static class TimerLogic
    {
        public static DateTime NextOccurrence(DateTime now, TimeSpan timeOfDay)
        {
            DateTime target = now.Date.Add(timeOfDay);
            return target <= now ? target.AddDays(1) : target;
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
    }
}

