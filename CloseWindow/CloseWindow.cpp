//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <string.h>
#include "resource.h"

//**********************************************************

HINSTANCE g_hInstance;
char* g_pCmdLine;

INT_PTR CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
void Close(HWND hDlg);

//**********************************************************

int CALLBACK WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
	LPSTR lpCmdLine, int nCmdShow)
{
	if (lpCmdLine == NULL || !*lpCmdLine)
	{
		return 1;
	}


	g_hInstance = hInstance;
	char* p;
	g_pCmdLine = lpCmdLine;

	// Make cmd line lower case
	for (p = g_pCmdLine; *p; p++)
		*p = tolower(*p);

	// Trim trailing white spaces
	for (p = g_pCmdLine + strlen(g_pCmdLine); p > g_pCmdLine && (*(p - 1) == ' ' || *(p - 1) == '\t' || *(p - 1) == '\r' || *(p - 1) == '\n'); p--)
		;
	*p = 0;

	DialogBox(hInstance, MAKEINTRESOURCE(IDD_DIALOG), NULL, DialogProc);

	return 0;
}

//**********************************************************

INT_PTR CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	switch (uMsg)
	{
	case WM_INITDIALOG:
		SetClassLongPtr(hDlg, GCLP_HICON, (LONG_PTR)LoadIcon(g_hInstance, MAKEINTRESOURCE(IDI_ICON)));

		if (!strcmp(g_pCmdLine, "-enum"))
		{
			ShowWindow(GetDlgItem(hDlg, IDC_TEXT), 0);
		}
		else
		{
			ShowWindow(GetDlgItem(hDlg, IDC_EDIT), 0);
			char szText[1000];
			sprintf(szText, ">>>%s<<<", g_pCmdLine);
			SetDlgItemText(hDlg, IDC_TEXT, szText);
		}

		SetTimer(hDlg, 1, 100, NULL);

		return FALSE;

	case WM_TIMER:
		Close(hDlg);
		return TRUE;

	case WM_SIZE:
		MoveWindow(GetDlgItem(hDlg, IDC_TEXT), 10, 10, LOWORD(lParam) - 20, HIWORD(lParam) - 20, TRUE);
		MoveWindow(GetDlgItem(hDlg, IDC_EDIT), 10, 10, LOWORD(lParam) - 20, HIWORD(lParam) - 20, TRUE);
		return TRUE;

	case WM_COMMAND:
		switch (LOWORD(wParam))
		{
		case IDCANCEL:
			KillTimer(hDlg, 1);
			EndDialog(hDlg, wParam);
			return TRUE;
		}
	}

	return FALSE;
}

//**********************************************************

char szText[100000];
char* p;

BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam)
{
	char szTitle[1000];

	if (GetWindowText(hwnd, szTitle, 1000))
	{
		if (!strcmp(g_pCmdLine, "-enum"))
		{
			p += sprintf(p, "%s\r\n", szTitle);
		}
		else
		{
			for (char* p = szTitle; *p; p++)
				*p = tolower(*p);

			if (strstr(szTitle, g_pCmdLine))
			{
				PostMessage(hwnd, WM_CLOSE, 0, 0);
			}
		}
	}

	return true;
}

//**********************************************************

char szTextOld[100000];

void Close(HWND hDlg)
{
	if (!strcmp(g_pCmdLine, "-enum"))
	{
		szText[0] = 0;
		p = szText;
	}

	EnumWindows(EnumWindowsProc, (LPARAM)hDlg);

	if (!strcmp(g_pCmdLine, "-enum"))
	{
		GetDlgItemText(hDlg, IDC_EDIT, szTextOld, 100000);
		if (strcmp(szTextOld, szText))
		{
			SetDlgItemText(hDlg, IDC_EDIT, szText);
		}
	}

	return;
}

//**********************************************************
