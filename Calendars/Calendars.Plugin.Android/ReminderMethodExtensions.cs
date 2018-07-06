using System;
using Android.Provider;
using Plugin.Calendars.Abstractions;

namespace Plugin.Calendars
{
    static class ReminderMethodExtensions
    {
        static public RemindersMethod ToRemindersMethod(this CalendarReminderMethod method)
        {
            switch (method)
            {
                case CalendarReminderMethod.Alert:
                    return RemindersMethod.Alert;

                case CalendarReminderMethod.Default:
                    return RemindersMethod.Default;

                case CalendarReminderMethod.Email:
                    return RemindersMethod.Email;

                case CalendarReminderMethod.Sms:
                    return RemindersMethod.Sms;

                default:
                    throw new ArgumentException("Unexpected reminder method", nameof(method));
            }
        }

        static public CalendarReminderMethod ToCalendarReminderMethod(this RemindersMethod method)
        {
            switch (method)
            {
                case RemindersMethod.Alert:
                    return CalendarReminderMethod.Alert;

                case RemindersMethod.Default:
                    return CalendarReminderMethod.Default;

                case RemindersMethod.Email:
                    return CalendarReminderMethod.Email;

                case RemindersMethod.Sms:
                    return CalendarReminderMethod.Sms;

                default:
                    throw new ArgumentException("Unexpected reminders method", nameof(method));
            }
        }
    }
}