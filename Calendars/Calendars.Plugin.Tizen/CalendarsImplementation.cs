using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Plugin.Calendars.Abstractions;
using Tizen.Pims.Calendar;
using Tizen.Pims.Calendar.CalendarViews;

namespace Plugin.Calendars
{
	/// <summary>
	/// Implementation for Calendars
	/// </summary>
	public class CalendarsImplementation : ICalendars
	{
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
				CalendarRecord calRecord = new CalendarRecord(Book.Uri);
				var calendars = new List<Calendar>();

				calRecord.Set<string>(Book.Name, "org.tizen.calendar");
				calendars.Add(
					new Calendar
					{
						Name = calRecord.Get<string>(Book.Name),
						ExternalID = calRecord.Get<int>(Book.Id).ToString(),
						CanEditEvents = true,
						CanEditCalendar = false,
						AccountName = calRecord.Get<int>(Book.AccountId).ToString(),
						Color = calRecord.Get<string>(Book.Color),
					}
				);

				calRecord?.Dispose();
				calRecord = null;

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
			CalendarRecord calRecord = new CalendarRecord(Book.Uri);
			Calendar calendar = new Calendar();

			calRecord.Set<string>(Book.Name, "org.tizen.calendar");
			calendar.Name = calRecord.Get<string>(Book.Name);
			calendar.ExternalID = calRecord.Get<int>(Book.Id).ToString();
			calendar.CanEditEvents = true;
			calendar.CanEditCalendar = false;
			calendar.AccountName = calRecord.Get<int>(Book.AccountId).ToString();
			calendar.Color = calRecord.Get<string>(Book.Color);

			calRecord?.Dispose();
			calRecord = null;
			
			return Task.Run(() => calendar);
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
		public Task<IList<CalendarEvent>> GetEventsAsync(Calendar calendar, DateTime start, DateTime end)
		{
			return Task.Run<IList<CalendarEvent>>(() =>
			{
				var calendars = new List<CalendarEvent>();

				CalendarManager calManager = new CalendarManager();
				CalendarQuery calQuery = new CalendarQuery(Event.Uri);
				CalendarList calList = calManager.Database.GetRecordsWithQuery(calQuery, 0, 0);
				CalendarRecord calRecord = calList.GetCurrentRecord();

				if (calList.Count == 0)
					return calendars;

				calList.MoveFirst();

				do
				{
					string summary = calRecord.Get<string>(Event.Summary);
					int IsAllday = calRecord.Get<int>(Event.IsAllday);
					CalendarTime sTime = Convert.ToBoolean(IsAllday) ? ConvertIntPtrToCalendarTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)) : calRecord.Get<CalendarTime>(Event.Start);
					CalendarTime End = Convert.ToBoolean(IsAllday) ? ConvertIntPtrToCalendarTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)): calRecord.Get<CalendarTime>(Event.End);
					string location = calRecord.Get<string>(Event.Location);
					string description = calRecord.Get<string>(Event.Description);
					int Id = calRecord.Get<int>(Event.Id);
					TimeSpan s = sTime.UtcTime - start;
					TimeSpan e = end - End.UtcTime;
					if (s.TotalSeconds >= 0 && e.TotalSeconds >= 0)
					{
						calendars.Add(
							new CalendarEvent
							{
								Name = summary,
								Start = sTime.UtcTime,
								End = End.UtcTime,
								Location = location,
								Description = description,
								AllDay = Convert.ToBoolean(IsAllday),
								ExternalID = Id.ToString(),
							}
						);
					}
				} while (calList.MoveNext());

				calRecord?.Dispose();
				calRecord = null;

				calList?.Dispose();
				calList = null;

				calManager?.Dispose();
				calManager = null;

				return calendars;
			});
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
			CalendarEvent events = null;
			CalendarManager calManager = new CalendarManager();
			CalendarList calList = calManager.Database.GetAll(Event.Uri, 0, 0);
			CalendarRecord calRecord = calList.GetCurrentRecord();

			if (calList.Count == 0)
				return Task.Run(() => events);

			calList.MoveFirst();

			do
			{
				string summary = calRecord.Get<string>(Event.Summary);
				int IsAllday = calRecord.Get<int>(Event.IsAllday);
				CalendarTime sTime = Convert.ToBoolean(IsAllday) ? ConvertIntPtrToCalendarTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)) : calRecord.Get<CalendarTime>(Event.Start);
				CalendarTime End = Convert.ToBoolean(IsAllday) ? ConvertIntPtrToCalendarTime(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)) : calRecord.Get<CalendarTime>(Event.End);
				string location = calRecord.Get<string>(Event.Location);
				string description = calRecord.Get<string>(Event.Description);
				int Id = calRecord.Get<int>(Event.Id);
				if (Id.ToString() == externalId)
				{
					events = new CalendarEvent
					{
						Name = summary,
						Start = sTime.UtcTime,
						End = End.UtcTime,
						Location = location,
						Description = description,
						AllDay = Convert.ToBoolean(IsAllday),
						ExternalID = Id.ToString(),
					};
					break;
				}
			} while (calList.MoveNext());

			calRecord?.Dispose();
			calRecord = null;

			calList?.Dispose();
			calList = null;

			calManager?.Dispose();
			calManager = null;

			return Task.Run(() => events);
		}

		/// <summary>
		/// Creates a new calendar or updates the name and color of an existing one.
		/// </summary>
		/// <param name="calendar">The calendar to create/update</param>
		/// <exception cref="System.ArgumentException">Calendar does not exist on device or is read-only</exception>
		/// <exception cref="System.UnauthorizedAccessException">Calendar access denied</exception>
		/// <exception cref="Plugin.Calendars.Abstractions.PlatformException">Unexpected platform-specific error</exception>
		/// <exception cref="System.PlatformNotSupportedException">Platform does not support api</exception>
		public Task AddOrUpdateCalendarAsync(Calendar calendar)
		{
			throw new PlatformNotSupportedException("Tizen platform does not support add or update calendars");
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
			Calendar existingCal = null;

			if (!string.IsNullOrEmpty(calendar.ExternalID) || !string.IsNullOrEmpty(calendarEvent.ExternalID))
			{
				existingCal = await GetCalendarByIdAsync(calendar.ExternalID).ConfigureAwait(false);
			}

			if (calendarEvent.End < calendarEvent.Start)
			{
				throw new ArgumentException("End time may not precede start time", "calendarEvent");
			}

			if (!existingCal.CanEditEvents)
			{
				throw new ArgumentException("Cannot delete event from readonly calendar", "calendar");
			}
			else
			{
				CalendarManager calManager = new CalendarManager();
				CalendarRecord updateRecord = calManager.Database.Get(Event.Uri, Convert.ToInt32(calendarEvent.ExternalID));

				if (updateRecord != null)
				{
					var start = calendarEvent.AllDay
							? DateTime.SpecifyKind(calendarEvent.Start, DateTimeKind.Utc)
							: calendarEvent.Start;
					var end = calendarEvent.AllDay
							? DateTime.SpecifyKind(calendarEvent.End, DateTimeKind.Utc)
							: calendarEvent.End;
					updateRecord.Set(Event.Summary, calendarEvent.Name);
					updateRecord.Set(Event.Start, ConvertIntPtrToCalendarTime(start));
					updateRecord.Set(Event.End, ConvertIntPtrToCalendarTime(end));
					updateRecord.Set(Event.Location, calendarEvent.Location);
					updateRecord.Set(Event.Description, calendarEvent.Description);

					calManager.Database.Update(updateRecord);

					updateRecord?.Dispose();
					updateRecord = null;

					calManager?.Dispose();
					calManager = null;
				}
				else
				{
					CalendarRecord addRecord = new CalendarRecord(Event.Uri);

					var start = DateTime.SpecifyKind(calendarEvent.Start, DateTimeKind.Utc);
					var end = DateTime.SpecifyKind(calendarEvent.End, DateTimeKind.Utc);
					addRecord.Set(Event.Summary, calendarEvent.Name);
					addRecord.Set(Event.Start, ConvertIntPtrToCalendarTime(start));
					addRecord.Set(Event.End, ConvertIntPtrToCalendarTime(end));
					addRecord.Set(Event.Location, calendarEvent.Location);
					addRecord.Set(Event.Description, calendarEvent.Description);

					calManager.Database.Insert(addRecord);
					
					addRecord?.Dispose();
					addRecord = null;

					calManager?.Dispose();
					calManager = null;
				}
			}
		}

		private static CalendarTime ConvertIntPtrToCalendarTime(DateTime time)
		{
			if (CalendarTime.Type.Local.ToString() == time.Kind.ToString())
			{
				return new CalendarTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
			}
			else
			{
				return new CalendarTime(time.Ticks);
			}
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

			var existingEvt = await GetEventByIdAsync(calendarEvent.ExternalID).ConfigureAwait(false);
			if (existingEvt == null)
			{
				throw new ArgumentException("Specified calendar event not found on device");
			}

			CalendarManager calManager = new CalendarManager();
			CalendarRecord calRecord = calManager.Database.Get(Event.Uri, Convert.ToInt32(calendarEvent.ExternalID));
			CalendarTime sTime = calRecord.Get<CalendarTime>(Event.Start);
			CalendarTime time = new CalendarTime(sTime.UtcTime.AddMinutes(reminder?.TimeBefore.TotalMinutes ?? 15).Ticks);
			CalendarRecord calAlarm = new CalendarRecord(Alarm.Uri);

			calAlarm.Set<CalendarTime>(Alarm.AlarmTime, time);
			calAlarm.Set<int>(Alarm.TickUnit, (int)CalendarTypes.TickUnit.Specific);
			calRecord.AddChildRecord(Event.Alarm, calAlarm);
			calManager.Database.Update(calRecord);

			calAlarm?.Dispose();
			calAlarm = null;
			calRecord?.Dispose();
			calRecord = null;
			calManager?.Dispose();
			calManager = null;
			
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
		public Task<bool> DeleteCalendarAsync(Calendar calendar)
		{
			throw new PlatformNotSupportedException("Tizen platform doesn't support delete calendars");
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

			var existingCal = await GetCalendarByIdAsync(calendar.ExternalID).ConfigureAwait(false);
			if (existingCal == null)
			{
				return false;
			}
			else if (!existingCal.CanEditEvents)
			{
				throw new ArgumentException("Cannot delete event from readonly calendar", "calendar");
			}
			else
			{
				await Task.Run(() =>
				{
					CalendarManager calManager = new CalendarManager();
					CalendarList calList = calManager.Database.GetAll(Event.Uri, 0, 0);

					calManager.Database.Delete(Event.Uri, Convert.ToInt32(calendarEvent.ExternalID));
					//calManager.Database.Delete(calList);

					calList?.Dispose();
					calList = null;
					calManager?.Dispose();
					calManager = null;
				});

				return true;
			}
		}
	}
}
