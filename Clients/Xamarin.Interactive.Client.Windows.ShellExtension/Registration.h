//
// Registration.h
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Microsoft. All rights reserved.

#pragma once

#include "stdafx.h"

HRESULT RegisterInprocServer (
	PCWSTR modulePath,
	const CLSID& clsid,
	PCWSTR friendlyName,
	PCWSTR threadModel);

HRESULT UnregisterInprocServer (const CLSID& clsid);