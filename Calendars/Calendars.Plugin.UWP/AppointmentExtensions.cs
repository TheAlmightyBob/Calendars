using Plugin.Calendars.Abstractions;
using System.Collections.Generic;
using Windows.ApplicationModel.Appointments;

#nullable enable

namespace Plugin.Calendars
{
    /// <summary>
    /// Appointment extensions
    /// </summary>
    internal static class AppointmentExtensions
    {
        /// <summary>
        /// Creates a new Calendars.Plugin.Abstractions.CalendarEvent from an Appointment
        /// </summary>
        /// <param name="appt">Source Appointment</param>
        /// <returns>Corresponding Calendars.Plugin.Abstractions.CalendarEvent</returns>
        public static CalendarEvent ToCalendarEvent(this Appointment appt)
        {
            var reminder = appt.Reminder.HasValue ? new CalendarEventReminder { TimeBefore = appt.Reminder.Value } : null;

            return new CalendarEvent
            {
                Name = appt.Subject,
                Description = appt.Details,
                Start = appt.StartTime.LocalDateTime,
                End = appt.StartTime.Add(appt.Duration).LocalDateTime,
                AllDay = appt.AllDay,
                Location = appt.Location,
                ExternalID = appt.LocalId,
                Reminders = reminder != null ? new List<CalendarEventReminder> { reminder } : new List<CalendarEventReminder>()
            };
        }
    }
}
