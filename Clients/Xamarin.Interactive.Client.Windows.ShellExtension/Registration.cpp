//
// Registration.cpp
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

#include "stdafx.h"

typedef struct {
	PCWSTR SubKey;
	PCWSTR ValueName;
	PCWSTR ValueData;
} RegistryEntry;

HRESULT RegisterInprocServer (
	PCWSTR modulePath,
	const CLSID& clsid,
	PCWSTR friendlyName,
	PCWSTR threadingModel)
{
	if (modulePath == nullptr || friendlyName == nullptr || threadingModel == nullptr)
		return E_INVALIDARG;

	HKEY hkey;
	wchar_t clsidString[MAX_PATH];
	wchar_t subkey[MAX_PATH];

	StringFromGUID2 (clsid, clsidString, ARRAYSIZE (clsidString));

	RegistryEntry entries[] = {
		{ L"CLSID\\%s", nullptr, friendlyName },
		{ L"CLSID\\%s\\InprocServer32", nullptr, modulePath },
		{ L"CLSID\\%s\\InprocServer32", L"ThreadingModel", threadingModel },
		{ nullptr, nullptr, nullptr }
	};

	for (int i = 0; entries[i].SubKey != nullptr; i++) {
		StringCchPrintf (subkey, ARRAYSIZE (subkey), entries[i].SubKey, clsidString);

		FAILBAIL (HRESULT_FROM_WIN32 (RegCreateKeyEx (
			HKEY_CLASSES_ROOT,
			subkey,
			0,
			nullptr,
			REG_OPTION_NON_VOLATILE,
			KEY_WRITE,
			nullptr,
			&hkey,
			nullptr)));

		if (entries[i].ValueData != nullptr) {
			FAILBAIL (HRESULT_FROM_WIN32 (RegSetValueEx (
				hkey,
				entries[i].ValueName,
				0,
				REG_SZ,
				reinterpret_cast<const BYTE *> (entries[i].ValueData),
				lstrlen (entries[i].ValueData) * sizeof (*entries[i].ValueData))));
			FAILBAIL (HRESULT_FROM_WIN32 (RegCloseKey (hkey)));
		}
	}

	FAILBAIL (HRESULT_FROM_WIN32 (RegCreateKeyEx (
		HKEY_LOCAL_MACHINE,
		L"Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved",
		0,
		nullptr,
		REG_OPTION_NON_VOLATILE,
		KEY_WRITE,
		nullptr,
		&hkey,
		nullptr)));

	FAILBAIL (HRESULT_FROM_WIN32 (RegSetValueEx (
		hkey, 
		clsidString,
		0,
		REG_SZ,
		reinterpret_cast<const BYTE *> (friendlyName),
		lstrlen (friendlyName) * sizeof (*friendlyName))));

	FAILBAIL (HRESULT_FROM_WIN32 (RegCloseKey (hkey)));

	return S_OK;
}

HRESULT UnregisterInprocServer (const CLSID& clsid)
{
	LSTATUS result;
	wchar_t clsidString[MAX_PATH];
	wchar_t subkey[MAX_PATH];

	StringFromGUID2 (clsid, clsidString, ARRAYSIZE (clsidString));

	FAILBAIL (StringCchPrintf (subkey, ARRAYSIZE (subkey), L"CLSID\\%s", clsidString));
	result = RegDeleteTree (HKEY_CLASSES_ROOT, subkey);
	if (result != ERROR_SUCCESS && result != ERROR_FILE_NOT_FOUND)
		return HRESULT_FROM_WIN32 (result);

	result = RegDeleteKeyValue (
		HKEY_LOCAL_MACHINE,
		L"Software\\Microsoft\\Windows\\CurrentVersion\\Shell Extensions\\Approved",
		clsidString);
	if (result != ERROR_SUCCESS && result != ERROR_FILE_NOT_FOUND)
		return HRESULT_FROM_WIN32 (result);

	return S_OK;
}