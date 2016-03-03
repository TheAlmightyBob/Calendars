using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using Plugin.Calendars.Abstractions;
using Plugin.Calendars.TestUtilities;

using static Plugin.Calendars.TestUtilities.TestData;

#if __UNIFIED__
namespace Plugin.Calendars.iOSUnified.Tests
#elif __IOS__
namespace Plugin.Calendars.iOS.Tests
#else
namespace Plugin.Calendars.Android.Tests
#endif
{
    [TestFixture]
#if __UNIFIED__
    [Category("iOSUnified")]
#elif __IOS__
    [Category("iOS")]
#else
    [Category("Android")]
#endif
    class CalendarTests
    {
#if __UNIFIED__
        private const string _calendarName = "Plugin.Calendars.iOSUnified.Tests.TestCalendar";
#elif __IOS__
        private const string _calendarName = "Plugin.Calendars.iOS.Tests.TestCalendar";
#else
        private const string _calendarName = "Plugin.Calendars.Android.Tests.TestCalendar";
#endif
        private EventComparer _eventComparer;
        private DateTimeComparer _dateTimeComparer;

        private ICalendars _service;

        [SetUp]
        public void Setup()
        {
            _service = new CalendarsImplementation();

            // Android supports milliseconds, iOS supports seconds
#if __IOS__
            _eventComparer = new EventComparer(Rounding.Seconds);
            _dateTimeComparer = new DateTimeComparer(Rounding.Seconds);
#else
            _eventComparer = new EventComparer(Rounding.Milliseconds);
            _dateTimeComparer = new DateTimeComparer(Rounding.Milliseconds);
#endif
        }

        [TearDown]
        public void TearDown()
        {
            var calendars = _service.GetCalendarsAsync().Result;

            foreach (var calendar in calendars.Where(c => c.CanEditCalendar == true && c.Name.Contains(_calendarName)))
            {
                _service.DeleteCalendarAsync(calendar).Wait();
            }
        }

#if __IOS__ // No default calendars on Android...
        [Test]
        public async void Calendars_GetCalendars_ReturnsAtLeastOneCalendar()
        {
            var cals = await _service.GetCalendarsAsync();

            Assert.IsNotNull(cals);
            Assert.IsTrue(cals.Count > 0);
        }
#endif

        [Test]
        public async void Calendars_CreateCalendar_IsFoundByID()
        {
            var calendar = await _service.CreateCalendarAsync(_calendarName);

            Assert.IsNotNull(calendar);
            Assert.IsFalse(string.IsNullOrWhiteSpace(calendar.ExternalID));

            var calendarFromId = await _service.GetCalendarByIdAsync(calendar.ExternalID);

            Assert.IsNotNull(calendarFromId);
            Assert.AreEqual(calendar.Name, calendarFromId.Name);
            Assert.AreEqual(calendar.ExternalID, calendarFromId.ExternalID);
            Assert.AreEqual(calendar.CanEditCalendar, calendarFromId.CanEditCalendar);
            Assert.AreEqual(calendar.CanEditEvents, calendarFromId.CanEditEvents);
            Assert.IsFalse(string.IsNullOrWhiteSpace(calendarFromId.Color), "Missing color");
            Assert.AreEqual(calendar.Color, calendarFromId.Color);
        }

        [Test]
        public async void Calendars_CreateCalendar_IsFoundByGetCalendars()
        {
            await _service.CreateCalendarAsync(_calendarName);
            var calendars = await _service.GetCalendarsAsync();

            Assert.IsTrue(calendars.Any(c => c.Name == _calendarName));
        }

        [Test]
        public async void Calendars_GetCalendarByID_NonexistentReturnsNull()
        {
            // Create and delete a calendar so that we have an ID that is valid
            // but does not exist
            //
            var calendar = await _service.CreateCalendarAsync(_calendarName);
            await _service.DeleteCalendarAsync(calendar);

            Assert.IsNull(await _service.GetCalendarByIdAsync(calendar.ExternalID));
        }

        [Test]
        public async void Calendars_AddOrUpdateCalendar_UpdatesExistingCalendar()
        {
            var calendar = await _service.CreateCalendarAsync(_calendarName);

            calendar.Name = _calendarName + " (edited)";

            // edit
            await _service.AddOrUpdateCalendarAsync(calendar);

            var calendarResult = await _service.GetCalendarByIdAsync(calendar.ExternalID);

            Assert.AreEqual(calendar.Name, calendarResult.Name);
        }

        [Test]
        public async void Calendars_AddOrUpdateCalendar_NonexistentThrows()
        {
            // Create and delete a calendar so that we have an ID that is valid
            // but does not exist
            //
            var calendar = await _service.CreateCalendarAsync(_calendarName);
            await _service.DeleteCalendarAsync(calendar);

            Assert.IsTrue(await _service.AddOrUpdateCalendarAsync(calendar).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
        }

#if __IOS__ // no built-in readonly calendar on Android to test with
        [Test]
        public async void Calendars_AddOrUpdateCalendar_ReadonlyThrows()
        {
            var calendars = await _service.GetCalendarsAsync();
            var readonlyCalendars = calendars.Where(c => !c.CanEditEvents).ToList();

            // For iOS we can rely on the built-in read-only Birthdays calendar
            //
            var readonlyCalendar = readonlyCalendars.First(c => c.Name == "Birthdays");

            readonlyCalendar.Name += " (edited)";

            Assert.IsTrue(await _service.AddOrUpdateCalendarAsync(readonlyCalendar).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");

            // Further ensure that the service does not return the edited calendar
            //
            var newCalendars = await _service.GetCalendarsAsync();
            Assert.That(newCalendars.Any(c => c.Name == "Birthdays"), "Calendar name has changed, even after throwing");
        }
#endif

        [Test]
        public async void Calendars_DeleteCalendar_DeletesExistingCalendar()
        {
            var calendar = await _service.CreateCalendarAsync(_calendarName);

            Assert.IsNotNull(await _service.GetCalendarByIdAsync(calendar.ExternalID));

            bool deleted = await _service.DeleteCalendarAsync(calendar);

            Assert.IsTrue(deleted);
            Assert.IsNull(await _service.GetCalendarByIdAsync(calendar.ExternalID));
        }

        [Test]
        public async void Calendars_DeleteCalendar_NonexistentReturnsFalse()
        {
            // Create and delete a calendar so that we have an ID that is valid
            // but does not exist
            //
            var calendar = await _service.CreateCalendarAsync(_calendarName);
            await _service.DeleteCalendarAsync(calendar);

            Assert.IsFalse(await _service.DeleteCalendarAsync(calendar));
        }

#if __IOS__ // no built-in readonly calendar on Android to test with
        [Test]
        public async void Calendars_DeleteCalendar_ReadonlyThrows()
        {
            var allCals = await _service.GetCalendarsAsync();
            var readonlyCals = allCals.Where(c => !c.CanEditCalendar);
            var calendarToDelete = readonlyCals.First();

            Assert.IsTrue(await _service.DeleteCalendarAsync(calendarToDelete).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
        }
#endif

        [Test]
        public async void Calendars_AddOrUpdateEvents_AddsEvents()
        {
            var events = GetTestEvents();
            var calendar = new Calendar { Name = _calendarName };

            await _service.AddOrUpdateCalendarAsync(calendar);

            foreach (var cev in events)
            {
                await _service.AddOrUpdateEventAsync(calendar, cev);
            }

            var eventResults = await _service.GetEventsAsync(calendar, DateTime.Today, DateTime.Today.AddDays(30));

            Assert.That(eventResults, Is.EqualTo(events).Using<CalendarEvent>(_eventComparer));

            // Extra check that DateTime.Kinds are local
            Assert.AreEqual(DateTimeKind.Local, eventResults.Select(e => e.Start.Kind).Distinct().Single());
            Assert.AreEqual(DateTimeKind.Local, eventResults.Select(e => e.End.Kind).Distinct().Single());
        }

        [Test]
        public async void Calendars_AddOrUpdateEvents_StartAfterEndThrows()
        {
            var calendarEvent = new CalendarEvent
            {
                Name = "Time warp",
                Start = DateTime.Today,
                End = DateTime.Today.AddDays(-1)
            };
            var calendar = new Calendar { Name = _calendarName };

            await _service.AddOrUpdateCalendarAsync(calendar);

            Assert.IsTrue(await _service.AddOrUpdateEventAsync(calendar, calendarEvent).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");

            // Ensure that calendar still has no events
            Assert.That(await _service.GetEventsAsync(calendar, DateTime.Today.AddMonths(-1), DateTime.Today.AddMonths(1)), Is.Empty,
                "Calendar has event, even after throwing");
        }

        [Test]
        public async void Calendars_AddOrUpdateEvents_HandlesUTC()
        {
            var calendarEvent = GetTestEvent();
            var calendar = new Calendar { Name = _calendarName };

            var calendarEventUtc = new CalendarEvent
            {
                Name = calendarEvent.Name,
                Start = calendarEvent.Start.ToUniversalTime(),
                End = calendarEvent.End.ToUniversalTime()
            };

            await _service.AddOrUpdateCalendarAsync(calendar);

            await _service.AddOrUpdateEventAsync(calendar, calendarEventUtc);

            var eventFromId = await _service.GetEventByIdAsync(calendarEventUtc.ExternalID);

            Assert.That(eventFromId.Start, Is.EqualTo(calendarEvent.Start).Using<DateTime>(_dateTimeComparer));
            Assert.That(eventFromId.End, Is.EqualTo(calendarEvent.End).Using<DateTime>(_dateTimeComparer));
        }

        [Test]
        public async void Calendars_AddOrUpdateEvent_UnspecifiedCalendarThrows()
        {
            var calendarEvent = GetTestEvent();
            var calendar = new Calendar { Name = _calendarName };

            Assert.IsTrue(await _service.AddOrUpdateEventAsync(calendar, calendarEvent).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
        }

        [Test]
        public async void Calendars_AddOrUpdateEvent_NonexistentCalendarThrows()
        {
            var calendarEvent = GetTestEvent();
            var calendar = new Calendar { Name = _calendarName };

            // Create/delete calendar so we have a valid ID for a nonexistent calendar
            //
            await _service.AddOrUpdateCalendarAsync(calendar);
            await _service.DeleteCalendarAsync(calendar);

            Assert.IsTrue(await _service.AddOrUpdateEventAsync(calendar, calendarEvent).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
        }

#if __IOS__ // no built-in readonly calendar on Android to test with
        [Test]
        public async void Calendars_AddOrUpdateEvent_ReadonlyCalendarThrows()
        {
            var calendarEvent = GetTestEvent();
            var calendars = await _service.GetCalendarsAsync();
            var readonlyCalendars = calendars.Where(c => !c.CanEditEvents).ToList();

            // For iOS we can rely on the built-in read-only Birthdays calendar
            //
            var readonlyCalendar = readonlyCalendars.First(c => c.Name == "Birthdays");

            Assert.IsTrue(await _service.AddOrUpdateEventAsync(readonlyCalendar, calendarEvent).ThrowsAsync<PlatformException>(), "Exception wasn't thrown");

            // Ensure that calendar does not contain the event
            Assert.IsFalse((await _service.GetEventsAsync(readonlyCalendar, DateTime.Today.AddMonths(-1), DateTime.Today.AddMonths(1))).Any(e => e.Name == "Bob"),
                "Calendar contains the new event, even after throwing");
        }
#endif

        [Test]
        public async void Calendars_AddOrUpdateEvents_UpdatesEvents()
        {
            var originalEvents = GetTestEvents();
            var editedEvents = GetEditedTestEvents();
            var calendar = new Calendar { Name = _calendarName };
            var queryStartDate = DateTime.Today;
            var queryEndDate = queryStartDate.AddDays(30);

            await _service.AddOrUpdateCalendarAsync(calendar);

            foreach (var cev in originalEvents)
            {
                await _service.AddOrUpdateEventAsync(calendar, cev);
            }

            var eventResults = await _service.GetEventsAsync(calendar, queryStartDate, queryEndDate);

            Assert.That(eventResults, Is.EqualTo(originalEvents).Using<CalendarEvent>(_eventComparer));

            for (int i = 0; i < eventResults.Count; i++)
            {
                editedEvents.ElementAt(i).ExternalID = eventResults.ElementAt(i).ExternalID;
            }

            foreach (var cev in editedEvents)
            {
                await _service.AddOrUpdateEventAsync(calendar, cev);
            }

            var editedEventResults = await _service.GetEventsAsync(calendar, queryStartDate, queryEndDate);

            Assert.That(editedEventResults, Is.EqualTo(editedEvents).Using<CalendarEvent>(_eventComparer));
        }

        [Test]
        public async void Calendars_AddOrUpdateEvents_CopiesEventsBetweenCalendars()
        {
            var calendarEvent = GetTestEvent();
            var calendarSource = new Calendar { Name = _calendarName };
            var calendarTarget = new Calendar { Name = _calendarName + " copy destination" };

            await _service.AddOrUpdateCalendarAsync(calendarSource);
            await _service.AddOrUpdateCalendarAsync(calendarTarget);

            await _service.AddOrUpdateEventAsync(calendarSource, calendarEvent);

            var sourceEvents = await _service.GetEventsAsync(calendarSource, DateTime.Today, DateTime.Today.AddDays(30));

            foreach (var cev in sourceEvents)
            {
                await _service.AddOrUpdateEventAsync(calendarTarget, cev);
            }

            var targetEvents = await _service.GetEventsAsync(calendarTarget, DateTime.Today, DateTime.Today.AddDays(30));

            // Requery source events, just to be extra sure
            sourceEvents = await _service.GetEventsAsync(calendarSource, DateTime.Today, DateTime.Today.AddDays(30));

            // Make sure the events are the same...
            Assert.That(targetEvents, Is.EqualTo(sourceEvents).Using<CalendarEvent>(_eventComparer));

            // ...except for their IDs! (i.e., they are actually unique copies)
            Assert.That(targetEvents.Select(e => e.ExternalID).ToList(), Is.Not.EqualTo(sourceEvents.Select(e => e.ExternalID).ToList()));
        }

        [Test]
        public async void Calendars_GetEvents_NonexistentCalendarThrows()
        {
            var calendar = new Calendar { Name = "Bob", ExternalID = "42" };

            Assert.IsTrue(await _service.GetEventsAsync(calendar, DateTime.Today, DateTime.Today.AddDays(30)).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
        }

        [Test]
        public async void Calendars_DeleteEvent_DeletesExistingEvent()
        {
            var calendarEvent = GetTestEvent();
            var calendar = await _service.CreateCalendarAsync(_calendarName);

            await _service.AddOrUpdateEventAsync(calendar, calendarEvent);

            Assert.IsTrue(await _service.DeleteEventAsync(calendar, calendarEvent));

            Assert.IsNull(await _service.GetEventByIdAsync(calendarEvent.ExternalID));
        }

        [Test]
        public async void Calendars_DeleteEvent_NonexistentEventReturnsFalse()
        {
            var calendar = await _service.CreateCalendarAsync(_calendarName);

            // Create and delete an event so that we have a valid event ID for a nonexistent event
            //
            var calendarEvent = GetTestEvent();
            await _service.AddOrUpdateEventAsync(calendar, calendarEvent);
            await _service.DeleteEventAsync(calendar, calendarEvent);

            Assert.IsFalse(await _service.DeleteEventAsync(calendar, calendarEvent));
        }

        [Test]
        public async void Calendars_DeleteEvent_NonexistentCalendarReturnsFalse()
        {
            // Create and delete a calendar and event so that we have valid IDs for nonexistent calendar/event
            //
            var calendar = await _service.CreateCalendarAsync(_calendarName);
            var calendarEvent = GetTestEvent();
            await _service.AddOrUpdateEventAsync(calendar, calendarEvent);
            await _service.DeleteCalendarAsync(calendar);

            Assert.IsFalse(await _service.DeleteEventAsync(calendar, calendarEvent));
        }

        [Test]
        public async void Calendars_DeleteEvent_WrongCalendarReturnsFalse()
        {
            var calendar1 = await _service.CreateCalendarAsync(_calendarName);
            var calendar2 = await _service.CreateCalendarAsync(_calendarName + "2");
            var calendarEvent = GetTestEvent();
            await _service.AddOrUpdateEventAsync(calendar1, calendarEvent);

            Assert.IsFalse(await _service.DeleteEventAsync(calendar2, calendarEvent));
        }

#if __IOS__ // no built-in readonly calendar on Android to test with
        [Test]
        public async void Calendars_DeleteEvent_ReadonlyCalendarThrowsException()
        {
            var calendars = await _service.GetCalendarsAsync();
            var readonlyCalendars = calendars.Where(c => !c.CanEditEvents).ToList();

            // For iOS we can rely on the built-in read-only Birthdays calendar
            //
            var readonlyCalendar = readonlyCalendars.First(c => c.Name == "Birthdays");
            
            var kateBellsBirthday = new DateTime(2015, 1, 20);

            var events = await _service.GetEventsAsync(readonlyCalendar, kateBellsBirthday.AddDays(-1), kateBellsBirthday.AddDays(1));

            Assert.IsTrue(await _service.DeleteEventAsync(readonlyCalendar, events.First()).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");

            // Ensure calendar is still there
            Assert.IsTrue((await _service.GetCalendarsAsync()).Any(c => c.Name == "Birthdays"));
        }
#endif
    }
}