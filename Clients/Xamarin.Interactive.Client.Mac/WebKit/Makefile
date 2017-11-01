XMFX = /Library/Frameworks/Xamarin.Mac.framework/Versions/Current
BMAC = $(XMFX)/bin/bmac

.PHONY: bind
bind:
	$(BMAC) \
		-sdk xamarin.mac \
		-target-framework Xamarin.Mac,Version=v4.5,Profile=Full \
		-baselib $(XMFX)/lib/reference/full/Xamarin.Mac.dll \
		-sourceonly sources.rsp \
		-api WebKitApi.cs \
		-outdir . \
		-ns WebKit

.PHONY: clean
clean:
	rm -rf ObjCRuntime WebKit sources.rsp
