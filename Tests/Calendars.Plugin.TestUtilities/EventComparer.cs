using Plugin.Calendars.Abstractions;
using System.Collections;
using System.Collections.Generic;

namespace Plugin.Calendars.TestUtilities
{
    public class EventComparer : IComparer<CalendarEvent>, IComparer
    {
        private DateTimeComparer _dateTimeComparer;

        public EventComparer(Rounding rounding)
        {
            _dateTimeComparer = new DateTimeComparer(rounding);
        }
        
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
                retval = _dateTimeComparer.Compare(x.Start, y.Start);
            }

            if (retval == 0)
            {
                retval = _dateTimeComparer.Compare(x.End, y.End);
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
    }
}
