using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Calendars.Plugin.Abstractions;
using Calendars.Plugin.TestUtilities;

namespace Calendars.Plugin.Android.Tests
{
    [TestFixture]
    [Category("Android")]
    class CalendarTests
    {
        private const string _calendarName = "Calendars.Plugin.Android.Tests.TestCalendar";
        private EventComparer _eventComparer;

        private CalendarsImplementation _service;

        [SetUp]
        public void Setup()
        {
            _service = new CalendarsImplementation();
            _eventComparer = new EventComparer();
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

        // No default calendars on Android...
//        [Test]
//        public async void Calendars_GetCalendars_ReturnsAtLeastOneCalendar()
//        {
//            var cals = await _service.GetCalendarsAsync();
//
//            Assert.IsNotNull(cals);
//            Assert.IsTrue(cals.Count > 0);
//        }

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
            Assert.IsFalse(string.IsNullOrWhiteSpace(calendar.Color), "Missing color");
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

            Assert.IsTrue(await _service.AddOrUpdateCalendarAsync(calendar).ThrowsAsync<ArgumentException>(),
                "Exception wasn't thrown");
        }

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

        // No built-in readonly calendar on Android to test with...
//        [Test]
//        public async void Calendars_DeleteCalendar_ReadonlyThrowsException()
//        {
//            var allCals = await _service.GetCalendarsAsync();
//            var readonlyCals = allCals.Where(c => !c.CanEditCalendar);
//            var calendarToDelete = readonlyCals.First();
//
//            Assert.IsTrue(await _service.DeleteCalendarAsync(calendarToDelete).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
//        }

        [Test]
        public async void Calendars_AddOrUpdateEvents_AddsEvents()
        {
            var events = new List<CalendarEvent> {
                new CalendarEvent { Name = "Bob", Description = "Bob's event", Start = DateTime.Today.AddDays(5), End = DateTime.Today.AddDays(5).AddHours(2), AllDay = false },
                new CalendarEvent { Name = "Steve", Description = "Steve's event", Start = DateTime.Today.AddDays(7), End = DateTime.Today.AddDays(8), AllDay = true },
                new CalendarEvent { Name = "Wheeee", Description = "Fun times", Start = DateTime.Today.AddDays(13), End = DateTime.Today.AddDays(15), AllDay = true }
            };
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
            var calendarEvent = new CalendarEvent { Name = "Bob", Start = DateTime.Today, End = DateTime.Today.AddDays(-1) };
            var calendar = new Calendar { Name = _calendarName };

            await _service.AddOrUpdateCalendarAsync(calendar);

            Assert.IsTrue(await _service.AddOrUpdateEventAsync(calendar, calendarEvent).ThrowsAsync<ArgumentException>(),
                "Exception wasn't thrown");
        }

        [Test]
        public async void Calendars_AddOrUpdateEvent_UnspecifiedCalendarThrows()
        {
            var calendarEvent = new CalendarEvent { Name = "Bob", Start = DateTime.Today, End = DateTime.Today.AddHours(1) };
            var calendar = new Calendar { Name = _calendarName };

            Assert.IsTrue(await _service.AddOrUpdateEventAsync(calendar, calendarEvent).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
        }

        [Test]
        public async void Calendars_AddOrUpdateEvent_NonexistentCalendarThrows()
        {
            var calendarEvent = new CalendarEvent { Name = "Bob", Start = DateTime.Today, End = DateTime.Today.AddHours(1) };
            var calendar = new Calendar { Name = _calendarName };

            // Create/delete calendar so we have a valid ID for a nonexistent calendar
            //
            await _service.AddOrUpdateCalendarAsync(calendar);
            await _service.DeleteCalendarAsync(calendar);

            Assert.IsTrue(await _service.AddOrUpdateEventAsync(calendar, calendarEvent).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
        }

        // No built-in readonly calendar on Android to test with...
//        [Test]
//        public async void Calendars_AddOrUpdateEvent_ReadonlyCalendarThrows()
//        {
//        }

        [Test]
        public async void Calendars_AddOrUpdateEvents_UpdatesEvents()
        {
            var originalEvents = new List<CalendarEvent> {
                new CalendarEvent { Name = "Bob", Description = "Bob's event", Start = DateTime.Today.AddDays(5), End = DateTime.Today.AddDays(5).AddHours(2), AllDay = false },
                new CalendarEvent { Name = "Steve", Description = "Steve's event", Start = DateTime.Today.AddDays(7), End = DateTime.Today.AddDays(8), AllDay = true },
                new CalendarEvent { Name = "Wheeee", Description = "Fun times", Start = DateTime.Today.AddDays(13), End = DateTime.Today.AddDays(15), AllDay = true }
            };
            var editedEvents = new List<CalendarEvent> {
                new CalendarEvent { Name = "Bob (edited)", Description = "Bob's edited event", Start = DateTime.Today.AddDays(5).AddHours(-2), End = DateTime.Today.AddDays(5).AddHours(1), AllDay = false },
                new CalendarEvent { Name = "Steve (edited)", Description = "Steve's edited event", Start = DateTime.Today.AddDays(6), End = DateTime.Today.AddDays(7).AddHours(-1), AllDay = false },
                new CalendarEvent { Name = "Yay (edited)", Description = "Edited fun times", Start = DateTime.Today.AddDays(12), End = DateTime.Today.AddDays(13), AllDay = true }
            };
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
            var calendarEvent = new CalendarEvent
            {
                Name = "Bob",
                Start = DateTime.Today.AddDays(5),
                End = DateTime.Today.AddDays(5).AddHours(2),
                AllDay = false
            };
            var calendarSource = new Calendar { Name = _calendarName };
            var calendarTarget = new Calendar { Name = _calendarName + " copy destination" };

            await _service.AddOrUpdateCalendarAsync(calendarSource);
            await _service.AddOrUpdateCalendarAsync(calendarTarget);

            await _service.AddOrUpdateEventAsync(calendarSource, calendarEvent);

            var sourceEvents = await _service.GetEventsAsync(calendarSource, DateTime.Today, DateTime.Today.AddDays(30));

            await _service.AddOrUpdateEventAsync(calendarTarget, sourceEvents.First());

            var targetEvents = await _service.GetEventsAsync(calendarTarget, DateTime.Today, DateTime.Today.AddDays(30));

            // Requery source events, just to be extra sure
            sourceEvents = await _service.GetEventsAsync(calendarSource, DateTime.Today, DateTime.Today.AddDays(30));

            // Make sure the events are the same...
            Assert.That(targetEvents, Is.EqualTo(sourceEvents).Using<CalendarEvent>(_eventComparer));

            // ...except for their IDs! (i.e., they are actually unique copies)
            Assert.That(targetEvents.Select(e => e.ExternalID).ToList(), Is.Not.EqualTo(sourceEvents.Select(e => e.ExternalID).ToList()));
        }

        [Test]
        public async void Calendars_GetEvents_NonexistentCalendarThrowsException()
        {
            var calendar = new Calendar { Name = "Bob", ExternalID = "42" };

            Assert.IsTrue(await _service.GetEventsAsync(calendar, DateTime.Today, DateTime.Today.AddDays(30)).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
        }

        [Test]
        public async void Calendars_DeleteEvent_DeletesExistingEvent()
        {
            var calendarEvent = new CalendarEvent
            {
                Name = "Bob",
                Start = DateTime.Now.AddDays(5),
                End = DateTime.Now.AddDays(5).AddHours(2),
                AllDay = false
            };
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
            var calendarEvent = new CalendarEvent
            {
                Name = "Bob",
                Start = DateTime.Now.AddDays(5),
                End = DateTime.Now.AddDays(5).AddHours(2),
                AllDay = false
            };
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
            var calendarEvent = new CalendarEvent
            {
                Name = "Bob",
                Start = DateTime.Now.AddDays(5),
                End = DateTime.Now.AddDays(5).AddHours(2),
                AllDay = false
            };
            await _service.AddOrUpdateEventAsync(calendar, calendarEvent);
            await _service.DeleteCalendarAsync(calendar);

            Assert.IsFalse(await _service.DeleteEventAsync(calendar, calendarEvent));
        }

        [Test]
        public async void Calendars_DeleteEvent_WrongCalendarReturnsFalse()
        {
            var calendar1 = await _service.CreateCalendarAsync(_calendarName);
            var calendar2 = await _service.CreateCalendarAsync(_calendarName + "2");
            var calendarEvent = new CalendarEvent
            {
                Name = "Bob",
                Start = DateTime.Now.AddDays(5),
                End = DateTime.Now.AddDays(5).AddHours(2),
                AllDay = false
            };
            await _service.AddOrUpdateEventAsync(calendar1, calendarEvent);

            Assert.IsFalse(await _service.DeleteEventAsync(calendar2, calendarEvent));
        }

        // No built-in readonly calendar on Android to test with...
//        [Test]
//        public async void Calendars_DeleteEvent_ReadonlyCalendarThrowsException()
//        {
//            var calendars = await _service.GetCalendarsAsync();
//            var readonlyCalendars = calendars.Where(c => !c.CanEditEvents).ToList();
//            var readonlyCalendar = readonlyCalendars.First();
//
//            // Note: An invalid event ID may also throw an ArgumentException...
//            //       But the default readonly calendar doesn't have any events to get the ID of...
//
//            Assert.IsTrue(await _service.DeleteEventAsync(readonlyCalendar, new CalendarEvent { ExternalID = "42" }).ThrowsAsync<ArgumentException>(), "Exception wasn't thrown");
//        }
    }
}

