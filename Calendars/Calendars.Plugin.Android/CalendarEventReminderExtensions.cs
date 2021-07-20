using Android.Content;
using Android.Provider;
using Plugin.Calendars.Abstractions;

#nullable enable

namespace Plugin.Calendars
{
    public static class CalendarEventReminderExtensions
    {
        public static ContentValues ToContentValues(this CalendarEventReminder reminder, string? eventID = null)
        {
            var reminderValues = new ContentValues();
            reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Minutes, reminder.TimeBefore.TotalMinutes);
            reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Method, (int)reminder.Method.ToRemindersMethod());

            if (!string.IsNullOrWhiteSpace(eventID))
            {
                reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.EventId, eventID);
            }

            return reminderValues;
        }
    }
}