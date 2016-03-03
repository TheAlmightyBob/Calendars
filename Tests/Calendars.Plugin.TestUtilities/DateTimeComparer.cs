using System;
using System.Collections;
using System.Collections.Generic;

namespace Plugin.Calendars.TestUtilities
{
    /// <summary>
    /// Rounds to seconds/milliseconds because device calendars may not store
    /// greater precision than that (and that's fine, nobody cares).
    /// </summary>
    public class DateTimeComparer : IComparer<DateTime>, IComparer
    {
        private bool _includeMS;

        public DateTimeComparer(bool includeMS)
        {
            _includeMS = includeMS;
        }
        
        #region IComparer<DateTime>

        public int Compare(DateTime x, DateTime y)
        {
            return (int)(Round(x) - Round(y)).Ticks;
        }

        #endregion

        #region IComparer

        public int Compare(object x, object y)
        {
            var dtX = x as DateTime?;
            var dtY = y as DateTime?;

            if (dtX == null)
            {
                if (dtY == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else if (dtY == null)
            {
                return 1;
            }

            return Compare(dtX, dtY);
        }

        #endregion

        #region Private Methods

        private DateTime Round(DateTime dt)
        {
            return _includeMS ? dt.RoundToMS() : dt.RoundToSeconds();
        }

        #endregion
    }
}
