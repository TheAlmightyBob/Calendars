using Plugin.Calendars.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Calendars.TestUtilities
{
    public class CalendarComparer : IComparer<Calendar>, IComparer
    {
        #region IComparer<Calendar>

        public int Compare(Calendar x, Calendar y)
        {
            var retval = string.Compare(x.ExternalID, y.ExternalID);

            if (retval == 0)
            {
                retval = string.Compare(x.Name, y.Name);
            }

            if (retval == 0 && x.CanEditCalendar != y.CanEditCalendar)
            {
                retval = x.CanEditCalendar ? 1 : -1;
            }

            if (retval == 0 && x.CanEditEvents != y.CanEditEvents)
            {
                retval = x.CanEditEvents ? 1 : -1;
            }

            if (retval == 0)
            {
                retval = string.Compare(x.Color, y.Color);
            }

            return retval;
        }

        #endregion

        #region IComparer

        public int Compare(object x, object y)
        {
            var calendarX = x as Calendar;
            var calendarY = y as Calendar;

            if (calendarX == null)
            {
                if (calendarY == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (calendarY == null)
            {
                return 1;
            }

            return Compare(calendarX, calendarY);
        }

        #endregion
    }
}
