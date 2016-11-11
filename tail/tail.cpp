//**********************************************************
//
// tail 0.2
//
// Shows text log files + auto update.
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
#include <malloc.h>          // Files
//#include <stdlib.h>         // Standard libraries
//#include <commdlg.h>        // Common dialog boxes
//#include <shlobj.h>         // Shell
//#include <ole2.h>           // OLE clipboard (files)
//#include <gdiplus.h>        // Graphics
//#include <htmlhelp.h>       // HTML help
//#include <shlwapi.h>        // Shell light weight ulility api
//#include <time.h>           // Time
#include "resource.h"       // Resources

#ifndef GCL_HICON
#define GCL_HICON (-14)
#endif

//**********************************************************

HINSTANCE g_instance;
bool      g_new;
FILETIME  g_ftLastWriteTime;
long      g_offset;
char      filename[1000];

INT_PTR CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
void LoadFile(HWND hDlg, char *filename, char *error, FILETIME *lastwritetime, bool newfile);
unsigned long long FixCRLF(char *inbuf, unsigned long long size, char *outbuf);

//**********************************************************

int CALLBACK WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpszCmdLine, int nCmdShow)
{
	g_instance = hInstance;
	strcpy(filename, lpszCmdLine);
	g_new = *lpszCmdLine ? true : false;

	DialogBox(hInstance, MAKEINTRESOURCE(IDD_DIALOG), NULL, DialogProc);

	return 0;
}

//**********************************************************

INT_PTR CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	char error[1000];
	static bool pause = false;

	switch (uMsg)
	{
	case WM_INITDIALOG:
		// Set Class icon
		SetClassLongPtr(hDlg, GCL_HICON, (LONG_PTR)LoadIcon(g_instance, MAKEINTRESOURCE(IDI_ICON)));

		SetTimer(hDlg, 1, 500, NULL);

		return TRUE;  // return FALSE if modeless and SetFocus is called

	case WM_DROPFILES:
		DragQueryFile((HDROP)wParam, 0, filename, MAX_PATH);
		DragFinish((HDROP)wParam);
		g_new = true;
		return TRUE;

	case WM_TIMER:
		if (!pause)
		{
			LoadFile(hDlg, filename, error, &g_ftLastWriteTime, g_new);
			g_new = false;

			if (*error)
			{
				pause = true;
				*filename = 0;
				MessageBox(hDlg, error, "Error", MB_OK);
				SetWindowText(hDlg, "Tail");
				pause = false;
			}
		}
		return TRUE;

	case WM_SIZE:
		MoveWindow(GetDlgItem(hDlg, IDC_TEXT), 0, 0, LOWORD(lParam), HIWORD(lParam), TRUE);
		return TRUE;

	case WM_COMMAND:
		switch (LOWORD(wParam))
		{
		case IDOK:
			return TRUE;

		case IDCANCEL:
			// Quit program...
			EndDialog(hDlg, wParam);  // Close dialogwindow
			return TRUE;
		}
	}

	return FALSE;
}

//**********************************************************

void LoadFile(HWND hDlg, char *filename, char *error, FILETIME *lastwritetime, bool newfile)
{
	*error = 0;

	if (!*filename)
	{
		return;
	}


	HANDLE hFind;
	WIN32_FIND_DATA FileEntry;
	if ((hFind = FindFirstFile(filename, &FileEntry)) == INVALID_HANDLE_VALUE)
	{
		sprintf(error, "Couldn't find file: '%s'", filename);
		return;
	}
	FindClose(hFind);


	if (CompareFileTime(&FileEntry.ftLastWriteTime, lastwritetime) <= 0)
	{
		return;
	}
	*lastwritetime = FileEntry.ftLastWriteTime;


	FILE *fh;
	if (!(fh = fopen(filename, "rb")))
	{
		sprintf(error, "Couldn't open file: '%s'", filename);
		return;
	}


	fseek(fh, 0, SEEK_END);
	long size = ftell(fh);

	unsigned long long bufsize;
	if (newfile)
	{
		g_offset = size;
		bufsize = 0;
	}
	else
	{
		if (size < g_offset)
		{
			g_offset = size;
		}

		bufsize = size - g_offset;
	}


	if (bufsize)
	{
		char *buf1;
		buf1 = (char *)malloc(bufsize);
		if (!buf1)
		{
			fclose(fh);
			sprintf(error, "Out of memory: %llu bytes.", bufsize);
			return;
		}

		char *buf2;
		buf2 = (char *)malloc(bufsize * 2 + 1);  // For null terminator
		if (!buf2)
		{
			fclose(fh);
			free(buf1);
			sprintf(error, "Out of memory: %llu bytes.", bufsize);
			return;
		}

		fseek(fh, g_offset, SEEK_SET);
		fread(buf1, bufsize, 1, fh);

		fclose(fh);

		bufsize = FixCRLF(buf1, bufsize, buf2);

		buf2[bufsize] = 0;  // Null terminate buf

		SetDlgItemText(hDlg, IDC_TEXT, buf2);

		free(buf1);
		free(buf2);
	}
	else
	{
		fclose(fh);
		SetDlgItemText(hDlg, IDC_TEXT, "");
	}


	char szTitle[1000];
	sprintf(szTitle, "%s - Tail", filename);
	SetWindowText(hDlg, szTitle);


	return;
}

//**********************************************************

unsigned long long FixCRLF(char *inbuf, unsigned long long size, char *outbuf)
{
	char *p1, *p2;

	for (p1 = inbuf, p2 = outbuf; p1 < inbuf + size; p1++, p2++)
	{
		if (*p1 == '\n' && (p1 == inbuf || *(p1 - 1) != '\r'))
		{
			*p2 = '\r';
			p2++;
		}
		*p2 = *p1;
	}

	return p2 - outbuf;
}

//**********************************************************
