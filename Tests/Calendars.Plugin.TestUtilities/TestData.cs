using System;
using System.Collections.Generic;
using Plugin.Calendars.Abstractions;

namespace Plugin.Calendars.TestUtilities
{
    public static class TestData
    {
        public static CalendarEvent GetTestEvent()
        {
            return new CalendarEvent
            {
                Name = "Bob",
                Start = DateTime.Now.AddDays(5),
                End = DateTime.Now.AddDays(5).AddHours(2),
                AllDay = false,
                Location = "42 Mercy St."
            };
        }

        public static List<CalendarEvent> GetTestEvents()
        {
            return new List<CalendarEvent> {
                new CalendarEvent { Name = "Bob", Description = "Bob's event", Start = DateTime.Today.AddDays(5), End = DateTime.Today.AddDays(5).AddHours(2), AllDay = false, Location = "here" },
                new CalendarEvent { Name = "Steve", Description = "Steve's event", Start = DateTime.Today.AddDays(7), End = DateTime.Today.AddDays(8), AllDay = true, Location = "there" },
                new CalendarEvent { Name = "Wheeee", Description = "Fun times", Start = DateTime.Today.AddDays(13), End = DateTime.Today.AddDays(15), AllDay = true, Location = "everywhere" }
            };
        }

        public static List<CalendarEvent> GetEditedTestEvents()
        {
            return new List<CalendarEvent> {
                new CalendarEvent { Name = "Bob (edited)", Description = "Bob's edited event", Start = DateTime.Today.AddDays(5).AddHours(-2), End = DateTime.Today.AddDays(5).AddHours(1), AllDay = false, Location = "nowhere, man" },
                new CalendarEvent { Name = "Steve (edited)", Description = "Steve's edited event", Start = DateTime.Today.AddDays(6), End = DateTime.Today.AddDays(7).AddHours(-1), AllDay = false, Location = "SPAAAAAAAAACE!" },
                new CalendarEvent { Name = "Yay (edited)", Description = "Edited fun times", Start = DateTime.Today.AddDays(12), End = DateTime.Today.AddDays(13), AllDay = true, Location = "A small planet somewhere in the vicinity of Betelgeuse" }
            };
        }
    }
}

