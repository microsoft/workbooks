CONFIGURATION = Release
ASSEMBLY = bin/$(CONFIGURATION)/Xamarin.Interactive.dll
ASSEMBLY_SEARCH_PATH = /Library/Frameworks/Mono.framework/Versions/Current/lib/mono/4.5/Facades
MDOC_UPDATE = mdoc update $(ASSEMBLY) -o API/en -L $(ASSEMBLY_SEARCH_PATH)

.PHONY: all
all: build

.PHONY: build
build:
	msbuild Xamarin.Interactive.csproj /t:Build /p:Configuration=$(CONFIGURATION)

update-docs: build
	$(MDOC_UPDATE)
