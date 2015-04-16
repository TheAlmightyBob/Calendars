using Calendars.Plugin.Abstractions;

#if __UNIFIED__
using EventKit;
#else
using MonoTouch.EventKit;
#endif

namespace Calendars.Plugin
{
    /// <summary>
    /// iOS EKCalendar extensions
    /// </summary>
    internal static class EKCalendarExtensions
    {
        /// <summary>
        /// Creates a new Calendars.Plugin.Abstractions.Calendar from an EKCalendar
        /// </summary>
        /// <param name="ekCalendar">Source EKCalendar</param>
        /// <returns>Corresponding Calendars.Plugin.Abstractions.Calendar</returns>
        public static Calendar ToCalendar(this EKCalendar ekCalendar)
        {
            return new Calendar
                {
                    Name = ekCalendar.Title,
                    ExternalID = ekCalendar.CalendarIdentifier,
                    CanEditCalendar = ekCalendar.AllowsContentModifications,
                    CanEditEvents = ekCalendar.AllowsContentModifications,
                    Color = ColorConversion.ToHexColor(ekCalendar.CGColor)
                };
        }
    }
}