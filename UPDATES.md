# Version @BUILD_DOCUMENTATION_VERSION@

A summary of the 1.4 release series is below. Please refer to the
[detailed release notes][docs-detailed-release-notes] and full
product documentation for [Workbooks][docs-workbooks] and
[Inspector][docs-inspector] for complete information.

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