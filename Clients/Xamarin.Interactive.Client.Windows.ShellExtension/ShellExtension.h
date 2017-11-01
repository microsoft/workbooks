//
// ShellExtension.h
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

#include "stdafx.h"

class ShellExtension : public IShellExtInit {
public:
	// IUnknown
	IFACEMETHODIMP QueryInterface (REFIID riid, void **ppv);
	IFACEMETHODIMP_(ULONG) AddRef ();
	IFACEMETHODIMP_(ULONG) Release ();

	// IShellExtInit
	IFACEMETHODIMP Initialize (
		LPCITEMIDLIST pidlFolder,
		LPDATAOBJECT pDataObj,
		HKEY hKeyProgID);

	ShellExtension ();

protected:
	~ShellExtension ();

private:
	long refCount;
};