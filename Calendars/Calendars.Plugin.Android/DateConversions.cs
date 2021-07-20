using System;

#nullable enable

namespace Plugin.Calendars
{
    /// <summary>
    /// Date conversion helpers.
    /// </summary>
    internal static class DateConversions
    {
        private readonly static DateTime _reference = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts a System.DateTime to an Android-expected long of milliseconds since 1970.
        /// </summary>
        /// <returns>The date as Android long value.</returns>
        /// <param name="date">Source DateTime.</param>
        public static long GetDateAsAndroidMS(DateTime date)
        {
            return (long)(date.ToUniversalTime() - _reference).TotalMilliseconds;
        }

        /// <summary>
        /// Converts an Android long value of milliseconds since 1970 to a System.DateTime
        /// </summary>
        /// <returns>The System.DateTime.</returns>
        /// <param name="ms">Source date as milliseconds since 1970.</param>
        public static DateTime GetDateFromAndroidMS(long ms)
        {
            return _reference.AddMilliseconds(ms).ToLocalTime();
        }
    }
}