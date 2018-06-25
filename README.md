# Xamarin Workbooks

[![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/Microsoft/workbooks)

Xamarin Workbooks provide a blend of documentation and code that is perfect
for experimentation, learning, and creating guides and teaching aids.

Create a rich C# workbook for .NET Core, Android, iOS, Mac, or WPF, and get
instant live results as you learn these APIs. Workbooks also have access to
the vast NuGet package ecosystem to make learning new APIs a breeze.

## Resources

### Download

* [Download Latest Public Release for Mac](https://dl.xamarin.com/interactive/XamarinInteractive.pkg)
* [Download Latest Public Release for Windows](https://dl.xamarin.com/interactive/XamarinInteractive.msi)

### Documentation

* [Workbooks Documentation](https://developer.xamarin.com/guides/cross-platform/workbooks/)
* [Live Inspection Documentation](https://developer.xamarin.com/guides/cross-platform/inspector/)
* [Sample Workbooks](https://github.com/xamarin/Workbooks)

### Provide Feedback

* [Discuss in Xamarin Forums](https://forums.xamarin.com/categories/inspector)
* [File Bugs](https://bugzilla.xamarin.com/enter_bug.cgi?product=Workbooks%20%26%20Inspector)

## Continuous Integration Status

| Service  | macOS          | Windows            | Linux              |
| -------- | -------------- | ------------------ | ------------------ |
| VSTS     | ![][vstsmacbs] | ![][vstswinbs]     |                    |
| AppVeyor |                | ![][appveyorwinbs] |                    |
| Travis   |                |                    | ![][travislinuxbs] |

[vstsmacbs]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/6539/badge "VSTS: macOS Build Status"
[vstswinbs]: https://devdiv.visualstudio.com/_apis/public/build/definitions/0bdbc590-a062-4c3f-b0f6-9383f67865ee/6563/badge "VSTS: Windows Build Status"
[appveyorwinbs]: https://ci.appveyor.com/api/projects/status/9v2ljdvcyjocpfvo/branch/master?svg=true "AppVeyor: Windows Build Status"
[travislinuxbs]: https://travis-ci.org/Microsoft/workbooks.svg?branch=master "Travis: Linux Build Status"

## Build & Run

Ensure git submodules are up-do-date:

```bash
git submodule sync
git submodule update --recursive --init
```

Now simply run:

```bash
msbuild /restore
```

Or for the strict .NET Core subset:

```bash
dotnet build
```

### Configuring the Build

The top-level build system can be driven either by the system `msbuild` or by
the installed .NET Core SDK `dotnet build`. When using `dotnet build`, only
projects that can run on .NET Core will be built.

Additionally, the build can be shaped via profiles. Any number of profiles
may be selected. By default, _all_ profiles will be selected.

#### Profiles

Profiles are specified via the MSBuild `Profile` property and may be
combined with a `+` delimiter:

```bash
msbuild /restore /p:Profile=Console+Web
```

| Name      | Description                               | Minimum Dependencies                                                             |
| :-------- | :---------------------------------------- | :------------------------------------------------------------------------------- |
| `Web`     | Build the ASP.NET Core Workbooks server   | [.NET Core ≥ 2.1][dep_dnc], [Node.js ≥ 8.10][dep_node], [Yarn ≥ 1.5.1][dep_yarn] |
| `Console` | Build the Console client                  | [.NET Core ≥ 2.1][dep_dnc]                                                       |
| `Desktop` | Build the macOS or Windows desktop client | [Visual Studio 2017 ≥ 15.6][dep_vs]                                              |

_Note: Support for Xamarin platforms will be detected automatically and built
if available. On macOS, the "macOS" platform (Xamarin.Mac) must be installed
to build the client. Xamarin/mobile is entirely optional on Windows._

[dep_dnc]: https://www.microsoft.com/net/learn/get-started
[dep_node]: https://nodejs.org/
[dep_yarn]: https://yarnpkg.com/en/docs/install
[dep_mono]: http://www.mono-project.com/download/stable/
[dep_vs]: https://www.visualstudio.com/vs/

#### Properties

Many properties that can be specified on the command line will be persisted
for subsequent runs. For example:

```bash
# "Configure" and perform initial build:
msbuild /restore \
  /p:Profile=Web \
  /p:Configuration=Release \
  /p:WithoutXamarin=true

# Rebuild with the same configuration as above, implied
# thanks to _build/Configuration.props:
msbuild
```

The following properties will persist and do not need to be specified on
the command line on subsequent runs:

| Name                 | Description                  | Default Value |
| :------------------- | :-------------------------------------------------------------------- | :-------------------- |
| `Profile`            | The set of profiles to build                                          | `Web+Console+Desktop` |
| `Configuration`      | The build configuration (`Debug` or `Release`)                        | `Debug`               |
| **Environnment:**                                                                                                    |
| `WithoutXamarin`     | A shortcut for setting all `HaveXamarin*` properties below to `false` | _unset_               |
| `HaveXamarinMac`     | Whether or Xamarin.Mac is available to the build                      | _auto-detected_       |
| `HaveXamarinIos`     | Whether or Xamarin.iOS is available to the build                      | _auto-detected_       |
| `HaveXamarinAndroid` | Whether or Xamarin.Android is available to the build                  | _auto-detected_       |
| **External Tools:**                                                                                                  |
| `NuGet`              | Path to `nuget.exe`                                                   | _resolved via `PATH`_ |
| `Node`               | Path to `node`                                                        | _resolved via `PATH`_ |
| `Yarn`               | Path to `yarn`                                                        | _resolved via `PATH`_ |
| `Npm`                | Path to `npm`                                                         | _resolved via `PATH`_ |

#### Windows Nuances

If you want to build a `Release` build on Windows (for example, you want to
build an installer), you will need to build in a slightly different fashion.
First, make sure that you connect to a Mac build host via Visual Studio at
least once. You can do this by doing the following:

* Open Visual Studio
* Go to _Tools → Options → Xamarin → iOS Settings_
* Click "Find Xamarin Mac Agent"
* Select a Mac on your network, or add one by name
* Enter credentials when prompted

Once the connection completes, click OK to close all the dialogs. Then,
build the `Release` configuration by running the following:

```bash
msbuild \
  /p:MacBuildHostAddress="<hostname-or-ip-of-your-mac>" \
  /p:MacBuildHostUser="<mac-username>" \
  /p:Configuration=Release /t:Build,Install
```

This is needed because the installer build now needs a zipped copy of the
Xamarin.iOS workbook app from the server. The `Xamarin.Workbooks.iOS` project
will do the build and copy automatically when a Mac build host is used. If you
are building in Debug, you can omit those properties unless you need the
Workbook app to be copied locally, in which case, include them there as well.

**Note:** the build will read properties from `Build/Local.props` as well,
for example:

```xml
<Project>
  <PropertyGroup>
    <MacBuildHostAddress>porkbelly</MacBuildHostAddress>
    <MacBuildHostUser>aaron</MacBuildHostUser>
  </PropertyGroup>
</Project>
```

## Contributing

This project welcomes contributions and suggestions. Most contributions require
you to agree to a Contributor License Agreement (CLA) declaring that you have
the right to, and actually do, grant us the rights to use your contribution.
For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether
you need to provide a CLA and decorate the PR appropriately (e.g., label,
comment). Simply follow the instructions provided by the bot. You will only
need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/)
or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any
additional questions or comments.

## Notices

### Telemetry

Official builds and releases of Xamarin Workbooks & Inspector from Microsoft
collect usage data and send it to Microsoft to help improve our products
and services. [Read our privacy statement to learn more](https://go.microsoft.com/fwlink/?LinkID=824704).

Users may opt out of telemetry and usage data collection from the _Preferences_
dialog.

_Non-Microsoft builds do not enable telemetry collection at all._

### Third Party Code

Xamarin Workbooks & Inspector incorporates open source code from external
projects. See [ThirdPartyNotices.txt](ThirdPartyNotices.txt) for attribution.

[our-nuget]: https://www.nuget.org/packages/Xamarin.Workbooks.Integration