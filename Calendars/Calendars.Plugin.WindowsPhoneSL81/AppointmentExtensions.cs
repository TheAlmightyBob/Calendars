using Calendars.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;

namespace Calendars.Plugin
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
            return new CalendarEvent
            {
                Name = appt.Subject,
                Description = appt.Details,
                Start = appt.StartTime.LocalDateTime,
                End = appt.StartTime.Add(appt.Duration).LocalDateTime,
                AllDay = appt.AllDay,
                Location = appt.Location,
                ExternalID = appt.LocalId
            };
        }
    }
}
