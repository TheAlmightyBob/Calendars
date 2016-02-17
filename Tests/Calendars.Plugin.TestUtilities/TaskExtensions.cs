using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plugin.Calendars.TestUtilities
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Returns true if the task throws the specified exception type.
        /// The task result is ignored.
        /// </summary>
        /// <typeparam name="TException">Expected exception type</typeparam>
        /// <param name="task">Task to test</param>
        public static async Task<bool> ThrowsAsync<TException>(this Task task) where TException : Exception
        {
            bool threw = false;

            try
            {
                await task;
            }
            catch (TException)
            {
                threw = true;
            }

            return threw;
        }
    }
}
