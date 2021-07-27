using System;
using Android.Provider;
using Plugin.Calendars.Abstractions;

#nullable enable

namespace Plugin.Calendars
{
    static class ReminderMethodExtensions
    {
        static public RemindersMethod ToRemindersMethod(this CalendarReminderMethod method) => method switch
        {
            CalendarReminderMethod.Alert => RemindersMethod.Alert,
            CalendarReminderMethod.Default => RemindersMethod.Default,
            CalendarReminderMethod.Email => RemindersMethod.Email,
            CalendarReminderMethod.Sms => RemindersMethod.Sms,
            _ => throw new ArgumentException("Unexpected reminder method", nameof(method)),
        };

        static public CalendarReminderMethod ToCalendarReminderMethod(this RemindersMethod method) => method switch
        {
            RemindersMethod.Alert => CalendarReminderMethod.Alert,
            RemindersMethod.Default => CalendarReminderMethod.Default,
            RemindersMethod.Email => CalendarReminderMethod.Email,
            RemindersMethod.Sms => CalendarReminderMethod.Sms,
            _ => throw new ArgumentException("Unexpected reminders method", nameof(method)),
        };
    }
}