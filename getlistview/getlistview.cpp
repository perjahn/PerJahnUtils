//**********************************************************

#include <windows.h>
#include <commctrl.h>
#include <stdio.h>
#include <stdlib.h>

//**********************************************************

void ListList(void);
BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam);
BOOL CALLBACK EnumChildProc(HWND hwnd, LPARAM lParam);

//**********************************************************

void main(int argc, char *argv[])
{
	if(argc!=2)
	{
		printf("Usage: getlistview <window handle>\n\n");
		ListList();
		return;
	}

	InitCommonControls();

	long l = strtoul(argv[1], NULL, 16);
	HWND hwndList = (HWND)l;

	int items = ListView_GetItemCount(hwndList);

	/*wchar_t wszClass[1000];
	if(GetClassName(hwndList, wszClass, 1000))
	{
		printf("'%S'\n", wszClass);
	}
	else
	{
		printf("%u\n", GetLastError());
	}*/

	int cols = Header_GetItemCount(ListView_GetHeader(hwndList));

	//printf("items:%d, cols:%d\n", items, cols);

	// Todo: Borde kunna känna av om process är 32/64-bit och allokera rätt typ
	// av struct i processen. Och sen alltid läsa av från en 64-bit kompilerad exe.

	LVITEM lvi;
	wchar_t wszText[1000];
	DWORD pid;
	void *plvi, *text;
	HANDLE hProc;
	SIZE_T copied;

	GetWindowThreadProcessId(hwndList, &pid);
	hProc = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
	plvi = VirtualAllocEx(hProc, NULL, sizeof(LVITEM), MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);
	text = VirtualAllocEx(hProc, NULL, 2000, MEM_RESERVE | MEM_COMMIT, PAGE_READWRITE);


	for(int i=0; i<items; i++)
	{
		for(int c=0; c<cols; c++)
		{
			ZeroMemory(&lvi, sizeof(LVITEM));
			lvi.pszText = (wchar_t*)text;
			lvi.cchTextMax = 1000;
			lvi.iSubItem = c;

			WriteProcessMemory(hProc, plvi, &lvi, sizeof(LVITEM), &copied);
			SendMessage(hwndList, LVM_GETITEMTEXT, i, (LPARAM)plvi);
			ReadProcessMemory(hProc, text, (LPVOID)wszText, 2000, &copied);

			//printf(c?"\t'%S'":"'%S'", wszText);
			printf(c?"\t%S":"%S", wszText);
		}

		printf("\n");
	}

	VirtualFreeEx(hProc, text, 0, MEM_RELEASE);
	VirtualFreeEx(hProc, plvi, 0, MEM_RELEASE);


	return;
}

//**********************************************************

void ListList(void)
{
	EnumWindows(EnumWindowsProc, 0);

	return;
}

//**********************************************************

BOOL CALLBACK EnumWindowsProc(HWND hwnd, LPARAM lParam)
{
	wchar_t szTitle[1000];

	if(!GetWindowText(hwnd, szTitle, 1000))
	{
		*szTitle = 0;
	}

	EnumChildWindows(hwnd, EnumChildProc, (LPARAM)szTitle);

	return TRUE;
}

//**********************************************************

BOOL CALLBACK EnumChildProc(HWND hwnd, LPARAM lParam)
{
	wchar_t szClassName[1000];
	wchar_t *pszTitle = (wchar_t*)lParam;

	if(GetClassName(hwnd, szClassName, 1000))
	{
		if(!wcscmp(szClassName, L"SysListView32"))
		{
			//CharToOem(pszTitle, szTitle);  // Convert to DOS code page
			printf("'%S' %08X\n", pszTitle, hwnd);
		}
	}

	return TRUE;
}

//**********************************************************
