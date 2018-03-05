#!/bin/bash -e

selfdir="$(cd "$(dirname "$0")" && pwd)/"
rootdir="$(cd "${selfdir}../../" && pwd)/"
workbookappsdir="${rootdir}WorkbookApps/"

function showHelp {
	echo usage: $0 [OPTIONS]
	echo
	echo "-skip-workbook-apps-build     do not build workbook apps"
	echo "                              (assume they're already built)"
	echo
}

skipWorkbookAppsBuild=

for arg in "$@"; do
	case "$arg" in
	-skip-workbook-apps-build)
		skipWorkbookAppsBuild=1
		;;
	-help|-h|--help)
		showHelp
		exit 1
		;;
	*)
		echo invalid option: $arg
		echo
		showHelp
		exit 1
		;;
	esac
done

RID=osx-x64
CONFIGURATION=Release
WORKBOOK_APPS="
	${rootdir}Agents/Xamarin.Interactive.Console/Xamarin.Interactive.Console.csproj
	${workbookappsdir}Xamarin.Workbooks.DotNetCore/Xamarin.Workbooks.DotNetCore.csproj
	${workbookappsdir}Xamarin.Workbooks.Android/Xamarin.Workbooks.Android.csproj
	${workbookappsdir}Xamarin.Workbooks.iOS/Xamarin.Workbooks.iOS.csproj
	${workbookappsdir}Xamarin.Workbooks.Mac/Xamarin.Workbooks.Mac.csproj
"

if [ -z "$skipWorkbookAppsBuild" ]; then
	for project in $WORKBOOK_APPS; do
		msbuild "/p:Configuration=${CONFIGURATION}" "$project"
	done
fi

rev="$(git rev-parse --short HEAD)"
bundlename="workbooks-server-${RID}-${rev}"
bindir="${selfdir}bin/"
outdir="${bindir}${bundlename}"

if [ -d "${outdir}" ]; then
	rm -r "${outdir}"
fi

dotnet publish \
	-o "$outdir" \
	-r "$RID" \
	-c "$CONFIGURATION"

echo Copying workbook apps into bundle...

cp -a \
	"${rootdir}_build/${CONFIGURATION}/WorkbookApps" \
	"$outdir"

echo Zipping...

(
	cd "$bindir"
	zip -r "${bundlename}.zip" "$bundlename"
)

echo Done
