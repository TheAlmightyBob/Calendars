using Plugin.Calendars.Abstractions;
using System;
using System.Threading;

#nullable enable

namespace Plugin.Calendars
{
  /// <summary>
  /// Cross platform Calendars implementations
  /// </summary>
  public class CrossCalendars
  {
    static readonly Lazy<ICalendars?> Implementation = new(() => CreateCalendars(), LazyThreadSafetyMode.PublicationOnly);

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

    static ICalendars? CreateCalendars()
    {
#if NETSTANDARD1_0 || (NET && !ANDROID && !IOS && !MACCATALYST && !WINDOWS)
        return null;
#else
        return new CalendarsImplementation();
#endif
    }

    internal static Exception NotImplementedInReferenceAssembly()
    {
      return new NotImplementedException("This functionality is not implemented in the portable version of this assembly. You should reference the NuGet package from your main application project in order to reference the platform-specific implementation.");
    }
  }
}
