using Calendars.Plugin.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

#if __UNIFIED__
using EventKit;
using Foundation;
using CGColor = CoreGraphics.CGColor;
#else
using MonoTouch.CoreGraphics;
using MonoTouch.EventKit;
using MonoTouch.Foundation;
#endif


namespace Calendars.Plugin
{
    /// <summary>
    /// Implementation for Calendars
    /// </summary>
    public class CalendarsImplementation : ICalendars
    {
        #region Constants

        // iOS SDK provides this constant, but I'm not seeing it be exposed through Xamarin..
        private const string _ekErrorDomain = "EKErrorDomain";

        #endregion

        #region Fields

        private EKEventStore _eventStore;
        private bool? _hasCalendarAccess;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public CalendarsImplementation()
        {
            _eventStore = new EKEventStore();
        }

        #endregion

        #region ICalendars Implementation

        /// <summary>
        /// Gets a list of all calendars on the device.
        /// </summary>
        /// <returns>Calendars</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task<IList<Calendar>> GetCalendarsAsync()
        {
            await RequestCalendarAccess().ConfigureAwait(false);

            var calendars = _eventStore.GetCalendars(EKEntityType.Event);

            return calendars.Select(c => c.ToCalendar()).ToList();
        }

        /// <summary>
        /// Gets a single calendar by platform-specific ID.
        /// </summary>
        /// <param name="externalId">Platform-specific calendar identifier</param>
        /// <returns>The corresponding calendar, or null if not found</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task<Calendar> GetCalendarByIdAsync(string externalId)
        {
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return null;
            }

            await RequestCalendarAccess().ConfigureAwait(false);

            var calendar = _eventStore.GetCalendar(externalId);

            return calendar == null ? null : calendar.ToCalendar();
        }

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
        public async Task<IList<CalendarEvent>> GetEventsAsync(Calendar calendar, DateTime start, DateTime end)
        {
            await RequestCalendarAccess().ConfigureAwait(false);

            var deviceCalendar = _eventStore.GetCalendar(calendar.ExternalID);

            if (deviceCalendar == null)
            {
                throw new ArgumentException("Specified calendar not found on device");
            }

            var query = _eventStore.PredicateForEvents(start.ToNSDate(), end.ToNSDate(), new EKCalendar[] { deviceCalendar });
            var events = await Task.Run(() =>
            {
                var iosEvents = _eventStore.EventsMatching(query);
                return iosEvents == null ? new List<CalendarEvent>() : iosEvents.Select(e => e.ToCalendarEvent()).ToList();
            }).ConfigureAwait(false);

            return events;
        }

        /// <summary>
        /// Gets a single calendar event by platform-specific ID.
        /// </summary>
        /// <param name="externalId">Platform-specific calendar event identifier</param>
        /// <returns>The corresponding calendar event, or null if not found</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task<CalendarEvent> GetEventByIdAsync(string externalId)
        {
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return null;
            }

            await RequestCalendarAccess().ConfigureAwait(false);

            var iosEvent = _eventStore.EventFromIdentifier(externalId);

            return iosEvent == null ? null : iosEvent.ToCalendarEvent();
        }

        /// <summary>
        /// Creates a new calendar or updates the name and color of an existing one.
        /// </summary>
        /// <param name="calendar">The calendar to create/update</param>
        /// <exception cref="System.ArgumentException">Calendar does not exist on device or is read-only</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task AddOrUpdateCalendarAsync(Calendar calendar)
        {
            await RequestCalendarAccess().ConfigureAwait(false);

            EKCalendar deviceCalendar = null;

            if (!string.IsNullOrEmpty(calendar.ExternalID))
            {
                deviceCalendar = _eventStore.GetCalendar(calendar.ExternalID);

                if (deviceCalendar == null)
                {
                    throw new ArgumentException("Specified calendar does not exist on device", "calendar");
                }
            }

            if (deviceCalendar == null)
            {
                deviceCalendar = CreateEKCalendar(calendar.Name, calendar.Color);
                calendar.ExternalID = deviceCalendar.CalendarIdentifier;

                // Update color in case iOS assigned one
                calendar.Color = ColorConversion.ToHexColor(deviceCalendar.CGColor);
            }
            else
            {
                deviceCalendar.Title = calendar.Name;

                if (!string.IsNullOrEmpty(calendar.Color))
                {
                    deviceCalendar.CGColor = ColorConversion.ToCGColor(calendar.Color);
                }

                NSError error = null;
                if (!_eventStore.SaveCalendar(deviceCalendar, true, out error))
                {
                    // Without this, the eventStore will continue to return the "updated"
                    // calendar even though the save failed!
                    // (this obviously also resets any other changes, but since we own the eventStore
                    //  we can be pretty confident that won't be an issue)
                    //
                    _eventStore.Reset();

                    if (error.Domain == _ekErrorDomain && error.Code == (int)EKErrorCode.CalendarIsImmutable)
                    {
                        throw new ArgumentException(error.LocalizedDescription, new NSErrorException(error));
                    }
                    else
                    {
                        throw new PlatformException(error.LocalizedDescription, new NSErrorException(error));
                    }
                }
            }
        }

        /// <summary>
        /// Add new event to a calendar or update an existing event.
        /// Throws if Calendar ID is empty, calendar does not exist, or calendar is read-only.
        /// </summary>
        /// <param name="calendar">Destination calendar</param>
        /// <param name="calendarEvent">Event to add or update</param>
        /// <exception cref="System.ArgumentException">Calendar is not specified, does not exist on device, or is read-only</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task AddOrUpdateEventAsync(Calendar calendar, CalendarEvent calendarEvent)
        {
            await RequestCalendarAccess().ConfigureAwait(false);

            EKCalendar deviceCalendar = null;

            if (string.IsNullOrEmpty(calendar.ExternalID))
            {
                throw new ArgumentException("Missing calendar identifier", "calendar");
            }
            else
            {
                deviceCalendar = _eventStore.GetCalendar(calendar.ExternalID);

                if (deviceCalendar == null)
                {
                    throw new ArgumentException("Specified calendar not found on device");
                }
            }

            EKEvent iosEvent = null;

            // If Event already corresponds to an existing EKEvent in the target
            // Calendar, then edit that instead of creating a new one.
            //
            if (!string.IsNullOrEmpty(calendarEvent.ExternalID))
            {
                var existingEvent = _eventStore.EventFromIdentifier(calendarEvent.ExternalID);

                if (existingEvent.Calendar.CalendarIdentifier == deviceCalendar.CalendarIdentifier)
                {
                    iosEvent = existingEvent;
                }
            }

            if (iosEvent == null)
            {
                iosEvent = EKEvent.FromStore(_eventStore);
            }

            iosEvent.Title = calendarEvent.Name;
            iosEvent.Notes = calendarEvent.Description;
            iosEvent.AllDay = calendarEvent.AllDay;
            iosEvent.StartDate = calendarEvent.Start.ToNSDate();

            // If set to AllDay and given an EndDate of 12am the next day, EventKit
            // assumes that the event consumes two full days.
            // (whereas WinPhone/Android consider that one day, and thus so do we)
            //
            iosEvent.EndDate = calendarEvent.AllDay ? calendarEvent.End.AddMilliseconds(-1).ToNSDate() : calendarEvent.End.ToNSDate();
            iosEvent.Calendar = deviceCalendar;

            NSError error = null;
            if (!_eventStore.SaveEvent(iosEvent, EKSpan.ThisEvent, out error))
            {
                // Without this, the eventStore will continue to return the "updated"
                // event even though the save failed!
                // (this obviously also resets any other changes, but since we own the eventStore
                //  we can be pretty confident that won't be an issue)
                //
                _eventStore.Reset();

                // Technically, probably any ekerrordomain error would be an ArgumentException?
                // - but we don't necessarily know *which* argument (at least not without the code)
                // - for now, just focusing on the start > end scenario and translating the rest to PlatformException. Can always add more later.

                if (error.Domain == _ekErrorDomain && error.Code == (int)EKErrorCode.DatesInverted)
                {
                    throw new ArgumentException(error.LocalizedDescription, new NSErrorException(error));
                }
                else
                {
                    throw new PlatformException(error.LocalizedDescription, new NSErrorException(error));
                }
            }

            calendarEvent.ExternalID = iosEvent.EventIdentifier;
        }

        /// <summary>
        /// Removes a calendar and all its events from the system.
        /// </summary>
        /// <param name="calendar">Calendar to delete</param>
        /// <returns>True if successfully removed</returns>
        /// <exception cref="System.ArgumentException">Calendar is read-only</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task<bool> DeleteCalendarAsync(Calendar calendar)
        {
            if (string.IsNullOrEmpty(calendar.ExternalID))
            {
                return false;
            }

            await RequestCalendarAccess().ConfigureAwait(false);

            var deviceCalendar = _eventStore.GetCalendar(calendar.ExternalID);

            if (deviceCalendar == null)
            {
                return false;
            }

            NSError error = null;
            if (!_eventStore.RemoveCalendar(deviceCalendar, true, out error))
            {
                // Without this, the eventStore may act like the remove succeeded.
                // (this obviously also resets any other changes, but since we own the eventStore
                //  we can be pretty confident that won't be an issue)
                //
                _eventStore.Reset();
                
                throw new ArgumentException(error.LocalizedDescription, "calendar", new NSErrorException(error));
            }

            return true;
        }

        /// <summary>
        /// Removes an event from the specified calendar.
        /// </summary>
        /// <param name="calendar">Calendar to remove event from</param>
        /// <param name="calendarEvent">Event to remove</param>
        /// <returns>True if successfully removed</returns>
        /// <exception cref="System.ArgumentException">Calendar is read-only</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Calendars.Plugin.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task<bool> DeleteEventAsync(Calendar calendar, CalendarEvent calendarEvent)
        {
            if (string.IsNullOrEmpty(calendar.ExternalID) || string.IsNullOrEmpty(calendarEvent.ExternalID))
            {
                return false;
            }
            
            await RequestCalendarAccess().ConfigureAwait(false);

            var deviceCalendar = _eventStore.GetCalendar(calendar.ExternalID);

            if (deviceCalendar == null)
            {
                return false;
            }

            var iosEvent = _eventStore.EventFromIdentifier(calendarEvent.ExternalID);

            if (iosEvent == null || iosEvent.Calendar.CalendarIdentifier != deviceCalendar.CalendarIdentifier)
            {
                return false;
            }

            NSError error = null;
            if (!_eventStore.RemoveEvent(iosEvent, EKSpan.ThisEvent, true, out error))
            {
                // Without this, the eventStore may act like the remove succeeded.
                // (this obviously also resets any other changes, but since we own the eventStore
                //  we can be pretty confident that won't be an issue)
                //
                _eventStore.Reset();

                if (error.Domain == _ekErrorDomain && error.Code == (int)EKErrorCode.CalendarReadOnly)
                {
                    throw new ArgumentException(error.LocalizedDescription, "calendar", new NSErrorException(error));
                }
                else
                {
                    throw new PlatformException(error.LocalizedDescription, new NSErrorException(error));
                }
            }

            return true;
        }

        #endregion

        #region Private Methods

        private async Task<bool> RequestCalendarAccess()
        {
            if (!_hasCalendarAccess.HasValue)
            {
                var accessResult = await _eventStore.RequestAccessAsync(EKEntityType.Event).ConfigureAwait(false);

                NSError error = null;

#if __UNIFIED__
                _hasCalendarAccess = accessResult.Item1;
                error = accessResult.Item2;
#else
                _hasCalendarAccess = accessResult;
#endif

                if (!_hasCalendarAccess.Value)
                {
                    // This is the same exception WinPhone would throw
                    throw new UnauthorizedAccessException("Calendar access denied", error != null ? new NSErrorException(error) : null);
                }
            }
            return _hasCalendarAccess.Value;
        }

        private EKCalendar CreateEKCalendar(string calendarName, string color = null)
        {
            var calendar = EKCalendar.Create(EKEntityType.Event, _eventStore);
            calendar.Source = _eventStore.Sources.First(source => source.SourceType == EKSourceType.Local);
            calendar.Title = calendarName;

            if (!string.IsNullOrEmpty(color))
            {
                calendar.CGColor = ColorConversion.ToCGColor(color);
            }

            NSError error = null;
            if (!_eventStore.SaveCalendar(calendar, true, out error))
            {
                // Without this, the eventStore may return the new calendar even though the save failed.
                // (this obviously also resets any other changes, but since we own the eventStore
                //  we can be pretty confident that won't be an issue)
                //
                _eventStore.Reset();

                throw new PlatformException(error.LocalizedDescription, new NSErrorException(error));
            }

            return calendar;
        }

        #endregion
    }
}