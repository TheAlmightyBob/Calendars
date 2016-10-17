## Calendar API plugin for Xamarin and Windows
[![Build status](https://ci.appveyor.com/api/projects/status/rdco9mbbsbvgf66r?svg=true)](https://ci.appveyor.com/project/TheAlmightyBob/calendars)

Cross-platform plugin for querying and modifying device calendars. Supports basic CRUD operations with calendars and events.
Try it out with the [Calendars Tester](https://github.com/TheAlmightyBob/CalendarsTester).

### Setup & Usage
* Available on NuGet: http://www.nuget.org/packages/CClarke.Plugin.Calendars [![NuGet](https://img.shields.io/nuget/v/CClarke.Plugin.Calendars.svg?label=NuGet)](https://www.nuget.org/packages/CClarke.Plugin.Calendars)
* Install into your PCL project and Client projects.
* Call CrossCalendars.Current from any project or PCL to gain access to APIs.

**Supports**
* Xamarin.iOS
* Xamarin.iOS (x64 Unified)
* Xamarin.Android
* Windows Phone 8.1 Silverlight
* Windows Phone 8.1 RT
* Universal Windows Platform (uap10.0 NuGet target)

Windows Store 8.1 just throws NotSupportedException (as the platform does not provide a corresponding native API).

### Platform Notes:
* Android:
  * Requires ReadCalendar & WriteCalendar permissions.
  * Android calendars have additional "account name" and "owner account" properties. By default, this will set those properties for new calendars according to the application package label. However, custom names can be set via the Android implementation class.
  * Unlike iOS, permissions will _not_ automatically be requested on Android Marshmallow. Check out the [Permissions Plugin](https://github.com/jamesmontemagno/Xamarin.Plugins/tree/master/Permissions) for help with this.
* Windows Phone & Universal Windows Platform:
  * Calendar color is read-only.
  * Requires the Appointments capability
* iOS:
  * (iOS 10+) Info.plist must include the NSCalendarsUsageDescription key with user-facing text that explains why your app desires calendar access. See [Apple docs](https://developer.apple.com/library/prerelease/content/documentation/General/Reference/InfoPlistKeyReference/Articles/CocoaKeys.html#//apple_ref/doc/uid/TP40009251-SW15).
  * Calendar permission will be requested when any API function is called, if it has not already been granted.
  * The end time for all-day events will be returned as midnight of the following day (which is consistent with WinPhone/Android, but different from native iOS).

### A Note on Creating Calendars:
* Android’s default Calendar app (the Google one) does not allow creating *or deleting* calendars. Most 3rd-party calendars do (including some provided by manufacturers)… it is not a limitation of Android itself. But it is worth being aware that it’s possible the user may not know how to later remove a calendar your app created.
* Windows does not allow 3rd-party apps to write to the default calendar. You *must* create an app-specific calendar in order to add events.
* iOS allows creating/deleting calendars, but it’s a bit tricky: it is very important to specify the correct “calendar source” (e.g. iCloud/Gmail/local) for the user’s device configuration (i.e. whether or not iCloud is enabled), otherwise it may be successfully created but hidden (both from the built-in calendar app and from the API). This library attempts to take care of that for you, but it is theoretically possible that it could fail.
  * Although iOS calendar app allows creating/deleting calendars, most 3rd-party calendar apps do *not* (quite contrary to the Android scenario). Possibly due to this complication.
* More discussion of this in [Issue #10](https://github.com/TheAlmightyBob/Calendars/issues/10)

### Limitations:
* Recurring events are not currently supported.
* Reminders can be created but not read/edited/removed.
* Meeting attendees and other custom fields are also not supported.
* Async is a lie on Android and iOS. Windows provides a native async API, so to provide a common API abstraction, the Android and iOS implementations use background threads.
* Some performance tradeoffs were made in the interest of providing a clear and consistent API across all platforms with helpful error checking. Android in particular provides certain optimizations for activities that are unavailable to a cross-platform API. However, this may still be useful to an Android app that is not constantly re-querying calendar data, or to one that is written with Xamarin.Forms.
* Does not currently provide access to the native platform UIs.

tl;dr: You probably don't want to use this to write a replacement calendar app.
