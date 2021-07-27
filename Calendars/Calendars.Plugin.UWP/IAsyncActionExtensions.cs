using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;

#nullable enable

namespace Plugin.Calendars
{
    /// <summary>
    /// IAsyncAction extensions
    /// </summary>
    internal static class IAsyncActionExtensions
    {
        /// <summary>
        /// Enables Task's ConfigureAwait for IAsyncActions
        /// </summary>
        /// <param name="action">Source IAsyncAction</param>
        /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context
        ///                                         captured; otherwise, false.</param>
        /// <returns>An object used to await this IAsyncAction</returns>
        public static ConfiguredTaskAwaitable ConfigureAwait(this IAsyncAction action, bool continueOnCapturedContext)
        {
            return action.AsTask().ConfigureAwait(continueOnCapturedContext);
        }
    }
}
