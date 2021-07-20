using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Calendars.Abstractions
{
    /// <summary>
    /// Platform-agnostic convenience functions
    /// </summary>
    public static class ICalendarsExtensions
    {
        /// <summary>
        /// Creates a new calendar with the specified name and optional color.
        /// (just a convenience wrapper around AddOrUpdateCalendarAsync)
        /// </summary>
        /// <param name="api">ICalendars instance to extend</param>
        /// <param name="calendarName">Calendar name</param>
        /// <param name="color">Preferred color, or null to accept default</param>
        /// <returns>The created calendar</returns>
        public static async Task<Calendar> CreateCalendarAsync(this ICalendars api, string calendarName, string? color = null)
        {
            var calendar = new Calendar
            {
                Name = calendarName,
                Color = color,
                CanEditCalendar = true,
                CanEditEvents = true
            };

            await api.AddOrUpdateCalendarAsync(calendar).ConfigureAwait(false);

            return calendar;
        }
    }
}
