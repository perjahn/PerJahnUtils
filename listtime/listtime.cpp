//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <string.h>

//**********************************************************

struct direntry
{
	WIN32_FIND_DATA Data;
	unsigned long long w;
	unsigned long long c;
	unsigned long long a;
	SYSTEMTIME stW;
	SYSTEMTIME stC;
	SYSTEMTIME stA;
	unsigned long long s;
	direntry *pNext;
} *pStart, *pEnd;

unsigned _sort;

void ListDir(wchar_t *szPath);
void PrintEntry(direntry e);

HMODULE hModule;
int (WINAPI *StrCmpLogicalW)(LPCWSTR psz1, LPCWSTR psz2);

//**********************************************************

void wmain(int argc, wchar_t *argv[])
{
	if (argc > 3)
	{
		printf(
			"Usage listtime [-sX] [path]\n"
			"\n"
			" -sw - Sort by last write date.\n"
			" -sc - Sort by creation date.\n"
			" -sa - Sort by last access date.\n"
			" -ss - Sort by size.\n"
			" -sn - Sort by name.\n"
			" -si - Sort by name, case sensitive.\n"
			" -sl - Sort by name, Logical.\n"
			"\n"
			"Uppercase sort option (-sW, -sC, -sA, -sN, -sI, -sL) means 'sort backwards.'\n");
		return;
	}


	// Late binding of compare function, does only exist in WinXP or newer OS.
	hModule = LoadLibrary(L"shlwapi.dll");
	if (hModule)
		StrCmpLogicalW = (int (WINAPI *)(LPCWSTR psz1, LPCWSTR psz2))GetProcAddress(hModule, "StrCmpLogicalW");
	else
		StrCmpLogicalW = NULL;


	_sort = 0;

	if (argc > 1)
	{
		if (!_wcsicmp(argv[1], L"-sw"))
			_sort = 1;
		else if (!_wcsicmp(argv[1], L"-sc"))
			_sort = 2;
		else if (!_wcsicmp(argv[1], L"-sa"))
			_sort = 3;
		else if (!_wcsicmp(argv[1], L"-ss"))
			_sort = 4;
		else if (!_wcsicmp(argv[1], L"-sn"))
			_sort = 5;
		else if (!_wcsicmp(argv[1], L"-si"))
			_sort = 6;
		else if (!_wcsicmp(argv[1], L"-sl"))
			_sort = 7;
		else
			;  // Unsorted

		if (_sort > 0 && isupper(argv[1][2]))
			_sort |= 0x10;

		if (argc == 3)
			ListDir(argv[2]);
		else
			ListDir(argv[1]);
	}
	else
		ListDir(L"*");

	return;
}

//**********************************************************

int compare(const void *arg1, const void *arg2)
{
	direntry *entry1, *entry2;
	unsigned long long x1, x2;

	entry1 = (direntry*)arg1;
	entry2 = (direntry*)arg2;

	switch (_sort&~0x10)
	{
	case 1:
	{
					x1 = entry1->w;
					x2 = entry2->w;
					if (_sort & 0x10)
						return (x1 == x2) ? 0 : ((x1<x2) ? -1 : 1);
					else
						return (x1 == x2) ? 0 : ((x1>x2) ? -1 : 1);
	}
	case 2:
	{
					x1 = entry1->c;
					x2 = entry2->c;
					if (_sort & 0x10)
						return (x1 == x2) ? 0 : ((x1<x2) ? -1 : 1);
					else
						return (x1 == x2) ? 0 : ((x1>x2) ? -1 : 1);
	}
	case 3:
	{
					x1 = entry1->a;
					x2 = entry2->a;
					if (_sort & 0x10)
						return (x1 == x2) ? 0 : ((x1<x2) ? -1 : 1);
					else
						return (x1 == x2) ? 0 : ((x1>x2) ? -1 : 1);
	}
	case 4:
	{
					x1 = entry1->s;
					x2 = entry2->s;
					if (_sort & 0x10)
						return (x1 == x2) ? 0 : ((x1<x2) ? -1 : 1);
					else
						return (x1 == x2) ? 0 : ((x1>x2) ? -1 : 1);
	}
	case 5:
	{
					if (_sort & 0x10)
						return _wcsicmp(entry2->Data.cFileName, entry1->Data.cFileName);
					else
						return _wcsicmp(entry1->Data.cFileName, entry2->Data.cFileName);
	}
	case 6:
	{
					if (_sort & 0x10)
						return wcscmp(entry2->Data.cFileName, entry1->Data.cFileName);
					else
						return wcscmp(entry1->Data.cFileName, entry2->Data.cFileName);
	}
	case 7:
	{
					if (!StrCmpLogicalW)
						return 0;

					if (_sort & 0x10)
						return StrCmpLogicalW(entry2->Data.cFileName, entry1->Data.cFileName);
					else
						return StrCmpLogicalW(entry1->Data.cFileName, entry2->Data.cFileName);
	}
	default:
		return 0;
	}
}

//**********************************************************

void ListDir(wchar_t *szPath)
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;
	direntry *pNode, *EntryArray;
	unsigned count = 0;
	unsigned i;


	// Search for files and folders
	pStart = pEnd = NULL;
	if ((hFind = FindFirstFile(szPath, &Data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			pNode = new direntry;
			if (pNode)
			{
				pNode->Data = Data;
				pNode->pNext = NULL;
				count++;

				if (!pStart)
				{
					// Insert node at start of list
					pStart = pEnd = pNode;
				}
				else
				{
					// Insert node at end of list
					pEnd->pNext = pNode;
					pEnd = pNode;
				}
			}
		} while (FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	// Compact list to array
	EntryArray = new direntry[count];
	if (!EntryArray)
	{
		printf("Out of memory (%u bytes).\n", sizeof(direntry)*count);
		return;
	}

	i = 0;
	for (pNode = pStart; pNode; pNode = pNode->pNext)
	{
		EntryArray[i++].Data = pNode->Data;
	}


	// Precompute converted dates & size
	for (i = 0; i < count; i++)
	{
		direntry e = EntryArray[i];

		EntryArray[i].w = (((unsigned long long)(e.Data.ftLastWriteTime.dwHighDateTime)) << 32) | e.Data.ftLastWriteTime.dwLowDateTime;
		EntryArray[i].c = (((unsigned long long)(e.Data.ftCreationTime.dwHighDateTime)) << 32) | e.Data.ftCreationTime.dwLowDateTime;
		EntryArray[i].a = (((unsigned long long)(e.Data.ftLastAccessTime.dwHighDateTime)) << 32) | e.Data.ftLastAccessTime.dwLowDateTime;

		FileTimeToSystemTime(&(e.Data.ftLastWriteTime), &(EntryArray[i].stW));
		FileTimeToSystemTime(&(e.Data.ftCreationTime), &(EntryArray[i].stC));
		FileTimeToSystemTime(&(e.Data.ftLastAccessTime), &(EntryArray[i].stA));

		EntryArray[i].s = (((unsigned long long)(e.Data.nFileSizeHigh)) << 32) | e.Data.nFileSizeLow;
	}


	// Sort array
	if (_sort)
		qsort(EntryArray, count, sizeof(direntry), compare);


	// Print array
	for (i = 0; i < count; i++)
	{
		if (i == 0)
			printf("          LastWriteTime             CreationTime           LastAccessTime         Size  Filename\n");

		PrintEntry(EntryArray[i]);
	}


	return;
}

//**********************************************************

void PrintEntry(direntry e)
{
	char szFileName[2000];

	if (*(e.Data.cFileName) && wcscmp(e.Data.cFileName, L".") && wcscmp(e.Data.cFileName, L".."))
	{
		if (e.Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
		{
			// Dir

			CharToOem(e.Data.cFileName, szFileName);

			short x = 1;
			wprintf(
				L"%4hu-%02hu-%02hu %02hu:%02hu.%02hu.%03hu  "
				L"%4hu-%02hu-%02hu %02hu:%02hu.%02hu.%03hu  "
				L"%4hu-%02hu-%02hu %02hu:%02hu.%02hu.%03hu  "
				L"             "
				L"%S\n",
				e.stW.wYear, e.stW.wMonth, e.stW.wDay, e.stW.wHour, e.stW.wMinute, e.stW.wSecond, e.stW.wMilliseconds,
				e.stC.wYear, e.stC.wMonth, e.stC.wDay, e.stC.wHour, e.stC.wMinute, e.stC.wSecond, e.stC.wMilliseconds,
				e.stA.wYear, e.stA.wMonth, e.stA.wDay, e.stA.wHour, e.stA.wMinute, e.stA.wSecond, e.stA.wMilliseconds,
				szFileName);
		}
		else
		{
			// File

			CharToOem(e.Data.cFileName, szFileName);

			short x = 1;
			wprintf(
				L"%4hu-%02hu-%02hu %02hu:%02hu.%02hu.%03hu  "
				L"%4hu-%02hu-%02hu %02hu:%02hu.%02hu.%03hu  "
				L"%4hu-%02hu-%02hu %02hu:%02hu.%02hu.%03hu  "
				L"%11llu  "
				L"%S\n",
				e.stW.wYear, e.stW.wMonth, e.stW.wDay, e.stW.wHour, e.stW.wMinute, e.stW.wSecond, e.stW.wMilliseconds,
				e.stC.wYear, e.stC.wMonth, e.stC.wDay, e.stC.wHour, e.stC.wMinute, e.stC.wSecond, e.stC.wMilliseconds,
				e.stA.wYear, e.stA.wMonth, e.stA.wDay, e.stA.wHour, e.stA.wMinute, e.stA.wSecond, e.stA.wMilliseconds,
				e.s,
				szFileName);
		}
	}

	return;
}

//**********************************************************
