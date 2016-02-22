using Plugin.Calendars.Abstractions;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Android.App;
using Android.Provider;
using Android.Content;
using Android.Database;
using System.Globalization;
using Calendar = Plugin.Calendars.Abstractions.Calendar;

namespace Plugin.Calendars
{
    /// <summary>
    /// Implementation for Calendars
    /// </summary>
    public class CalendarsImplementation : ICalendars
    {
        #region Constants

        private static readonly Android.Net.Uri _calendarsUri = CalendarContract.Calendars.ContentUri;
        private static readonly Android.Net.Uri _eventsUri = CalendarContract.Events.ContentUri;

        private static readonly string[] _calendarsProjection =
            {
                CalendarContract.Calendars.InterfaceConsts.Id,
                CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
                CalendarContract.Calendars.InterfaceConsts.CalendarColor,
                CalendarContract.Calendars.InterfaceConsts.AccountName,
                CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel,
                CalendarContract.Calendars.InterfaceConsts.OwnerAccount,
                CalendarContract.Calendars.InterfaceConsts.AccountType
            };
        
        #endregion
        
        #region Properties

        /// <summary>
        /// Gets or sets the name of the account to use for creating/editing calendars.
        /// Defaults to application package label.
        /// </summary>
        /// <value>The name of the account.</value>
        public string AccountName { get; set; }

        /// <summary>
        /// Gets or sets the owner account to use for creating/editing calendars.
        /// Defaults to application package label.
        /// </summary>
        /// <value>The owner account.</value>
        public string OwnerAccount { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin.Calendars.CalendarsImplementation"/> class.
        /// </summary>
        public CalendarsImplementation()
        {
            AccountName = OwnerAccount = Application.Context.ApplicationInfo.LoadLabel(Application.Context.PackageManager);
        }

        #endregion

        #region ICalendars implementation

        /// <summary>
        /// Gets a list of all calendars on the device.
        /// </summary>
        /// <returns>Calendars</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public Task<IList<Calendar>> GetCalendarsAsync()
        {
            return Task.Run<IList<Calendar>>(() =>
                {
                    var calendars = new List<Calendar>();
                    var cursor = Query(_calendarsUri, _calendarsProjection);

                    try
                    {
                        if (cursor.MoveToFirst())
                        {
                            do
                            {
                                calendars.Add(GetCalendar(cursor));
                            } while (cursor.MoveToNext());
                        }
                    }
                    catch (Java.Lang.Exception ex)
                    {
                        throw new PlatformException(ex.Message, ex);
                    }
                    finally
                    {
                        cursor.Close();
                    }

                    return calendars;
                });
        }

        /// <summary>
        /// Gets a single calendar by platform-specific ID.
        /// </summary>
        /// <param name="externalId">Platform-specific calendar identifier</param>
        /// <returns>The corresponding calendar, or null if not found</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public Task<Calendar> GetCalendarByIdAsync(string externalId)
        {
            long calendarId = -1;

            if (!long.TryParse(externalId, out calendarId))
            {
                return null;
            }

            return Task.Run<Calendar>(() =>
                {
                    Calendar calendar = null;
                    var cursor = Query(
                        ContentUris.WithAppendedId(_calendarsUri, calendarId),
                        _calendarsProjection);
                   
                    try
                    {
                        if (cursor.MoveToFirst())
                        {
                            calendar = GetCalendar(cursor);
                        }
                    }
                    catch (Java.Lang.Exception ex)
                    {
                        throw new PlatformException(ex.Message, ex);
                    }
                    finally
                    {
                        cursor.Close();
                    }

                    return calendar;
                });
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
            var deviceCalendar = await GetCalendarByIdAsync(calendar.ExternalID).ConfigureAwait(false);

            if (deviceCalendar == null)
            {
                throw new ArgumentException("Specified calendar not found on device");
            }

            var eventsUriBuilder = CalendarContract.Instances.ContentUri.BuildUpon();

            // Note that this is slightly different from the GetEventById projection
            // due to the Instances API vs. Event API (specifically, IDs)
            //
            string[] eventsProjection =
            {
//                CalendarContract.Events.InterfaceConsts.Id,
                CalendarContract.Events.InterfaceConsts.Title,
                CalendarContract.Events.InterfaceConsts.Description,
                CalendarContract.Events.InterfaceConsts.Dtstart,
                CalendarContract.Events.InterfaceConsts.Dtend,
                CalendarContract.Events.InterfaceConsts.AllDay,
                CalendarContract.Events.InterfaceConsts.EventLocation,
                CalendarContract.Instances.EventId
            };

            ContentUris.AppendId(eventsUriBuilder, DateConversions.GetDateAsAndroidMS(start));
            ContentUris.AppendId(eventsUriBuilder, DateConversions.GetDateAsAndroidMS(end));
            var eventsUri = eventsUriBuilder.Build();
            var events = new List<CalendarEvent>();

            await Task.Run(() => 
            {
                var cursor = Query(eventsUri, eventsProjection,
                   string.Format("{0} = {1}", CalendarContract.Events.InterfaceConsts.CalendarId, calendar.ExternalID),
                   null, CalendarContract.Events.InterfaceConsts.Dtstart + " ASC");

                try
                {
                    if (cursor.MoveToFirst())
                    {
                        do
                        {
                            events.Add(new CalendarEvent
                                {
                                    Name = cursor.GetString(CalendarContract.Events.InterfaceConsts.Title),
                                    ExternalID = cursor.GetString(CalendarContract.Instances.EventId),
                                    Description = cursor.GetString(CalendarContract.Events.InterfaceConsts.Description),
                                    Start = cursor.GetDateTime(CalendarContract.Events.InterfaceConsts.Dtstart),
                                    End = cursor.GetDateTime(CalendarContract.Events.InterfaceConsts.Dtend),
                                    Location = cursor.GetString(CalendarContract.Events.InterfaceConsts.EventLocation),
                                    AllDay = cursor.GetBoolean(CalendarContract.Events.InterfaceConsts.AllDay)
                                });
                        } while (cursor.MoveToNext());
                    }
                }
                catch (Java.Lang.Exception ex)
                {
                    throw new PlatformException(ex.Message, ex);
                }
                finally
                {
                    cursor.Close();
                }
            });
            
            return events;
        }

        /// <summary>
        /// Gets a single calendar event by platform-specific ID.
        /// </summary>
        /// <param name="externalId">Platform-specific calendar event identifier</param>
        /// <returns>The corresponding calendar event, or null if not found</returns>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public Task<CalendarEvent> GetEventByIdAsync(string externalId)
        {
            // Note that this is slightly different from the GetEvents projection
            // due to the Instances API vs Events API (specifically, IDs)
            //
            string[] eventsProjection =
            {
                CalendarContract.Events.InterfaceConsts.Id,
                CalendarContract.Events.InterfaceConsts.Title,
                CalendarContract.Events.InterfaceConsts.Description,
                CalendarContract.Events.InterfaceConsts.Dtstart,
                CalendarContract.Events.InterfaceConsts.Dtend,
                CalendarContract.Events.InterfaceConsts.EventLocation,
                CalendarContract.Events.InterfaceConsts.AllDay
            };
                    
            return Task.Run<CalendarEvent>(() => 
            {
                CalendarEvent calendarEvent = null;
                var cursor = Query(
                    ContentUris.WithAppendedId(_eventsUri, long.Parse(externalId)),
                    eventsProjection);

                try
                {
                    if (cursor.MoveToFirst())
                    {
                        calendarEvent = new CalendarEvent
                        {
                            Name = cursor.GetString(CalendarContract.Events.InterfaceConsts.Title),
                            ExternalID = cursor.GetString(CalendarContract.Events.InterfaceConsts.Id),
                            Description = cursor.GetString(CalendarContract.Events.InterfaceConsts.Description),
                            Start = cursor.GetDateTime(CalendarContract.Events.InterfaceConsts.Dtstart),
                            End = cursor.GetDateTime(CalendarContract.Events.InterfaceConsts.Dtend),
                            Location = cursor.GetString(CalendarContract.Events.InterfaceConsts.EventLocation),
                            AllDay = cursor.GetBoolean(CalendarContract.Events.InterfaceConsts.AllDay)
                        };
                    }
                }
                catch (Java.Lang.Exception ex)
                {
                    throw new PlatformException(ex.Message, ex);
                }
                finally
                {
                    cursor.Close();
                }
                
                return calendarEvent;
            });
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
            bool updateExisting = false;
            long existingId = -1;

            if (long.TryParse(calendar.ExternalID, out existingId))
            {
                var existingCalendar = await GetCalendarByIdAsync(calendar.ExternalID).ConfigureAwait(false);

                if (existingCalendar != null)
                {
                    if (!existingCalendar.CanEditCalendar)
                    {
                        throw new ArgumentException("Destination calendar is not writeable");
                    }

                    updateExisting = true;
                }
                else
                {
                    throw new ArgumentException("Specified calendar does not exist on device", "calendar");
                }
            }

            var values = new ContentValues();
            values.Put(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName, calendar.Name);
            values.Put(CalendarContract.Calendars.Name, calendar.Name);

            // Unlike iOS/WinPhone, Android does not automatically assign a color for us,
            // so we use our own default of blue.
            //
            int colorInt = unchecked((int)0xFF0000FF);

            if (!string.IsNullOrEmpty(calendar.Color))
            {
                int.TryParse(calendar.Color.Trim('#'), NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out colorInt);
            }

            values.Put(CalendarContract.Calendars.InterfaceConsts.CalendarColor, colorInt);

            if (!updateExisting)
            {
                values.Put(CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel, (int)CalendarAccess.AccessOwner);
                values.Put(CalendarContract.Calendars.InterfaceConsts.AccountName, AccountName);
                values.Put(CalendarContract.Calendars.InterfaceConsts.OwnerAccount, OwnerAccount);
                values.Put(CalendarContract.Calendars.InterfaceConsts.Visible, true);
                values.Put(CalendarContract.Calendars.InterfaceConsts.SyncEvents, true);

                values.Put(CalendarContract.Calendars.InterfaceConsts.AccountType, CalendarContract.AccountTypeLocal);
            }

            await Task.Run(() =>
                {
                    if (updateExisting)
                    {
                        Update(_calendarsUri, existingId, values);
                    }
                    else
                    {
                        var uri = _calendarsUri.BuildUpon()
                            .AppendQueryParameter(CalendarContract.CallerIsSyncadapter, "true")
                            .AppendQueryParameter(CalendarContract.Calendars.InterfaceConsts.AccountName, AccountName)
                            .AppendQueryParameter(CalendarContract.Calendars.InterfaceConsts.AccountType, CalendarContract.AccountTypeLocal)
                            .Build();

                        calendar.ExternalID = Insert(uri, values);

                        calendar.CanEditCalendar = true;
                        calendar.CanEditEvents = true;
                        calendar.Color = "#" + colorInt.ToString("x8");
                    }
                }).ConfigureAwait(false);
        }

        /// <summary>
        /// Add new event to a calendar or update an existing event.
        /// </summary>
        /// <param name="calendar">Destination calendar</param>
        /// <param name="calendarEvent">Event to add or update</param>
        /// <exception cref="System.ArgumentException">Calendar is not specified, does not exist on device, or is read-only</exception>
        /// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task AddOrUpdateEventAsync(Calendar calendar, CalendarEvent calendarEvent)
        {
            if (string.IsNullOrEmpty(calendar.ExternalID))
            {
                throw new ArgumentException("Missing calendar identifier", "calendar");
            }
            else
            {
                // Verify calendar exists (Android actually allows using a nonexistent calendar ID...)
                //
                var deviceCalendar = await GetCalendarByIdAsync(calendar.ExternalID).ConfigureAwait(false);

                if (deviceCalendar == null)
                {
                    throw new ArgumentException("Specified calendar not found on device");
                }
            }

            // Validate times
            if (calendarEvent.End < calendarEvent.Start)
            {
                throw new ArgumentException("End time may not precede start time", "calendarEvent");
            }

            bool updateExisting = false;
            long existingId = -1;

            await Task.Run(() =>
                {
                    if (long.TryParse(calendarEvent.ExternalID, out existingId))
                    {
                        var calendarId = GetCalendarIdForEventId(calendarEvent.ExternalID);

                        if (calendarId.HasValue && calendarId.Value.ToString() == calendar.ExternalID)
                        {
                            updateExisting = true;
                        }
                    }

                    var eventValues = new ContentValues();

                    eventValues.Put(CalendarContract.Events.InterfaceConsts.CalendarId,
                        calendar.ExternalID);
                    eventValues.Put(CalendarContract.Events.InterfaceConsts.Title,
                        calendarEvent.Name);
                    eventValues.Put(CalendarContract.Events.InterfaceConsts.Description,
                        calendarEvent.Description);
                    eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtstart,
                        DateConversions.GetDateAsAndroidMS(calendarEvent.Start));
                    eventValues.Put(CalendarContract.Events.InterfaceConsts.Dtend,
                        DateConversions.GetDateAsAndroidMS(calendarEvent.End));
                    eventValues.Put(CalendarContract.Events.InterfaceConsts.AllDay,
                        calendarEvent.AllDay);
                    eventValues.Put(CalendarContract.Events.InterfaceConsts.EventLocation,
                        calendarEvent.Location ?? string.Empty);

                    eventValues.Put(CalendarContract.Events.InterfaceConsts.EventTimezone,
                        Java.Util.TimeZone.Default.ID);

                    if (!updateExisting)
                    {
                        calendarEvent.ExternalID = Insert(_eventsUri, eventValues);
                    }
                    else
                    {
                        Update(_eventsUri, existingId, eventValues);
                    }
                });
        }

        /// <summary>
        /// Adds an event reminder to specified calendar event
        /// </summary>
        /// <param name="calendarEvent">Event to add the reminder to</param>
        /// <param name="reminder">The reminder</param>
        /// <returns>Success or failure</returns>
        /// <exception cref="ArgumentException">If calendar event is not created or not valid</exception>
        /// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
        public async Task<bool> AddEventReminderAsync(CalendarEvent calendarEvent, CalendarEventReminder reminder)
        {
            if (string.IsNullOrEmpty(calendarEvent.ExternalID))
            {
                throw new ArgumentException("Missing calendar event identifier", "calendarEvent");
            }
            // Verify calendar event exists 
            var existingAppt = await GetEventByIdAsync(calendarEvent.ExternalID).ConfigureAwait(false);

            if (existingAppt == null)
            {
                throw new ArgumentException("Specified calendar event not found on device");
            }
            
            return await Task.Run(() =>
            {
                var reminderValues = new ContentValues();
                reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Minutes, reminder?.TimeBefore.TotalMinutes ?? 15);
                reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.EventId, calendarEvent.ExternalID);
                switch(reminder.Method)
                {
                    case CalendarReminderMethod.Alert:
                        reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Method, (int)RemindersMethod.Alert);
                        break;
                    case CalendarReminderMethod.Default:
                        reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Method, (int)RemindersMethod.Default);
                        break;
                    case CalendarReminderMethod.Email:
                        reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Method, (int)RemindersMethod.Email);
                        break;
                    case CalendarReminderMethod.Sms:
                        reminderValues.Put(CalendarContract.Reminders.InterfaceConsts.Method, (int)RemindersMethod.Sms);
                        break;

                }
                var uri = CalendarContract.Reminders.ContentUri;
                Insert(uri, reminderValues);
                

                return true;
            });

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
            var existing = await GetCalendarByIdAsync(calendar.ExternalID).ConfigureAwait(false);

            if (existing == null)
            {
                return false;
            }
            else if (!existing.CanEditCalendar)
            {
                throw new ArgumentException("Cannot delete calendar (probably because it's non-local)", "calendar");
            }

            return await Task.Run(() => Delete(_calendarsUri, long.Parse(calendar.ExternalID))).ConfigureAwait(false);
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
            long existingId = -1;

            // Even though a Calendar was passed-in, we get this to both verify
            // that the calendar exists and to make sure we have accurate permissions
            // (rather than trusting the permissions that were passed to us...)
            //
            var existingCal = await GetCalendarByIdAsync(calendar.ExternalID).ConfigureAwait(false);

            if (existingCal == null)
            {
                return false;
            }
            else if (!existingCal.CanEditEvents)
            {
                throw new ArgumentException("Cannot delete event from readonly calendar", "calendar");
            }

            if (long.TryParse(calendarEvent.ExternalID, out existingId))
            {
                return await Task.Run<bool>(() =>
                    {
                        var calendarId = GetCalendarIdForEventId(calendarEvent.ExternalID);

                        if (calendarId.HasValue && calendarId.Value.ToString() == calendar.ExternalID)
                        {
                            var eventsUri = CalendarContract.Events.ContentUri;
                            return Delete(eventsUri, existingId);
                        }
                        return false;
                    });
            }

            return false;
        }

        #endregion

        #region Private Methods

        private static bool IsCalendarWriteable(int accessLevel)
        {
            switch ((CalendarAccess)accessLevel)
            {
                case CalendarAccess.AccessContributor:
                case CalendarAccess.AccessEditor:
                case CalendarAccess.AccessOwner:
                case CalendarAccess.AccessRoot:
                    return true;
                default:
                    return false;
            }
        }

        private static long? GetCalendarIdForEventId(string externalId)
        {
            string[] eventsProjection =
            {
                CalendarContract.Events.InterfaceConsts.CalendarId
            };

            long? calendarId = null;
            var cursor = Query(
                ContentUris.WithAppendedId(_eventsUri, long.Parse(externalId)),
                eventsProjection);

            try
            {
                if (cursor.MoveToFirst())
                {
                    calendarId = cursor.GetLong(CalendarContract.Events.InterfaceConsts.CalendarId);
                }
            }
            catch (Java.Lang.Exception ex)
            {
                throw new PlatformException(ex.Message, ex);
            }
            finally
            {
                cursor.Close();
            }

            return calendarId;
        }

        private static Calendar GetCalendar(ICursor cursor)
        {
            var accessLevel = cursor.GetInt(CalendarContract.Calendars.InterfaceConsts.CalendarAccessLevel);
            var accountType = cursor.GetString(CalendarContract.Calendars.InterfaceConsts.AccountType);
            var colorInt = cursor.GetInt(CalendarContract.Calendars.InterfaceConsts.CalendarColor);
            var colorString = string.Format("#{0:x8}", colorInt);

            return new Calendar
                {
                    Name = cursor.GetString(CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName),
                    ExternalID = cursor.GetString(CalendarContract.Calendars.InterfaceConsts.Id),
                    CanEditCalendar = accountType == CalendarContract.AccountTypeLocal,
                    CanEditEvents = IsCalendarWriteable(accessLevel),
                    Color = colorString
                };
        }
            
        private static ICursor Query(Android.Net.Uri uri, string[] projection, string selection = null,
            string[] selectionArgs = null, string sortOrder = null)
        {
            try
            {
                return Application.Context.ContentResolver.Query(uri, projection, selection, selectionArgs, sortOrder);
            }
            catch (Java.Lang.Exception ex)
            {
                throw TranslateException(ex);
            }
        }

        /// <summary>
        /// Returns ID of new item
        /// </summary>
        private static string Insert(Android.Net.Uri uri, ContentValues values)
        {
            try
            {
                return Application.Context.ContentResolver.Insert(uri, values).LastPathSegment;
            }
            catch (Java.Lang.Exception ex)
            {
                throw TranslateException(ex);
            }
        }

        private static void Update(Android.Net.Uri uri, long id, ContentValues values)
        {
            try
            {
                Application.Context.ContentResolver.Update(ContentUris.WithAppendedId(uri, id), values, null, null);
            }
            catch (Java.Lang.Exception ex)
            {
                throw TranslateException(ex);
            }
        }

        private static bool Delete(Android.Net.Uri uri, long id)
        {
            try
            {
                return 0 < Application.Context.ContentResolver.Delete(ContentUris.WithAppendedId(uri, id), null, null);
            }
            catch (Java.Lang.Exception ex)
            {
                throw TranslateException(ex);
            }
        }

        private static Exception TranslateException(Java.Lang.Exception ex)
        {
            if (ex is Java.Lang.SecurityException)
            {
                return new UnauthorizedAccessException(ex.Message, ex);
            }
            else
            {
                return new PlatformException(ex.Message, ex);
            }
        }
            
        #endregion
    }
}
