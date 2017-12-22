# Version @BUILD_DOCUMENTATION_VERSION@

Please refer to the [detailed release notes][docs-detailed-release-notes] and
full product documentation for [Workbooks][docs-workbooks] and
[Inspector][docs-inspector] for complete information.

## NEW & IMPROVED

* It is now possible to use ASP.NET Core in your .NET Core workbooks.

* Signature help now behaves more like Visual Studio.

* The visual tree inspector is now more consistent between Windows and macOS
  and has improved view selection and navigation features.

* More improvements to the new property editor including custom editors for
  various `CoreGraphics` and `System.Windows.Media` types.

* Choose the new _Report an Issue_ menu item in the _Help_ menu to easily
  report issues.

* Xamarin.Forms support has been bumped to 2.5.0.

* The New Workbook dialog now defaults to the last selected type.

## FIXED

* Fixed a rendering issue in the Mac sidebar. Thank you to Yusuke Yamada
  (@yamachu) for our
  [first ever public contribution](https://github.com/Microsoft/workbooks/pull/97)!

* Fixed an SDK location bug that prevented Android workbooks from running.

* Add `workbook` tool to `PATH` using `/etc/paths.d` instead of writing a
  script to `/usr/local/bin`.

* Fix generic type name rendering.

* Inspector view picking once again works with devices other than iPhone 5s.

* Fix rendering of emoji in C# string literals.

* Fix user interface rendering issues after unlocking your computer on Windows.

* Fix custom attributes using types defined in the workbook, enabling
  custom JSON deserialization.

# Version 1.4.0 Beta 1

## NEW & IMPROVED

* Open-sourced under the MIT license. [Join us on GitHub!][github]

* Support for iOS 11 and Xcode 9.

* Camera controls on the 3D view inspector have been enhanced and now share
  functionality across macOS and Windows with support for Pan, Zoom and Rotate.

* The property panel in the view inspector is now based on
  [Xamarin.PropertyEditing][proppy], which provides a number of improvements:
  - Properties can now be edited on macOS.
  - Performance improvements thanks to loading properties asynchronously.
  - Editing support for enum, size, and rectangle properties.

* Line-wrapping may be turned off for code cells via the preferences dialog.

* It is now possible for integrations to asynchronously post results to
  code cells. This is the groundwork for supporting `IObservable` and allows
  for [deeper integration with cell compilations][cell-compilations].

* Added "Copy Version Information" and "Reveal Log File" Help menu items to
  make reporting issues easier.

## FIXED

* Additional accessibility fixes for High Contrast mode users, particularly
  for buttons and menus in the High Contrast White theme.

* The plain text formatter for strings now preserves whitespace in formatted
  output.

* Workbooks are now marked as dirty when cells are deleted, preventing possible
  stale workbook files on disk.

* Fixed a few minor issues with NuGet package restoration.

# 1.3 Series Changes

* [See the full release notes for the 1.3 series][13-series].

[github]: https://github.com/Microsoft/workbooks
[proppy]: https://github.com/xamarin/Xamarin.PropertyEditing
[cell-compilations]: https://github.com/Microsoft/workbooks/blob/master/Samples/CompilationIntegration/AgentIntegration.cs

[docs-workbooks]: https://developer.xamarin.com/guides/cross-platform/workbooks/
[docs-inspector]: https://developer.xamarin.com/guides/cross-platform/inspector/
[docs-detailed-release-notes]: https://developer.xamarin.com/releases/interactive/interactive-1.4/
[13-series]: https://developer.xamarin.com/releases/interactive/interactive-1.3