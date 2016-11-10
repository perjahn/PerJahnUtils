#include <windows.h>
#include <stdio.h>
#include <malloc.h>
#include "resource.h"

char *commandline;

INT_PTR CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
void ReadFile(char *filename, HWND hDlg);

int CALLBACK WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	commandline = lpCmdLine;

	DialogBox(hInstance, MAKEINTRESOURCE(IDD_DIALOG), NULL, DialogProc);

	return 0;
}

INT_PTR CALLBACK DialogProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	RECT rc;
	unsigned result;
	char filename[1000];

	switch (uMsg)
	{
	case WM_INITDIALOG:
		if (commandline && *commandline)
		{
			ReadFile(commandline, hDlg);
		}
		return TRUE;

	case WM_DROPFILES:
		result = DragQueryFile((HDROP)wParam, 0, filename, 1000);
		if (!result)
		{
			MessageBox(hDlg, "Couldn't retrive filename.", "Error", MB_OK);
		}
		else
		{
			ReadFile(filename, hDlg);
		}
		return TRUE;

	case WM_SIZE:
		GetClientRect(hDlg, &rc);
		MoveWindow(GetDlgItem(hDlg, IDC_TEXT), 0, 0, rc.right, rc.bottom, TRUE);
		return TRUE;

	case WM_CLOSE:
		EndDialog(hDlg, 0);
		return TRUE;
	}

	return FALSE;
}

void ReadFile(char *filename, HWND hDlg)
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
