//
// ShellExtension.cpp
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

#include "stdafx.h"

extern long globalDllRefCount;

ShellExtension::ShellExtension ()
	: refCount (1)
{
	InterlockedIncrement (&globalDllRefCount);
}

ShellExtension::~ShellExtension ()
{
	InterlockedDecrement (&globalDllRefCount);
}

#pragma region IUnknown

IFACEMETHODIMP ShellExtension::QueryInterface (REFIID riid, void **ppv)
{
	#pragma warning(push)
	#pragma warning(disable: 4838)
	static const QITAB table[] = {
		QITABENT (ShellExtension, IShellExtInit),
		{ nullptr, 0 },
	};

	return QISearch (this, table, riid, ppv);
	#pragma warning(pop)
}

IFACEMETHODIMP_(ULONG) ShellExtension::AddRef ()
{
	return InterlockedIncrement (&refCount);
}

IFACEMETHODIMP_(ULONG) ShellExtension::Release ()
{
	ULONG ref = InterlockedDecrement (&refCount);
	if (ref == 0)
		delete this;
	return ref;
}

#pragma endregion

#pragma region IShellExtInit

IFACEMETHODIMP ShellExtension::Initialize (
	LPCITEMIDLIST pidlFolder,
	LPDATAOBJECT pDataObj,
	HKEY hKeyProgID)
{
	return S_OK;
}

#pragma endregion