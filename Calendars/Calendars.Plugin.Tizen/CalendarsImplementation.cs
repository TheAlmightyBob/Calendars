using System;
using System.Collections.Generic;
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
			var calendars = new List<Calendar>();
			CalendarRecord calRecord = null;

			try
			{
				calRecord = new CalendarRecord(Book.Uri);
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
			}
			finally
			{
				calRecord?.Dispose();
				calRecord = null;
			}
			return Task.FromResult<IList<Calendar>>(calendars);
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
			Calendar calendar = null;
			CalendarRecord calRecord = null;

			try
			{
				calendar = new Calendar();
				calRecord = new CalendarRecord(Book.Uri);
				calRecord.Set<string>(Book.Name, "org.tizen.calendar");
				calendar.Name = calRecord.Get<string>(Book.Name);
				calendar.ExternalID = calRecord.Get<int>(Book.Id).ToString();
				calendar.CanEditEvents = true;
				calendar.CanEditCalendar = false;
				calendar.AccountName = calRecord.Get<int>(Book.AccountId).ToString();
				calendar.Color = calRecord.Get<string>(Book.Color);
			}
			finally
			{
				calRecord?.Dispose();
				calRecord = null;
			}
			return Task.FromResult(calendar);
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
			var calEvent = new List<CalendarEvent>();
			CalendarManager calManager = null;
			CalendarQuery calQuery = null;
			CalendarList calList = null;
			CalendarRecord calRecord = null;
			CalendarFilter calFilter = null;

			try
			{
				calManager = new CalendarManager();
				calFilter.AddCondition(CalendarFilter.LogicalOperator.And, Event.Start, CalendarFilter.IntegerMatchType.GreaterThanOrEqual, ConvertIntPtrToCalendarTime(start));
				calFilter.AddCondition(CalendarFilter.LogicalOperator.And, Event.End, CalendarFilter.IntegerMatchType.LessThanOrEqual, ConvertIntPtrToCalendarTime(end));
				calQuery = new CalendarQuery(Event.Uri);
				calQuery.SetFilter(calFilter);
				calList = calManager.Database.GetRecordsWithQuery(calQuery, 0, 0);

				if (calList.Count == 0)
					return Task.FromResult<IList<CalendarEvent>>(calEvent);

				calList.MoveFirst();

				do
				{
					calRecord = calList.GetCurrentRecord();
					string summary = calRecord.Get<string>(Event.Summary);
					CalendarTime startTime = calRecord.Get<CalendarTime>(Event.Start);
					CalendarTime endTime = calRecord.Get<CalendarTime>(Event.End);
					string location = calRecord.Get<string>(Event.Location);
					string description = calRecord.Get<string>(Event.Description);
					int IsAllday = calRecord.Get<int>(Event.IsAllday);
					int Id = calRecord.Get<int>(Event.Id);

					calEvent.Add(
						new CalendarEvent
						{
							Name = summary,
							Start = startTime.UtcTime,
							End = endTime.UtcTime,
							Location = location,
							Description = description,
							AllDay = Convert.ToBoolean(IsAllday),
							ExternalID = Id.ToString(),
						}
					);
				} while (calList.MoveNext());
			}
			finally
			{
				calRecord?.Dispose();
				calRecord = null;

				calFilter?.Dispose();
				calFilter = null;

				calQuery?.Dispose();
				calQuery = null;

				calList?.Dispose();
				calList = null;

				calManager?.Dispose();
				calManager = null;
			}
			return Task.FromResult<IList<CalendarEvent>>(calEvent);
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
			CalendarEvent calEvent = null;
			CalendarManager calManager = null;
			CalendarRecord calRecord = null;

			try
			{
				calManager = new CalendarManager();
				calRecord = calManager.Database.Get(Event.Uri, Convert.ToInt32(externalId));

				string summary = calRecord.Get<string>(Event.Summary);
				CalendarTime startTime =calRecord.Get<CalendarTime>(Event.Start);
				CalendarTime endTime =calRecord.Get<CalendarTime>(Event.End);
				string location = calRecord.Get<string>(Event.Location);
				string description = calRecord.Get<string>(Event.Description);
				int IsAllday = calRecord.Get<int>(Event.IsAllday);
				int Id = calRecord.Get<int>(Event.Id);

				calEvent = new CalendarEvent
				{
					Name = summary,
					Start = startTime.UtcTime,
					End = endTime.UtcTime,
					Location = location,
					Description = description,
					AllDay = Convert.ToBoolean(IsAllday),
					ExternalID = Id.ToString(),
				};
			}
			finally
			{
				calRecord?.Dispose();
				calRecord = null;

				calManager?.Dispose();
				calManager = null;
			}
			return Task.FromResult(calEvent);
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
				CalendarManager calManager = null;
				CalendarRecord updateRecord = null;

				try
				{
					calManager = new CalendarManager();
					updateRecord = calManager.Database.Get(Event.Uri, Convert.ToInt32(calendarEvent.ExternalID));

					if (updateRecord != null)
					{
						try
						{
							DateTime startTime = calendarEvent.AllDay
									? DateTime.SpecifyKind(calendarEvent.Start, DateTimeKind.Utc)
									: calendarEvent.Start;
							DateTime endTime = calendarEvent.AllDay
									? DateTime.SpecifyKind(calendarEvent.End, DateTimeKind.Utc)
									: calendarEvent.End;
							updateRecord.Set(Event.Summary, calendarEvent.Name);
							updateRecord.Set(Event.Start, ConvertIntPtrToCalendarTime(startTime));
							updateRecord.Set(Event.End, ConvertIntPtrToCalendarTime(endTime));
							updateRecord.Set(Event.Location, calendarEvent.Location);
							updateRecord.Set(Event.Description, calendarEvent.Description);

							calManager.Database.Update(updateRecord);
						}
						finally
						{
							updateRecord?.Dispose();
							updateRecord = null;
						}
					}
					else
					{
						CalendarRecord addRecord = null;

						try
						{
							addRecord = new CalendarRecord(Event.Uri);

							var start = DateTime.SpecifyKind(calendarEvent.Start, DateTimeKind.Utc);
							var end = DateTime.SpecifyKind(calendarEvent.End, DateTimeKind.Utc);
							addRecord.Set(Event.Summary, calendarEvent.Name);
							addRecord.Set(Event.Start, ConvertIntPtrToCalendarTime(start));
							addRecord.Set(Event.End, ConvertIntPtrToCalendarTime(end));
							addRecord.Set(Event.Location, calendarEvent.Location);
							addRecord.Set(Event.Description, calendarEvent.Description);

							calManager.Database.Insert(addRecord);
						}
						finally
						{
							addRecord?.Dispose();
							addRecord = null;
						}
					}
				}
				finally
				{
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

			CalendarManager calManager = null;
			CalendarRecord calRecord = null, calAlarm = null;
			CalendarTime startTime = null, calTime = null;

			try
			{
				calManager = new CalendarManager();
				calRecord = calManager.Database.Get(Event.Uri, Convert.ToInt32(calendarEvent.ExternalID));
				startTime = calRecord.Get<CalendarTime>(Event.Start);
				calTime = new CalendarTime(startTime.UtcTime.AddMinutes(reminder?.TimeBefore.TotalMinutes ?? 15).Ticks);
				calAlarm = new CalendarRecord(Alarm.Uri);

				calAlarm.Set<CalendarTime>(Alarm.AlarmTime, calTime);
				calAlarm.Set<int>(Alarm.TickUnit, (int)CalendarTypes.TickUnit.Specific);
				calRecord.AddChildRecord(Event.Alarm, calAlarm);
				calManager.Database.Update(calRecord);
			}
			finally
			{
				calAlarm?.Dispose();
				calAlarm = null;

				calRecord?.Dispose();
				calRecord = null;

				calManager?.Dispose();
				calManager = null;
			}
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
				CalendarManager calManager = null;

				try
				{
					calManager = new CalendarManager();
					calManager.Database.Delete(Event.Uri, Convert.ToInt32(calendarEvent.ExternalID));
				}
				finally
				{
					calManager?.Dispose();
					calManager = null;
				}
				return true;
			}
		}
	}
}
