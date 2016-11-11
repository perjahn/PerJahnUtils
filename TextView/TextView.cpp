//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <malloc.h>
#include "resource.h"

#ifndef GCL_HICON
#define GCL_HICON (-14)
#endif

//**********************************************************

HINSTANCE g_instance;
char *g_commandline;

INT_PTR CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
void LoadFile(HWND hDlg, char *filename);

//**********************************************************

int CALLBACK WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	g_instance = hInstance;
	g_commandline = lpCmdLine;

	DialogBox(hInstance, MAKEINTRESOURCE(IDD_DIALOG), NULL, DialogProc);

	return 0;
}

//**********************************************************

INT_PTR CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	unsigned result;
	char filename[1000];

	switch (uMsg)
	{
	case WM_INITDIALOG:
		SetClassLongPtr(hDlg, GCL_HICON, (LONG_PTR)LoadIcon(g_instance, MAKEINTRESOURCE(IDI_ICON)));

		if (g_commandline && *g_commandline)
		{
			LoadFile(hDlg, g_commandline);
		}
		return TRUE;

	case WM_DROPFILES:
		result = DragQueryFile((HDROP)wParam, 0, filename, 1000);
		DragFinish((HDROP)wParam);
		if (!result)
		{
			MessageBox(hDlg, "Couldn't retrive filename.", "Error", MB_OK);
		}
		else
		{
			LoadFile(hDlg, filename);
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

void LoadFile(HWND hDlg, char *filename)
{
	char error[2000];
	FILE *fh;

	if (!(fh = fopen(filename, "rb")))
	{
		sprintf(error, "Couldn't open file: '%s'", filename);
		MessageBox(hDlg, error, "Error", MB_OK);
		return;
	}

	fseek(fh, 0, SEEK_END);
	long filesize = ftell(fh);
	long bufsize = filesize + 1;

	char *buf = (char *)malloc(bufsize);
	if (!buf)
	{
		fclose(fh);
		sprintf(error, "Couldn't allocate memory: %u bytes", bufsize);
		MessageBox(hDlg, error, "Error", MB_OK);
		return;
	}

	fseek(fh, 0, SEEK_SET);

	fread(buf, filesize + 1, 1, fh);

	fclose(fh);

	for (int i = 0; i < filesize; i++)
	{
		if (!buf[i])
		{
			buf[i] = ' ';
		}
	}
	buf[bufsize - 1] = 0;

	SetWindowText(hDlg, filename);
	SetDlgItemText(hDlg, IDC_TEXT, (char *)buf);

	free(buf);
}

//**********************************************************
