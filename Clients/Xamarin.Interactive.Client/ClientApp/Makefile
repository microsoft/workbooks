V=m

.PHONY: all
all:
	xbuild /nologo /v:$(V) WebApp.proj

test: all
	node ../../obj/app.js
