//**********************************************************
//
// MouseMove 1.2
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
	int x, y;
	unsigned long long time;
	movenode *next;
} *start, *end;

//**********************************************************

HINSTANCE  g_hInstance;
DWORD      g_OldIcon;
int        g_widthcount;

//**********************************************************

BOOL CALLBACK DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
void UpdateCount(int x, int y);
void UpdateTimer(HWND hwnd);

//**********************************************************

int PASCAL WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
	LPSTR lpszCmdLine, int nCmdShow)
{
	g_hInstance = hInstance;
	start = end = NULL;
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
		UpdateCount(LOWORD(lParam), HIWORD(lParam));
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

void UpdateCount(int x, int y)
{
	auto time = GetTickCount64();

	movenode *node = new movenode;
	node->x = x;
	node->y = y;
	node->time = time;
	node->next = NULL;

	if (start)
	{
		end->next = node;
		end = node;
	}
	else
	{
		start = end = node;
	}
}

//**********************************************************

void UpdateTimer(HWND hwnd)
{
	auto time = GetTickCount64();

	int count1 = 0, count2 = 0, x = 0, y = 0;
	for (movenode *node = start; node;)
	{
		if (node->time < time - 1000)
		{
			start = node->next;
			delete node;
			node = start;
		}
		else
		{
			count1++;
			if (node->x != x && node->y != y)
			{
				count2++;
			}
			x = node->x;
			y = node->y;
			node = node->next;
		}
	}


	char counttext[100];
	sprintf_s(counttext, 100, "%d (Move coords: %d)", count1, count2);
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
		LineTo(hdc, g_widthcount, rc.bottom - count1);
		ReleaseDC(hwnd, hdc);
	}

	g_widthcount++;
}

//**********************************************************
