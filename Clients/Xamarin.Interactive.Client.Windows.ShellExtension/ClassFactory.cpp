//
// ClassFactory.cpp
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

#include "stdafx.h"

extern long globalDllRefCount;

ClassFactory::ClassFactory ()
	: refCount (1)
{
	InterlockedIncrement (&globalDllRefCount);
}

ClassFactory::~ClassFactory ()
{
	InterlockedDecrement (&globalDllRefCount);
}

#pragma region IUnknown

IFACEMETHODIMP ClassFactory::QueryInterface (REFIID riid, void **ppv)
{
	#pragma warning(push)
	#pragma warning(disable: 4838)
	static const QITAB table[] = {
		QITABENT (ClassFactory, IClassFactory),
		{ nullptr, 0 },
	};

	return QISearch (this, table, riid, ppv);
	#pragma warning(pop)
}

IFACEMETHODIMP_(ULONG) ClassFactory::AddRef ()
{
	return InterlockedIncrement (&refCount);
}

IFACEMETHODIMP_(ULONG) ClassFactory::Release ()
{
	ULONG ref = InterlockedDecrement (&refCount);
	if (ref == 0)
		delete this;
	return ref;
}

#pragma endregion

#pragma region IClassFactory

IFACEMETHODIMP ClassFactory::CreateInstance (IUnknown *pUnkOuter, REFIID riid, void **ppv)
{
	if (pUnkOuter != nullptr)
		return CLASS_E_NOAGGREGATION;

	auto *extension = new (std::nothrow) ShellExtension ();
	if (extension == nullptr)
		return E_OUTOFMEMORY;

	HRESULT result = extension->QueryInterface (riid, ppv);
	extension->Release ();
	return result;
}

IFACEMETHODIMP ClassFactory::LockServer (BOOL fLock)
{
	if (fLock)
		InterlockedIncrement (&globalDllRefCount);
	else
		InterlockedDecrement (&globalDllRefCount);
	return S_OK;
}

#pragma endregion