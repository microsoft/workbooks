# Version @BUILD_DOCUMENTATION_VERSION@

* Bump .NET Core support to 2.2 final.

* Fix blank representations in 3D view when using Android Pie emulator. Users
  should stick with Oreo or earlier emulators for best 3D view performance at
  this time.

* Fix a crash when selecting certain items from the 3D view on Mac.

* Force light mode on Mojave. Proper dark mode support will come later.

* Fix wrong error message being displayed when a Xamarin.Forms workbook fails to
  connect due to emulator issues.

# Version 1.5.0

Please refer to the [detailed release notes][docs-detailed-release-notes] and
full product documentation for [Workbooks][docs-workbooks] and
[Inspector][docs-inspector] for complete information.

This is a minor update to the [1.4 series][14-series].

## New & Improved

* Add support for iOS 12, Xcode 10, and macOS 10.13 SDKs.

* Prefer iPhone X to iPhone 5s for running iOS workbooks.

* Bump .NET Core support to the 2.2 preview SDK.

* Add support for C# 7.3.

* Bump NuGet to 4.8.0 for increased package support.

## Notable Bug Fixes

* Support launching emulators with latest Android SDK.

## Known Issues

* NuGet Limitations
  - Native libraries are supported only on iOS, and only when linked with
    the managed library.
  - Packages which depend on `.targets` files or PowerShell scripts will likely
    fail to work as expected.
  - To modify a package dependency, edit the workbook's manifest with
    a text editor. A more complete package management UI is on the way.

[github]: https://github.com/Microsoft/workbooks

[docs-workbooks]: https://developer.xamarin.com/guides/cross-platform/workbooks/
[docs-inspector]: https://developer.xamarin.com/guides/cross-platform/inspector/
[docs-detailed-release-notes]: https://developer.xamarin.com/releases/interactive/interactive-1.5/
[docs-workbooks-logs]: https://developer.xamarin.com/guides/cross-platform/workbooks/install/#Log_Files
[14-series]: https://developer.xamarin.com/releases/interactive/interactive-1.4