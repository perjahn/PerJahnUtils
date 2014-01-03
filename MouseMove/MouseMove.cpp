//**********************************************************
//
// MouseMove 1.0
//
// Written by Per Jahn
//
// Necessary static link-libraries:
// -
//
//**********************************************************

#include <windows.h>        // Windoze
//#include <commctrl.h>       // Common controls
#include <string.h>         // Text string
//#include <fstream.h>        // File stream
#include <stdio.h>          // Files
//#include <stdlib.h>         // Standard libraries
//#include <commdlg.h>        // Common dialog boxes
//#include <shlobj.h>         // Shell
//#include <ole2.h>           // OLE clipboard (files)
//#include <gdiplus.h>        // Graphics
//#include <htmlhelp.h>       // HTML help
//#include <shlwapi.h>        // Shell light weight ulility api
//#include <time.h>           // Time
#include "resource.h"       // Resources

//**********************************************************

struct movenode
{
	unsigned long long time;
	movenode *pNext;
} *pStart, *pEnd;

//**********************************************************

HINSTANCE  g_hInstance;
DWORD      g_OldIcon;

//**********************************************************

BOOL CALLBACK DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
void UpdateCount(HWND hwnd);

//**********************************************************

int PASCAL WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
	LPSTR lpszCmdLine, int nCmdShow)
{
	g_hInstance = hInstance;

	pStart = pEnd = NULL;

	DialogBox(hInstance, MAKEINTRESOURCE(IDD_DIALOG), NULL, (DLGPROC)DlgProc);

	return 0;
}

//**********************************************************

BOOL CALLBACK DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	switch(uMsg)
	{
	case WM_INITDIALOG:
		// Set Class icon
		g_OldIcon = SetClassLong(hDlg, GCL_HICON, (LONG)LoadIcon(g_hInstance, MAKEINTRESOURCE(IDI_ICON)));
		return TRUE;  // return FALSE if modeless and SetFocus is called

	case WM_MOUSEMOVE:
		{
			int x = LOWORD(lParam);
			int y = HIWORD(lParam);
			HDC hdc;
			if (hdc = GetDC(hDlg))
			{
				SetPixel(hdc, x, y, RGB(255, 0, 0));
				ReleaseDC(hDlg, hdc);
			}

			UpdateCount(hDlg);
		}
		return TRUE;

	case WM_COMMAND:
		switch(LOWORD(wParam))
		{
		case IDCANCEL:
			// Quit program...

			// Remove Class icon
			SetClassLong(hDlg, GCL_HICON, g_OldIcon);

			EndDialog(hDlg, 0);  // Close dialogwindow
			return TRUE;
		}
		break;
	}

	return FALSE;
}

//**********************************************************

void UpdateCount(HWND hwnd)
{
	long long unsigned time = GetTickCount64();

	movenode *pNode = new movenode;
	pNode->time = time;
	pNode->pNext = NULL;

	if (pStart)
	{
		pEnd->pNext = pNode;
		pEnd = pNode;
	}
	else
	{
		pStart = pNode;
	}

	SetWindowText(hwnd, "qwe");
}

//**********************************************************
