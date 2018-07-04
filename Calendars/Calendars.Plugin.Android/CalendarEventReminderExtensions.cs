using Android.Content;
using Android.Provider;
using Plugin.Calendars.Abstractions;

namespace Plugin.Calendars
{
    public static class CalendarEventReminderExtensions
    {
        public static ContentValues ToContentValues(this CalendarEventReminder reminder, string eventID)
        {
            var reminderValues = new ContentValues();
            reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Minutes, reminder.TimeBefore.TotalMinutes);
            reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.EventId, eventID);
            reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Method, (int)reminder.Method.ToRemindersMethod());

            return reminderValues;
        }
    }
}