using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Calendars.Plugin.Abstractions
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
      /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
      Task<IList<Calendar>> GetCalendarsAsync();

      /// <summary>
      /// Gets a single calendar by platform-specific ID.
      /// </summary>
      /// <param name="externalId">Platform-specific calendar identifier</param>
      /// <returns>The corresponding calendar, or null if not found</returns>
      /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
      /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
      Task<Calendar> GetCalendarByIdAsync(string externalId);

      /// <summary>
      /// Gets all events for a calendar within the specified time range.
      /// </summary>
      /// <param name="calendar">Calendar containing events</param>
      /// <param name="start">Start of event range</param>
      /// <param name="end">End of event range</param>
      /// <returns>Calendar events</returns>
      /// <exception cref="System.ArgumentException">Calendar does not exist on device</exception>
      /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
      /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
      Task<IList<CalendarEvent>> GetEventsAsync(Calendar calendar, DateTime start, DateTime end);

      /// <summary>
      /// Gets a single calendar event by platform-specific ID.
      /// </summary>
      /// <param name="externalId">Platform-specific calendar event identifier</param>
      /// <returns>The corresponding calendar event, or null if not found</returns>
      /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
      /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
      Task<CalendarEvent> GetEventByIdAsync(string externalId);


      /// <summary>
      /// Creates a new calendar or updates the name and color of an existing one.
      /// </summary>
      /// <param name="calendar">The calendar to create/update</param>
      /// <exception cref="System.ArgumentException">Calendar does not exist on device or is read-only</exception>
      /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
      /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
      Task AddOrUpdateCalendarAsync(Calendar calendar);

      /// <summary>
      /// Add new event to a calendar or update an existing event.
      /// </summary>
      /// <param name="calendar">Destination calendar</param>
      /// <param name="calendarEvent">Event to add or update</param>
      /// <exception cref="System.ArgumentException">Calendar is not specified, does not exist on device, or is read-only</exception>
      /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
      /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
      Task AddOrUpdateEventAsync(Calendar calendar, CalendarEvent calendarEvent);


      /// <summary>
      /// Removes a calendar and all its events from the system.
      /// </summary>
      /// <param name="calendar">Calendar to delete</param>
      /// <returns>True if successfully removed</returns>
      /// <exception cref="System.ArgumentException">Calendar is read-only</exception>
      /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
      /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
      Task<bool> DeleteCalendarAsync(Calendar calendar);

      /// <summary>
      /// Removes an event from the specified calendar.
      /// </summary>
      /// <param name="calendar">Calendar to remove event from</param>
      /// <param name="calendarEvent">Event to remove</param>
      /// <returns>True if successfully removed</returns>
      /// <exception cref="System.ArgumentException">Calendar is read-only</exception>
      /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
      /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
      Task<bool> DeleteEventAsync(Calendar calendar, CalendarEvent calendarEvent);

      /// <summary>
      /// Adds a reminder to the specified calendar event
      /// </summary>
      /// <param name="calendarEvent">Event to add</param>
      /// <param name="reminder">Reminder to add</param>
      /// <returns>If successful</returns>
      /// <exception cref="ArgumentException">If calendar event is not create or note valid</exception>
      /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
      Task<bool> AddEventReminderAsync(CalendarEvent calendarEvent, CalendarEventReminder reminder);
  }
}
