using Plugin.Calendars.Abstractions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.ApplicationModel.Appointments;

namespace Plugin.Calendars
{
    /// <summary>
    /// Implementation for Calendars
    /// </summary>
    public class CalendarsImplementation : ICalendars
    {
        #region Fields

        private AppointmentStore _apptStore;
        private AppointmentStore _localApptStore;

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
            await EnsureInitializedAsync().ConfigureAwait(false);

            var allCalendars = await _apptStore.FindAppointmentCalendarsAsync().ConfigureAwait(false);
            var localCalendars = await _localApptStore.FindAppointmentCalendarsAsync().ConfigureAwait(false);

            return allCalendars
                .Select(c =>
                    {
                        bool writeable = localCalendars.Any(l => l.LocalId == c.LocalId);
                        return c.ToCalendar(writeable);
                    }
                ).ToList();
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

            await EnsureInitializedAsync().ConfigureAwait(false);

            bool writeable = true;

            //var calendar = await _localApptStore.GetAppointmentCalendarAsync(externalId).ConfigureAwait(false);
            var calendar = await GetLocalCalendarAsync(externalId).ConfigureAwait(false);

            if (calendar == null)
            {
                writeable = false;

                // This throws an ArgumentException if externalId is not in the valid
                // WinPhone calendar ID format. Oddly, the above otherwise-identical call to 
                // the local appointment store does not...
                //
                calendar = await _apptStore.GetAppointmentCalendarAsync(externalId).ConfigureAwait(false);
            }

            return calendar == null ? null : calendar.ToCalendar(writeable);
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
            await EnsureInitializedAsync().ConfigureAwait(false);

            AppointmentCalendar deviceCalendar = null;

            try
            {
                deviceCalendar = await _apptStore.GetAppointmentCalendarAsync(calendar.ExternalID).ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                throw new ArgumentException("Specified calendar not found on device", ex);
            }

            // Not all properties are populated by default
            //
            var options = new FindAppointmentsOptions { IncludeHidden = false };
            options.FetchProperties.Add(AppointmentProperties.Subject);
            options.FetchProperties.Add(AppointmentProperties.Details);
            options.FetchProperties.Add(AppointmentProperties.StartTime);
            options.FetchProperties.Add(AppointmentProperties.Duration);
            options.FetchProperties.Add(AppointmentProperties.AllDay);
            options.FetchProperties.Add(AppointmentProperties.Location);

            var appointments = await deviceCalendar.FindAppointmentsAsync(start, end - start, options).ConfigureAwait(false);
            var events = appointments.Select(a => a.ToCalendarEvent()).ToList();

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

            await EnsureInitializedAsync().ConfigureAwait(false);

            var appt = await _apptStore.GetAppointmentAsync(externalId).ConfigureAwait(false);

            return appt == null ? null : appt.ToCalendarEvent();
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
            await EnsureInitializedAsync().ConfigureAwait(false);

            AppointmentCalendar existingCalendar = null;

            if (!string.IsNullOrEmpty(calendar.ExternalID))
            {
                existingCalendar = await GetAndValidateLocalCalendarAsync(calendar.ExternalID).ConfigureAwait(false);
            }

            // Note: DisplayColor is read-only, we cannot set/update it.

            if (existingCalendar == null)
            {
                // Create new calendar
                //
                var appCalendar = await CreateAppCalendarAsync(calendar.Name).ConfigureAwait(false);

                calendar.ExternalID = appCalendar.LocalId;
                calendar.Color = appCalendar.DisplayColor.ToString();
            }
            else
            {
                // Edit existing calendar
                //
                existingCalendar.DisplayName = calendar.Name;

                await existingCalendar.SaveAsync().ConfigureAwait(false);
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
        /// <exception cref="System.InvalidOperationException">Editing recurring events is not supported</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task AddOrUpdateEventAsync(Calendar calendar, CalendarEvent calendarEvent)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            AppointmentCalendar appCalendar = null;

            if (string.IsNullOrEmpty(calendar.ExternalID))
            {
                throw new ArgumentException("Missing calendar identifier", "calendar");
            }
            else
            {
                appCalendar = await GetAndValidateLocalCalendarAsync(calendar.ExternalID).ConfigureAwait(false);
            }

            Appointment appt = null;

            // If Event already corresponds to an existing Appointment in the target
            // Calendar, then edit that instead of creating a new one.
            //
            if (!string.IsNullOrEmpty(calendarEvent.ExternalID))
            {
                var existingAppt = await _localApptStore.GetAppointmentAsync(calendarEvent.ExternalID);

                if (existingAppt?.Recurrence != null)
                {
                    throw new InvalidOperationException("Editing recurring events is not supported");
                }

                if (existingAppt != null && existingAppt.CalendarId == appCalendar.LocalId)
                {
                    appt = existingAppt;
                }
            }

            if (appt == null)
            {
                appt = new Appointment();
            }

            appt.Subject = calendarEvent.Name;
            appt.Details = calendarEvent.Description ?? string.Empty;
            appt.StartTime = calendarEvent.Start;
            appt.Duration = calendarEvent.End - calendarEvent.Start;
            appt.AllDay = calendarEvent.AllDay;
            appt.Location = calendarEvent.Location ?? string.Empty;

            await appCalendar.SaveAppointmentAsync(appt);

            calendarEvent.ExternalID = appt.LocalId;
        }

        /// <summary>
        /// Sets/replaces the event reminder for the specified calendar event
        /// </summary>
        /// <param name="calendarEvent">Event to add the reminder to</param>
        /// <param name="reminder">The reminder</param>
        /// <returns>If successful</returns>
        /// <exception cref="ArgumentException">Calendar event is not created or not valid</exception>
        /// <exception cref="System.InvalidOperationException">Editing recurring events is not supported</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task<bool> AddEventReminderAsync(CalendarEvent calendarEvent, CalendarEventReminder reminder)
        {
            if (string.IsNullOrEmpty(calendarEvent.ExternalID))
            {
                throw new ArgumentException("Missing calendar event identifier", "calendarEvent");
            }

            await EnsureInitializedAsync().ConfigureAwait(false);
            
            var existingAppt = await _localApptStore.GetAppointmentAsync(calendarEvent.ExternalID);
            
            if (existingAppt == null)
            {
                throw new ArgumentException("Specified calendar event not found on device");
            }

            if (existingAppt.Recurrence != null)
            {
                throw new InvalidOperationException("Editing recurring events is not supported");
            }

            var appCalendar = await _localApptStore.GetAppointmentCalendarAsync(existingAppt.CalendarId);

            if (appCalendar == null)
            {
                throw new ArgumentException("Event does not have a valid calendar.");
            }

            existingAppt.Reminder = reminder?.TimeBefore ?? TimeSpan.FromMinutes(15);
            
            await appCalendar.SaveAppointmentAsync(existingAppt);

            return true;
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

            await EnsureInitializedAsync().ConfigureAwait(false);

            bool deleted = false;
            //var appCalendar = await _localApptStore.GetAppointmentCalendarAsync(calendar.ExternalID).ConfigureAwait(false);
            var appCalendar = await GetLocalCalendarAsync(calendar.ExternalID).ConfigureAwait(false);

            if (appCalendar != null)
            {
                // Perform the delete and return true
                await appCalendar.DeleteAsync().ConfigureAwait(false);
                deleted = true;
            }
            else
            {
                // Check for calendar from non-local appt store
                // If we get it from there, then error that it's not writeable
                // else, it just doesn't exist, so return false

                appCalendar = await _apptStore.GetAppointmentCalendarAsync(calendar.ExternalID).ConfigureAwait(false);

                if (appCalendar != null)
                {
                    throw new ArgumentException("Cannot delete read-only calendar", "calendar");
                }
            }

            return deleted;
        }

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
        public async Task<bool> DeleteEventAsync(Calendar calendar, CalendarEvent calendarEvent)
        {
            if (string.IsNullOrEmpty(calendar.ExternalID) || string.IsNullOrEmpty(calendarEvent.ExternalID))
            {
                return false;
            }

            await EnsureInitializedAsync().ConfigureAwait(false);

            bool deleted = false;
            var appCalendar = await GetLocalCalendarAsync(calendar.ExternalID).ConfigureAwait(false);

            if (appCalendar != null)
            {
                // Verify that event actually exists on calendar
                //
                var appt = await appCalendar.GetAppointmentAsync(calendarEvent.ExternalID).ConfigureAwait(false);

                if (appt?.Recurrence != null)
                {
                    throw new InvalidOperationException("Editing recurring events is not supported");
                }

                // The second check is because AppointmentCalendar.GetAppointmentAsync will apparently still
                // return events if they are associated with a different calendar...
                //
                if (appt != null && appt.CalendarId == appCalendar.LocalId)
                {
                    // Sometimes DeleteAppointmentAsync throws UnauthorizedException if the event doesn't exist?
                    // And sometimes it just fails silently?
                    // Well, hopefully the above check will help avoid either case...
                    //
                    await appCalendar.DeleteAppointmentAsync(calendarEvent.ExternalID).ConfigureAwait(false);
                    deleted = true;
                }
            }
            else
            {
                // Check for calendar from non-local appt store
                // If we get it from there, then error that it's not writeable
                // else, it just doesn't exist, so return false

                appCalendar = await _apptStore.GetAppointmentCalendarAsync(calendar.ExternalID).ConfigureAwait(false);

                if (appCalendar != null)
                {
                    throw new ArgumentException("Cannot delete event from readonly calendar", "calendar");
                }
            }

            return deleted;
        }

#endregion

#region Private Methods

        private async Task EnsureInitializedAsync()
        {
            if (_apptStore == null)
            {
                _apptStore = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AllCalendarsReadOnly).ConfigureAwait(false);
            }

            if (_localApptStore == null)
            {
                _localApptStore = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite).ConfigureAwait(false);
            }
        }

        private async Task<AppointmentCalendar> CreateAppCalendarAsync(string calendarName)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);

            var appCalendar = await _localApptStore.CreateAppointmentCalendarAsync(calendarName).ConfigureAwait(false);

            appCalendar.OtherAppReadAccess = AppointmentCalendarOtherAppReadAccess.SystemOnly;
            appCalendar.OtherAppWriteAccess = AppointmentCalendarOtherAppWriteAccess.SystemOnly;
            appCalendar.SummaryCardView = AppointmentSummaryCardView.System;

            await appCalendar.SaveAsync().ConfigureAwait(false);

            return appCalendar;
        }

        /// <summary>
        /// The main purpose of this is just to throw an appropriate exception on failure.
        /// </summary>
        /// <param name="id">Local calendar ID</param>
        /// <returns>App calendar with write access (will not return null)</returns>
        /// <exception cref="System.ArgumentException">Calendar ID does not refer to an app-owned calendar</exception>
        private async Task<AppointmentCalendar> GetAndValidateLocalCalendarAsync(string id)
        {
            AppointmentCalendar appCalendar = null;
            Exception platformException = null;

            try
            {
                appCalendar = await GetLocalCalendarAsync(id).ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                platformException = ex;
            }

            if (appCalendar == null)
            {
                throw new ArgumentException("Specified calendar does not exist or is not writeable", platformException);
            }

            return appCalendar;
        }

        /// <summary>
        /// This is to handle a difference in the behavior of Windows UWP and WinPhone 8.1.
        /// On UWP, GetAppointmentCalendarAsync for a store created with AppCalendarsReadWrite
        /// access will still return non-app calendars that the app does not have write access
        /// to. FindAppointmentCalendarsAsync, however, does still respect the access type,
        /// so we just iterate.
        /// </summary>
        /// <remarks>
        /// Trying to save changes to the calendar would have thrown an appropriate
        /// UnauthorizedAccessException, but that would be inconsistent with our
        /// behavior on other platforms and wouldn't help with setting the
        /// CanEditCalendar/CanEditEvents properties.
        /// </remarks>
        /// <param name="id">Local calendar ID</param>
        /// <returns>App calendar with write access, or null if not found.</returns>
        private async Task<AppointmentCalendar> GetLocalCalendarAsync(string id)
        {
#if WINDOWS_UWP
            var calendars = await _localApptStore.FindAppointmentCalendarsAsync().ConfigureAwait(false);
            return calendars.FirstOrDefault(cal => cal.LocalId == id);
#else
            return await _localApptStore.GetAppointmentCalendarAsync(id).ConfigureAwait(false);
#endif
        }

#endregion
    }
}
