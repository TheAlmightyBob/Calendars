using Calendars.Plugin.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Calendars.Plugin.TestUtilities
{
    public class EventComparer : IComparer<CalendarEvent>, IComparer
    {
        // This is supported by NUnit and preferable to IComparer, but MSTest only supports IComparer,
        // and NUnit also supports IComparer, so....
        //
        //#region EqualityComparer

        //public override bool Equals(CalendarEvent x, CalendarEvent y)
        //{
        //    return x.Name == y.Name
        //        && x.Description == y.Description
        //        && x.AllDay == y.AllDay
        //        && RoundToMS(x.Start) == RoundToMS(y.Start)
        //        && RoundToMS(x.End) == RoundToMS(y.End);
        //}

        //public override int GetHashCode(CalendarEvent obj)
        //{
        //    return obj.GetHashCode();
        //}

        //#endregion

        #region IComparer<CalendarEvent>

        public int Compare(CalendarEvent x, CalendarEvent y)
        {
            var retval = string.Compare(x.Name, y.Name);

            if (retval == 0)
            {
                retval = string.Compare(x.Description, y.Description);
            }

            if (retval == 0)
            {
                if (x.AllDay && !y.AllDay)
                {
                    retval = 1;
                }
                else if (!x.AllDay && y.AllDay)
                {
                    retval = -1;
                }
            }

            if (retval == 0)
            {
                retval = (int)(RoundToMS(x.Start) - RoundToMS(y.Start)).Ticks;
            }

            if (retval == 0)
            {
                retval = (int)(RoundToMS(x.Start) - RoundToMS(y.Start)).Ticks;
            }

            return retval;
        }

        #endregion

        #region IComparer

        public int Compare(object x, object y)
        {
            var eventX = x as CalendarEvent;
            var eventY = y as CalendarEvent;

            if (eventX == null)
            {
                if (eventY == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (eventY == null)
            {
                return 1;
            }

            return Compare(eventX, eventY);
        }

        #endregion

        #region Private Static Helpers

        private static DateTime RoundToMS(DateTime dt)
        {
            return dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerMillisecond));
        }

        #endregion
    }
}
