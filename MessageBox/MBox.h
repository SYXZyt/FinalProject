#pragma once
#include <string>
#include <Windows.h>

extern "C"
{
	__declspec(dllexport) void DisplayError(const wchar_t* message);
}