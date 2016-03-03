using System;
namespace Plugin.Calendars.TestUtilities
{
    static class DateTimeExtensions
    {
        public static DateTime RoundToMS(this DateTime dt)
        {
            return dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerMillisecond));
        }

        public static DateTime RoundToSeconds(this DateTime dt)
        {
            return dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerSecond));
        }

        public static DateTime RoundToMinutes(this DateTime dt)
        {
            return dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerMinute));
        }
    }
}

