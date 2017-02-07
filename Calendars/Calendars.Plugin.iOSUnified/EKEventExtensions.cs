using Plugin.Calendars.Abstractions;

#if __UNIFIED__
using EventKit;
#else
using MonoTouch.EventKit;
#endif

namespace Plugin.Calendars
{
    /// <summary>
    /// iOS EKEvent extensions
    /// </summary>
    internal static class EKEventExtensions
    {
        /// <summary>
        /// Creates a new Calendars.Plugin.Abstractions.CalendarEvent from an EKEvent
        /// </summary>
        /// <param name="ekEvent">Source EKEvent</param>
        /// <returns>Corresponding Calendars.Plugin.Abstraction.CalendarEvent</returns>
        public static CalendarEvent ToCalendarEvent(this EKEvent ekEvent)
        {
            return new CalendarEvent
            {
                Name = ekEvent.Title,
                Description = ekEvent.Notes,
                Start = ekEvent.StartDate.ToDateTime(),

                // EventKit treats a one-day AllDay event as starting/ending on the same day,
                // but WinPhone/Android (and thus Calendars.Plugin) define it as ending on the following day.
                //
                    End = ekEvent.EndDate.ToDateTime().AddSeconds(ekEvent.AllDay ? 1 : 0),
                    AllDay = ekEvent.AllDay,
                    Location = ekEvent.Location,
                    Uri = ekEvent.Url,
                    ExternalID = ekEvent.EventIdentifier
                };
        }
    }
}