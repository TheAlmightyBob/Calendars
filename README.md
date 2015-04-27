## Calendar API plugin for Xamarin and Windows Phone

Cross-platform plugin for querying and modifying device calendars. Supports basic CRUD operations with calendars and events.

WARNING: I do not recommend using this to edit events that it did not create, as data could be lost if those events use fields that it does not yet support. (see Limitations for details)

### Setup & Usage
* Available on NuGet: http://www.nuget.org/packages/CClarke.Plugin.Calendars
* Install into your PCL project and Client projects.
* Call CrossCalendars.Current from any project or PCL to gain access to APIs.

**Supports**
* Xamarin.iOS
* Xamarin.iOS (x64 Unified)
* Xamarin.Android
* Windows Phone 8.1 Silverlight
* Windows Phone 8.1 RT

Windows Store 8.1 currently just throws NotSupportedException (as the platform does not provide a corresponding native API).

### Platform Notes:
* Android:
  * Requires ReadCalendar & WriteCalendar permissions.
  * Android calendars have additional "account name" and "owner account" properties. By default, this will set those properties for new calendars according to the application package label. However, custom names can be set via the Android implementation class.
* Windows Phone:
  * Calendar color is read-only.
* iOS:
  * Calendar permission will be requested the first time any API function is called, if it has not already been granted.
  * The end time for all-day events will be returned as midnight of the following day (which is consistent with WinPhone/Android, but different from native iOS).

### Limitations:
* Recurring events are not currently supported. At all. This should not be used to edit existing recurring events. Bad things will likely happen.
* Reminders, location, meeting attendees, and other custom fields are also not supported.
* Async is a lie on Android and iOS. Windows Phone provides a native async API, so to provide a common API abstraction, the Android and iOS implementations use background threads.
* Some performance tradeoffs were made in the interest of providing a clear and consistent API across all platforms with helpful error checking. Android in particular provides certain optimizations for activities that are unavailable to a cross-platform API. However, this may still useful to an Android app that is not constantly re-querying calendar data, or to one that is written with Xamarin.Forms.
* Does not currently provide access to the native platform UIs.

tl;dr: You probably don't want to use this to write a replacement calendar app.

### Developer Notes:
The solution configurations are a bit unconventional. I am unable to use Visual Studio with Xamarin.Android, so it is specifically excluded from the AnyCPU and Mixed Platform configurations.
