using Plugin.Calendars.Abstractions;
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


namespace Plugin.Calendars
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
        private double _defaultTimeBefore;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        public CalendarsImplementation()
        {
            _eventStore = new EKEventStore();
            //iOS stores in negative seconds before the event
            _defaultTimeBefore = -TimeSpan.FromMinutes(15).TotalSeconds;
        }

        #endregion

        #region ICalendars Implementation

        /// <summary>
        /// Gets a list of all calendars on the device.
        /// </summary>
        /// <returns>Calendars</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
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
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
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
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
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
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
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
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task AddOrUpdateCalendarAsync(Calendar calendar)
        {
            await RequestCalendarAccess().ConfigureAwait(false);

            EKCalendar deviceCalendar = null;

            if (!string.IsNullOrEmpty(calendar.ExternalID))
            {
                deviceCalendar = _eventStore.GetCalendar(calendar.ExternalID);

                if (deviceCalendar == null)
                {
                    throw new ArgumentException("Specified calendar does not exist on device", nameof(calendar));
                }
            }

            if (deviceCalendar == null)
            {
                deviceCalendar = CreateEKCalendar(calendar.Name, calendar.Color);
                calendar.ExternalID = deviceCalendar.CalendarIdentifier;

                // Update color in case iOS assigned one
                if(deviceCalendar?.CGColor != null)
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
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task AddOrUpdateEventAsync(Calendar calendar, CalendarEvent calendarEvent)
        {
            await RequestCalendarAccess().ConfigureAwait(false);

            EKCalendar deviceCalendar = null;

            if (string.IsNullOrEmpty(calendar.ExternalID))
            {
                throw new ArgumentException("Missing calendar identifier", nameof(calendar));
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
            iosEvent.Location = calendarEvent.Location ?? string.Empty;
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
        /// Adds an event reminder to specified calendar event
        /// </summary>
        /// <param name="calendarEvent">Event to add the reminder to</param>
        /// <param name="reminder">The reminder</param>
        /// <returns>Success or failure</returns>
        /// <exception cref="ArgumentException">If calendar event is not created or not valid</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public Task<bool> AddEventReminderAsync(CalendarEvent calendarEvent, CalendarEventReminder reminder)
        {
            if (string.IsNullOrEmpty(calendarEvent.ExternalID))
            {
                throw new ArgumentException("Missing calendar event identifier", nameof(calendarEvent));
            }

            //Grab current event
            var existingEvent = _eventStore.EventFromIdentifier(calendarEvent.ExternalID);
        
            if (existingEvent == null)
            {
                throw new ArgumentException("Specified calendar event not found on device");
            }

            var seconds = -reminder?.TimeBefore.TotalSeconds ?? _defaultTimeBefore;
            var alarm = EKAlarm.FromTimeInterval(seconds);

            existingEvent.AddAlarm(alarm);

            NSError error = null;
            if (!_eventStore.SaveEvent(existingEvent, EKSpan.ThisEvent, out error))
            {
                // Without this, the eventStore will continue to return the "updated"
                // event even though the save failed!
                // (this obviously also resets any other changes, but since we own the eventStore
                //  we can be pretty confident that won't be an issue)
                //
                _eventStore.Reset();

                throw new ArgumentException(error.LocalizedDescription, nameof(reminder), new NSErrorException(error));
            }


            return Task.FromResult(true);
        }

        /// <summary>
        /// Removes a calendar and all its events from the system.
        /// </summary>
        /// <param name="calendar">Calendar to delete</param>
        /// <returns>True if successfully removed</returns>
        /// <exception cref="System.ArgumentException">Calendar is read-only</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
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
                
                throw new ArgumentException(error.LocalizedDescription, nameof(calendar), new NSErrorException(error));
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
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
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
                    throw new ArgumentException(error.LocalizedDescription, nameof(calendar), new NSErrorException(error));
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

        /// <summary>
        /// Tries to create a new calendar with the specified source/name/color.
        /// May fail depending on source.
        /// </summary>
        /// <remarks>
        /// This is only intended as a helper method for CreateEKCalendar,
        /// not to be called independently.
        /// </remarks>
        /// <returns>The created native calendar, or null on failure</returns>
        /// <param name="source">Calendar source (e.g. iCloud vs local vs gmail)</param>
        /// <param name="calendarName">Calendar name.</param>
        /// <param name="color">Calendar color.</param>
        EKCalendar SaveEKCalendar(EKSource source, string calendarName, string color = null)
        {
            var calendar = EKCalendar.Create(EKEntityType.Event, _eventStore);

            // Setup calendar to be inserted
            //
            calendar.Title = calendarName;

            if (!string.IsNullOrEmpty(color))
            {
                calendar.CGColor = ColorConversion.ToCGColor(color);
            }

            calendar.Source = source;

            NSError error = null; 
            if (_eventStore.SaveCalendar(calendar, true, out error))
            {
                return calendar;
            }

            _eventStore.Reset();

            return null;
        }

        /// <summary>
        /// Creates a new calendar.
        /// </summary>
        /// <remarks>
        /// This crazy series of loops and save attempts is necessary because it is difficult
        /// to determine which EKSource we should use for a new calendar.
        /// 1. If iCloud calendars are enabled, then local device calendars will be hidden from
        ///    the user (and from our own GetCalendars requests), even though they still exist on device.
        ///    Therefore we must create new calendars with the iCloud source.
        /// 2. If iCloud calendars are *not* enabled, then the opposite is true and we should go local.
        /// 3. It is difficult to identify the iCloud EKSource(s?) and to even determine if iCloud
        ///    is enabled.
        ///    a. By default, the name is "iCloud." Most of the time, that will be unchanged and
        ///       should be an effective way to locate an/the iCloud source.
        ///    b. However, it *might* have changed. iOS previously allowed the user to rename it in
        ///       Settings. They removed that setting, but users may have already changed it.
        ///       Additionally, some users have discovered elaborate workarounds to change it in
        ///       newer versions. Because, you know, Apple took away their setting and they wanted it back.
        ///       So, if we don't find any "iCloud" sources, we fall back to searching for any
        ///       CalDav sources.
        ///       - This does mean that we may try storing calendars to non-iCloud CalDav sources,
        ///         such as Gmail. Gmail is expected to fail, which is fine and we just keep searching.
        ///         Uncertain if there are other possible CalDav sources that could *succeed* and
        ///         that we would want to avoid.
        ///    c. Also, the mere existence of the iCloud source does not necessarily prove that
        ///       iCloud calendar sync is currently enabled. Turning iCloud calendars on and off
        ///       may leave the source in the event store even though it's disabled. So we also
        ///       check that there exists at least one calendar for that source.
        ///    d. We do not know if we can save to a calendar source until we actually try to do
        ///       so. Hence the repeated save attempts.
        /// 
        /// Full lengthy discussion at https://github.com/TheAlmightyBob/Calendars/issues/10
        /// </remarks>
        /// <returns>The new native calendar.</returns>
        /// <param name="calendarName">Calendar name.</param>
        /// <param name="color">Calendar color.</param>
        /// <exception cref="System.InvalidOperationException">No active calendar sources available to create calendar on.</exception>
        private EKCalendar CreateEKCalendar(string calendarName, string color = null)
        {
            // first attempt to find any and all iCloud sources
            //
            var iCloudSources = _eventStore.Sources.Where(s => s.SourceType == EKSourceType.CalDav && s.Title.Equals("icloud", StringComparison.InvariantCultureIgnoreCase));
            foreach (var source in iCloudSources)
            {
                //Ensure that the calendar is enabled
                if (source.GetCalendars(EKEntityType.Event).Count > 0)
                {
                    var cal = SaveEKCalendar(source, calendarName, color);
                    if (cal != null)
                        return cal;
                }
            }

            // other sources that we didn't try before that are caldav
            //
            var otherSources = _eventStore.Sources.Where(s => s.SourceType == EKSourceType.CalDav && !s.Title.Equals("icloud", StringComparison.InvariantCultureIgnoreCase));
            foreach (var source in otherSources)
            {
                var cal = SaveEKCalendar(source, calendarName, color);
                if (cal != null)
                    return cal;
            }
          
            // finally attempt just local sources
            //
            var localSources = _eventStore.Sources.Where(s => s.SourceType == EKSourceType.Local);
            foreach (var source in localSources)
            {
                var cal = SaveEKCalendar(source, calendarName, color);
                if (cal != null)
                    return cal;
            }

            throw new InvalidOperationException("No active calendar sources available to create calendar on.");
        }

        #endregion
    }
}
