#!/bin/bash -e

selfdir="$(cd "$(dirname "$0")" && pwd)/"
rootdir="$(cd "${selfdir}../../" && pwd)/"
workbookappsdir="${rootdir}WorkbookApps/"

RID=osx-x64
CONFIGURATION=Release
WORKBOOK_APPS="
	${rootdir}Agents/Xamarin.Interactive.Console/Xamarin.Interactive.Console.csproj
	${workbookappsdir}Xamarin.Workbooks.DotNetCore/Xamarin.Workbooks.DotNetCore.csproj
	${workbookappsdir}Xamarin.Workbooks.Android/Xamarin.Workbooks.Android.csproj
	${workbookappsdir}Xamarin.Workbooks.iOS/Xamarin.Workbooks.iOS.csproj
	${workbookappsdir}Xamarin.Workbooks.Mac/Xamarin.Workbooks.Mac.csproj
"

for project in $WORKBOOK_APPS; do
	msbuild "/p:Configuration=${CONFIGURATION}" "$project"
done

outdir="${selfdir}bin/workbooks-server-${RID}"

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

echo Done.
