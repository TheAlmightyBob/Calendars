using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;

#nullable enable

namespace Plugin.Calendars
{
    /// <summary>
    /// IAsyncOperation extensions
    /// </summary>
    internal static class IAsyncOperationExtensions
    {
        /// <summary>
        /// Enables Task's ConfigureAwait for IAsyncOperations
        /// </summary>
        /// <param name="op">Source IAsyncOperation</param>
        /// <param name="continueOnCapturedContext">true to attempt to marshal the continuation back to the original context
        ///                                         captured; otherwise, false.</param>
        /// <returns>An object used to await this IAsyncOperation</returns>
        public static ConfiguredTaskAwaitable<T> ConfigureAwait<T>(this IAsyncOperation<T> op, bool continueOnCapturedContext)
        {
            return op.AsTask().ConfigureAwait(continueOnCapturedContext);
        }
    }
}
