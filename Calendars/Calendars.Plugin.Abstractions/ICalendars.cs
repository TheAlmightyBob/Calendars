using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Plugin.Calendars.Abstractions
{
    /// <summary>
    /// Interface for Calendars
    /// </summary>
    public interface ICalendars
    {
        /// <summary>
        /// Gets a list of all calendars on the device.
        /// </summary>
        /// <returns>Calendars</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        Task<IList<Calendar>> GetCalendarsAsync();

        /// <summary>
        /// Gets a single calendar by platform-specific ID.
        /// </summary>
        /// <param name="externalId">Platform-specific calendar identifier</param>
        /// <returns>The corresponding calendar, or null if not found</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        Task<Calendar?> GetCalendarByIdAsync(string externalId);

        /// <summary>
        /// Gets all events for a calendar within the specified time range.
        /// </summary>
        /// <param name="calendar">Calendar containing events</param>
        /// <param name="start">Start of event range</param>
        /// <param name="end">End of event range</param>
        /// <returns>Calendar events</returns>
        /// <exception cref="System.ArgumentException">Calendar does not exist on device</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        Task<IList<CalendarEvent>> GetEventsAsync(Calendar calendar, DateTime start, DateTime end);

        /// <summary>
        /// Gets a single calendar event by platform-specific ID.
        /// </summary>
        /// <param name="externalId">Platform-specific calendar event identifier</param>
        /// <returns>The corresponding calendar event, or null if not found</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        Task<CalendarEvent?> GetEventByIdAsync(string externalId);


        /// <summary>
        /// Creates a new calendar or updates the name and color of an existing one.
        /// If a new calendar was created, the ExternalID property will be set on the Calendar object
        /// to support future queries/updates.
        /// </summary>
        /// <param name="calendar">The calendar to create/update</param>
        /// <exception cref="System.ArgumentException">Calendar does not exist on device or is read-only</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="System.InvalidOperationException">No active calendar sources available to create calendar on (iOS-only)</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        Task AddOrUpdateCalendarAsync(Calendar calendar);

        /// <summary>
        /// Add new event to a calendar or update an existing event.
        /// If a new event was added, the ExternalID property will be set on the CalendarEvent object,
        /// to support future queries/updates.
        /// Note that UWP does not allow adding multiple reminders to an event.
        /// </summary>
        /// <param name="calendar">Destination calendar</param>
        /// <param name="calendarEvent">Event to add or update</param>
        /// <exception cref="System.ArgumentException">Calendar is not specified, does not exist on device, or is read-only</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Windows does not support multiple reminders</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="System.InvalidOperationException">Editing recurring events is not supported</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        Task AddOrUpdateEventAsync(Calendar calendar, CalendarEvent calendarEvent);


        /// <summary>
        /// Removes a calendar and all its events from the system.
        /// </summary>
        /// <param name="calendar">Calendar to delete</param>
        /// <returns>True if successfully removed</returns>
        /// <exception cref="System.ArgumentException">Calendar is read-only</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        Task<bool> DeleteCalendarAsync(Calendar calendar);

        /// <summary>
        /// Removes an event from the specified calendar.
        /// </summary>
        /// <param name="calendar">Calendar to remove event from</param>
        /// <param name="calendarEvent">Event to remove</param>
        /// <returns>True if successfully removed</returns>
        /// <exception cref="System.ArgumentException">Calendar is read-only</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="System.InvalidOperationException">Editing recurring events is not supported</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        Task<bool> DeleteEventAsync(Calendar calendar, CalendarEvent calendarEvent);

        /// <summary>
        /// iOS/Android: Adds a reminder to the specified calendar event
        /// Windows: Sets/replaces the reminder for the specified calendar event
        /// </summary>
        /// <param name="calendarEvent">Event to add</param>
        /// <param name="reminder">Reminder to add</param>
        /// <exception cref="ArgumentException">Calendar event is not created or not valid</exception>
        /// <exception cref="System.InvalidOperationException">Editing recurring events is not supported</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        [Obsolete("Please use AddOrUpdateEventAsync to update reminders for an event")]
        Task AddEventReminderAsync(CalendarEvent calendarEvent, CalendarEventReminder reminder);
    }
}
