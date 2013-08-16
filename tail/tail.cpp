//**********************************************************
//
// tail 0.1
//
// Shows text log files.
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

HINSTANCE g_hInstance;
char      g_szFileName[1000];

bool      g_new;
FILETIME  g_ftLastWriteTime;
long      g_offset;

BOOL CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
void LoadFile(HWND hDlg, char *szFileName, char *szError, FILETIME *ftLastWriteTime, bool newfile);
unsigned FixCRLF(char *inbuf, unsigned size, char *outbuf);

//**********************************************************

int CALLBACK WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
	LPSTR lpszCmdLine, int nCmdShow)
{
	g_hInstance = hInstance;

	strcpy(g_szFileName, lpszCmdLine);
	g_new = *lpszCmdLine ? true: false;

	DialogBox(hInstance, MAKEINTRESOURCE(IDD_DIALOG), NULL, DialogProc);

	return 0;
}

//**********************************************************

BOOL CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	char szError[1000];
	static bool pause = false;

	switch(uMsg)
	{
	case WM_INITDIALOG:
		// Set Class icon
		SetClassLong(hDlg, GCL_HICON, (LONG)LoadIcon(g_hInstance, MAKEINTRESOURCE(IDI_ICON)));

		SetTimer(hDlg, 1, 500, NULL);

		return TRUE;  // return FALSE if modeless and SetFocus is called

	case WM_DROPFILES:
		DragQueryFile((HDROP)wParam, 0, g_szFileName, MAX_PATH);
		DragFinish((HDROP)wParam);
		g_new = true;
		return TRUE;

	case WM_TIMER:
		if(!pause)
		{
			LoadFile(hDlg, g_szFileName, szError, &g_ftLastWriteTime, g_new);
			g_new = false;

			if(*szError)
			{
				pause = true;
				*g_szFileName = 0;
				MessageBox(hDlg, szError, "Error", MB_OK);
				SetWindowText(hDlg, "Tail");
				pause = false;
			}
		}
		return TRUE;

	case WM_SIZE:
		MoveWindow(GetDlgItem(hDlg, IDC_TEXT), 0, 0, LOWORD(lParam), HIWORD(lParam), TRUE);
		return TRUE;

	case WM_COMMAND:
		switch(LOWORD(wParam))
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

void LoadFile(HWND hDlg, char *szFileName, char *szError, FILETIME *ftLastWriteTime, bool newfile)
{
	*szError = 0;

	if(!*szFileName)
	{
		return;
	}


	HANDLE hFind;
	WIN32_FIND_DATA FileEntry;
	if((hFind=FindFirstFile(szFileName, &FileEntry)) == INVALID_HANDLE_VALUE)
	{
		sprintf(szError, "Couldn't find file (%s).", szFileName);
		return;
	}
	FindClose(hFind);


	if(CompareFileTime(&FileEntry.ftLastWriteTime, ftLastWriteTime) <= 0)
	{
		return;
	}
	*ftLastWriteTime = FileEntry.ftLastWriteTime;


	FILE *fh;
	if(!(fh = fopen(szFileName, "rb")))
	{
		sprintf(szError, "Couldn't open file (%s).", szFileName);
		return;
	}


	fseek(fh, 0, SEEK_END);
	long size = ftell(fh);

	unsigned bufsize;
	if(newfile)
	{
		g_offset = size;
		bufsize = 0;
	}
	else
	{
		if(size < g_offset)
			g_offset = size;

		bufsize = size - g_offset;
	}


	if(bufsize)
	{
		char *buf1;
		buf1 = new char[bufsize];
		if(!buf1)
		{
			fclose(fh);
			sprintf(szError, "Out of memory (%u bytes).", bufsize);
			return;
		}

		char *buf2;
		buf2 = new char[bufsize*2+1];  // For null terminator
		if(!buf2)
		{
			fclose(fh);
			delete[] buf1;
			sprintf(szError, "Out of memory (%u bytes).", bufsize);
			return;
		}

		fseek(fh, g_offset, SEEK_SET);
		fread(buf1, bufsize, 1, fh);

		fclose(fh);

		bufsize = FixCRLF(buf1, bufsize, buf2);

		buf2[bufsize] = 0;  // Null terminate buf

		SetDlgItemText(hDlg, IDC_TEXT, buf2);

		delete[] buf1;
		delete[] buf2;
	}
	else
	{
		fclose(fh);
		SetDlgItemText(hDlg, IDC_TEXT, "");
	}


	char szTitle[1000];
	sprintf(szTitle, "%s - Tail", szFileName);
	SetWindowText(hDlg, szTitle);


	return;
}

//**********************************************************

unsigned FixCRLF(char *inbuf, unsigned size, char *outbuf)
{
	char *p1, *p2;

	for(p1=inbuf,p2=outbuf; p1<inbuf+size; p1++,p2++)
	{
		if(*p1=='\n' && (p1==inbuf || *(p1-1)!='\r'))
		{
			*p2 = '\r';
			p2++;
		}
		*p2 = *p1;
	}

	return p2-outbuf;
}

//**********************************************************
