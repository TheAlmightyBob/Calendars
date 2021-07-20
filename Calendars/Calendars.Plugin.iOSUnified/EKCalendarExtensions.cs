using Plugin.Calendars.Abstractions;

using EventKit;

#nullable enable

namespace Plugin.Calendars
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
            System.Console.WriteLine($"Calendar: {ekCalendar.Title}, Source: {ekCalendar.Source.Title}, {ekCalendar.Source.SourceType}");

            return new Calendar
            {
                Name = ekCalendar.Title,
                ExternalID = ekCalendar.CalendarIdentifier,
                CanEditCalendar = !ekCalendar.Immutable,
                CanEditEvents = ekCalendar.AllowsContentModifications,
                Color = ColorConversion.ToHexColor(ekCalendar.CGColor),
                AccountName = ekCalendar.Source.Title
            };
        }
    }
}