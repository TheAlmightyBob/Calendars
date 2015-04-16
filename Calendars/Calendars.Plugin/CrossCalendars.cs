using Calendars.Plugin.Abstractions;
using System;

namespace Calendars.Plugin
{
  /// <summary>
  /// Cross platform Calendars implemenations
  /// </summary>
  public class CrossCalendars
  {
    static Lazy<ICalendars> Implementation = new Lazy<ICalendars>(() => CreateCalendars(), System.Threading.LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Current settings to use
    /// </summary>
    public static ICalendars Current
    {
      get
      {
        var ret = Implementation.Value;
        if (ret == null)
        {
          throw NotImplementedInReferenceAssembly();
        }
        return ret;
      }
    }

    static ICalendars CreateCalendars()
    {
#if PORTABLE
        return null;
#else
        return new CalendarsImplementation();
#endif
    }

    internal static Exception NotImplementedInReferenceAssembly()
    {
      return new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
    }
  }
}
