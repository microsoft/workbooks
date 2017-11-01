//
// EntryPoints.cpp
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

#include "stdafx.h"

// {0000004E-B15F-4277-86EA-E5B808FC0C45}
static const CLSID CLSID_XamarinWorkbooksShellExtension =
	{ 0x0000004e, 0xb15f, 0x4277, { 0x86, 0xea, 0xe5, 0xb8, 0x08, 0xfc, 0x0c, 0x45 } };

HINSTANCE globalDllModule;
long globalDllRefCount;

extern "C" BOOL APIENTRY DllMain (
	HMODULE hModule,
	DWORD  dwReason,
	LPVOID lpReserved)
{
	switch (dwReason) {
	case DLL_PROCESS_ATTACH:
		globalDllModule = hModule;
		DisableThreadLibraryCalls (hModule);
		MessageBox (nullptr, L"DllMain: DLL_PROCESS_ATTACH", L"Xamarin Workbooks", MB_OK);
		break;
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		MessageBox (nullptr, L"DllMain: DLL_PROCESS_DETATCH", L"Xamarin Workbooks", MB_OK);
		break;
	}
	return TRUE;
}

STDAPI DllGetClassObject (REFCLSID rclsid, REFIID riid, void **ppv)
{
	MessageBox (nullptr, L"DllGetClassObject", L"Xamarin Workbooks", MB_OK);

	if (!IsEqualCLSID (rclsid, CLSID_XamarinWorkbooksShellExtension))
		return CLASS_E_CLASSNOTAVAILABLE;

	auto *classFactory = new (std::nothrow) ClassFactory ();
	if (classFactory == nullptr)
		return E_OUTOFMEMORY;

	HRESULT result = classFactory->QueryInterface (riid, ppv);
	classFactory->Release ();
	return result;
}

STDAPI DllCanUnloadNow ()
{
	MessageBox (nullptr, L"DllCanUnloadNow", L"Xamarin Workbooks", MB_OK);

	return globalDllRefCount > 0 ? S_FALSE : S_OK;
}

STDAPI DllRegisterServer ()
{
	MessageBox (nullptr, L"DllRegisterServer", L"Xamarin Workbooks", MB_OK);

	wchar_t modulePath[MAX_PATH];
	if (GetModuleFileName (globalDllModule, modulePath, ARRAYSIZE (modulePath)) == 0)
		return HRESULT_FROM_WIN32 (GetLastError ());

	return RegisterInprocServer (
		modulePath,
		CLSID_XamarinWorkbooksShellExtension,
		L"Xamarin Workbooks",
		L"Apartment");
}

STDAPI DllUnregisterServer ()
{
	MessageBox (nullptr, L"DllUnregisterServer", L"Xamarin Workbooks", MB_OK);

	return UnregisterInprocServer (CLSID_XamarinWorkbooksShellExtension);
}