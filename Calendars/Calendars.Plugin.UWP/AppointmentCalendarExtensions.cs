using Plugin.Calendars.Abstractions;
using Windows.ApplicationModel.Appointments;

#nullable enable

namespace Plugin.Calendars
{
    /// <summary>
    /// AppointmentCalendar extensions
    /// </summary>
    internal static class AppointmentCalendarExtensions
    {
        /// <summary>
        /// Creates a new Calendars.Plugin.Abstractions.Calendar from an AppointmentCalendar
        /// </summary>
        /// <param name="apptCalendar">Source AppointmentCalendar</param>
        /// <param name="writable">Whether or not the calendar is writable (this isn't part of AppointmentCalendar)</param>
        /// <returns>Corresponding Calendars.Plugin.Abstractions.Calendar</returns>
        public static Calendar ToCalendar(this AppointmentCalendar apptCalendar, bool writable) => new()
        {
            Name = apptCalendar.DisplayName,
            Color = apptCalendar.DisplayColor.ToString(),
            ExternalID = apptCalendar.LocalId,
            CanEditCalendar = writable,
            CanEditEvents = writable,
            AccountName = apptCalendar.SourceDisplayName
        };
    }
}
