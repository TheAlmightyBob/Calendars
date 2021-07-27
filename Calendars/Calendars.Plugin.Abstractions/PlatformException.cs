using System;

namespace Plugin.Calendars.Abstractions
{
    /// <summary>
    /// Simple wrapper for iOS NSErrorExceptions and Android Java.Lang.Exception,
    /// to make it easier to catch them in PCL.
    /// </summary>
    public class PlatformException : Exception
    {
        /// <summary>
        /// Creates a platform exception
        /// </summary>
        /// <param name="message">Error description</param>
        /// <param name="innerException">Original platform-specific exception</param>
        public PlatformException(string? message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
