#pragma once

#include "targetver.h"

#ifndef WIN32_LEAN_AND_MEAN
#define WIN32_LEAN_AND_MEAN
#endif
#include <Windows.h>

#include <new>
#include <guiddef.h>
#include <Unknwn.h>
#include <ShlObj.h>
#include <strsafe.h>

#include <Shlwapi.h> 
#pragma comment(lib, "shlwapi.lib") 

#include "ClassFactory.h"
#include "ShellExtension.h"
#include "Registration.h"

#define FAILBAIL(expr) do { HRESULT result = (expr); if (!SUCCEEDED (result)) return result; } while (0)