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
    /// AppointmentCalendar extensions
    /// </summary>
    internal static class AppointmentCalendarExtensions
    {
        /// <summary>
        /// Creates a new Calendars.Plugin.Abstractions.Calendar from an AppointmentCalendar
        /// </summary>
        /// <param name="apptCalendar">Source AppointmentCalendar</param>
        /// <param name="writeable">Whether or not the calendar is writeable (this isn't part of AppointmentCalendar)</param>
        /// <returns>Corresponding Calendars.Plugin.Abstractions.Calendar</returns>
        public static Calendar ToCalendar(this AppointmentCalendar apptCalendar, bool writeable)
        {
            return new Calendar
            {
                Name = apptCalendar.DisplayName,
                Color = apptCalendar.DisplayColor.ToString(),
                ExternalID = apptCalendar.LocalId,
                CanEditCalendar = writeable,
                CanEditEvents = writeable
            };
        }
    }
}
