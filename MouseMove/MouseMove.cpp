//**********************************************************
//
// MouseMove 1.1
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
int        g_widthcount;

//**********************************************************

BOOL CALLBACK DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
void UpdateCount(void);
void UpdateTimer(HWND hwnd);

//**********************************************************

int PASCAL WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
	LPSTR lpszCmdLine, int nCmdShow)
{
	g_hInstance = hInstance;
	pStart = pEnd = NULL;
	g_widthcount = 0;

	DialogBox(hInstance, MAKEINTRESOURCE(IDD_DIALOG), NULL, (DLGPROC)DlgProc);

	return 0;
}

//**********************************************************

BOOL CALLBACK DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	switch (uMsg)
	{
	case WM_INITDIALOG:
		// Set Class icon
		g_OldIcon = SetClassLong(hDlg, GCL_HICON, (LONG)LoadIcon(g_hInstance, MAKEINTRESOURCE(IDI_ICON)));

		SetTimer(hDlg, 1, 100, NULL);
		return TRUE;  // return FALSE if modeless and SetFocus is called

	case WM_TIMER:
		UpdateTimer(hDlg);
		return TRUE;

	case WM_MOUSEMOVE:
		UpdateCount();
		return TRUE;

	case WM_COMMAND:
		switch (LOWORD(wParam))
		{
		case IDCANCEL:
			// Quit program...

			KillTimer(hDlg, 1);

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

void UpdateCount(void)
{
	auto time = GetTickCount64();

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
		pStart = pEnd = pNode;
	}
}

//**********************************************************

void UpdateTimer(HWND hwnd)
{
	auto time = GetTickCount64();

	int count = 0;
	for (movenode *pNode = pStart; pNode;)
	{
		if (pNode->time < time - 1000)
		{
			pStart = pNode->pNext;
			delete pNode;
			pNode = pStart;
		}
		else
		{
			count++;
			pNode = pNode->pNext;
		}
	}


	char counttext[100];
	sprintf_s(counttext, 100, "%d", count);
	SetWindowText(hwnd, counttext);


	RECT rc;
	GetClientRect(hwnd, &rc);

	if (g_widthcount >= rc.right)
	{
		g_widthcount = 0;
		InvalidateRect(hwnd, NULL, TRUE);
		UpdateWindow(hwnd);
	}

	HDC hdc;
	if (hdc = GetDC(hwnd))
	{
		MoveToEx(hdc, g_widthcount, rc.bottom, NULL);
		LineTo(hdc, g_widthcount, rc.bottom - count);
		ReleaseDC(hwnd, hdc);
	}

	g_widthcount++;
}

//**********************************************************
