#include "MBox.h"

void DisplayError(const wchar_t* message)
{
	std::wstring s(L"An error was caught.\n");
	s += std::wstring(message);

	MessageBeep(0x00000010L);
	MessageBox(GetConsoleWindow(), s.c_str(), L"Oops!", MB_OK | MB_ICONERROR);
}