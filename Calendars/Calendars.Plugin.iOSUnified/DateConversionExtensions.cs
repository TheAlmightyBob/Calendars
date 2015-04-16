using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if __UNIFIED__
using Foundation;
using UIKit;
#else
using MonoTouch.Foundation;
using MonoTouch.UIKit;
#endif

namespace Calendars.Plugin
{
    /// <summary>
    /// iOS date conversion extension helpers, based on Xamarin sample for Unified migration
    /// </summary>
    internal static class DateConversionExtensions
    {
        // Xamarin example had this in local time, but that does not work with daylight savings...
        //
        private static DateTime _reference =
            new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Converts NSDate to System.DateTime
        /// </summary>
        /// <param name="date">Original NSDate</param>
        /// <returns>Corresponding System.DateTime</returns>
        public static DateTime ToDateTime(this NSDate date)
        {
            return _reference.AddSeconds(date.SecondsSinceReferenceDate).ToLocalTime();
        }

        /// <summary>
        /// Converts System.DateTime to NSDate
        /// </summary>
        /// <param name="date">Original System.DateTime</param>
        /// <returns>Corresponding NSDate</returns>
        public static NSDate ToNSDate(this DateTime date)
        {
            return NSDate.FromTimeIntervalSinceReferenceDate(
                (date.ToUniversalTime() - _reference).TotalSeconds);
        }
    }
}