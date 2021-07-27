using EventKit;
using Plugin.Calendars.Abstractions;

#nullable enable

namespace Plugin.Calendars
{
    public static class CalendarEventReminderExtensions
    {
        public static EKAlarm ToEKAlarm(this CalendarEventReminder calendarEventReminder)
        {
            // iOS stores in negative seconds before the event, but CalendarEventReminder.TimeBefore uses
            // a positive TimeSpan before the event
            var seconds = -calendarEventReminder.TimeBefore.TotalSeconds;
            return EKAlarm.FromTimeInterval(seconds); 
        }
    }
}