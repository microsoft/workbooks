//
// ClassFactory.h
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

#include "stdafx.h"

class ClassFactory : public IClassFactory
{
public:
	// IUnknown 
	IFACEMETHODIMP QueryInterface (REFIID riid, void **ppv);
	IFACEMETHODIMP_ (ULONG) AddRef ();
	IFACEMETHODIMP_ (ULONG) Release ();

	// IClassFactory 
	IFACEMETHODIMP CreateInstance (IUnknown *pUnkOuter, REFIID riid, void **ppv);
	IFACEMETHODIMP LockServer (BOOL fLock);

	ClassFactory ();

protected:
	~ClassFactory ();

private:
	long refCount;
};