using System;
using System.Collections;
using System.Collections.Generic;

namespace Plugin.Calendars.TestUtilities
{
    /// <summary>
    /// Rounds to seconds/milliseconds/minutes because device calendars
    /// may not store greater precision than that.
    /// </summary>
    public class DateTimeComparer : IComparer<DateTime>, IComparer
    {
        private Rounding _rounding;

        public DateTimeComparer(Rounding rounding = Rounding.None)
        {
            _rounding = rounding;
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
            switch (_rounding)
            {
                case Rounding.None:
                    return dt;

                case Rounding.Milliseconds:
                    return dt.RoundToMS();

                case Rounding.Seconds:
                    return dt.RoundToSeconds();

                case Rounding.Minutes:
                    return dt.RoundToMinutes();

                default:
                    throw new NotImplementedException("Unsupported rounding type");
            }
        }

        #endregion
    }

    /// <summary>
    /// Android/iOS/Windows each support a different level of time precision
    /// when saving events.
    /// </summary>
    public enum Rounding
    {
        None,
        Milliseconds,
        Seconds,
        Minutes
    }
}
