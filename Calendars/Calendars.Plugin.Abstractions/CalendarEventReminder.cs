using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calendars.Plugin.Abstractions
{
    /// <summary>
    /// Calendar reminder that happens before the event such as an alert
    /// </summary>
    public class CalendarEventReminder
    {

        /// <summary>
        /// How many minutes before the event the reminder happens
        /// </summary>
        public uint MinutesBefore { get; set; } = 0;
        /// <summary>
        /// Type of reminder to display
        /// </summary>
        public CalendarReminderMethod Method { get; set; } = CalendarReminderMethod.Default;

    }

    /// <summary>
    /// Types of methods of the reminder
    /// </summary>
    public enum CalendarReminderMethod
    {
        /// <summary>
        /// Use system default
        /// </summary>
        Default,
        /// <summary>
        /// Pop up alert
        /// </summary>
        Alert,
        /// <summary>
        /// Send an email
        /// </summary>
        Email,
        /// <summary>
        /// Send an sms
        /// </summary>
        Sms
    }
}
