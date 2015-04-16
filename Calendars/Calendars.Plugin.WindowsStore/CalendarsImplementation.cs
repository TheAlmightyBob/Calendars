using Calendars.Plugin.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Calendars.Plugin
{
  /// <summary>
  /// Implementation for Calendars
  /// </summary>
  /// <remarks>Currently this entire implementation is just a wrapper around NotSupportedException,
  ///          due to lack of access to a direct calendar API in Windows Store apps.</remarks>
  public class CalendarsImplementation : ICalendars
  {
      /// <summary>
      /// Not supported for Windows Store apps
      /// </summary>
      public Task<IList<Calendar>> GetCalendarsAsync()
      {
          throw new NotSupportedException();
      }

      /// <summary>
      /// Not supported for Windows Store apps
      /// </summary>
      public Task<Calendar> GetCalendarByIdAsync(string externalId)
      {
          throw new NotSupportedException();
      }

      /// <summary>
      /// Not supported for Windows Store apps
      /// </summary>
      public Task<IList<CalendarEvent>> GetEventsAsync(Calendar calendar, DateTime start, DateTime end)
      {
          throw new NotSupportedException();
      }

      /// <summary>
      /// Not supported for Windows Store apps
      /// </summary>
      public Task<CalendarEvent> GetEventByIdAsync(string externalId)
      {
          throw new NotSupportedException();
      }

      /// <summary>
      /// Not supported for Windows Store apps
      /// </summary>
      public Task<Calendar> CreateCalendarAsync(string calendarName, string color = null)
      {
          throw new NotSupportedException();
      }

      /// <summary>
      /// Not supported for Windows Store apps
      /// </summary>
      public Task AddOrUpdateCalendarAsync(Calendar calendar)
      {
          throw new NotSupportedException();
      }

      /// <summary>
      /// Not supported for Windows Store apps
      /// </summary>
      public Task AddOrUpdateEventAsync(Calendar calendar, CalendarEvent calendarEvent)
      {
          throw new NotSupportedException();
      }

      /// <summary>
      /// Not supported for Windows Store apps
      /// </summary>
      public Task<bool> DeleteCalendarAsync(Calendar calendar)
      {
          throw new NotSupportedException();
      }

      /// <summary>
      /// Not supported for Windows Store apps
      /// </summary>
      public Task<bool> DeleteEventAsync(Calendar calendar, CalendarEvent cev)
      {
          throw new NotSupportedException();
      }
  }
}