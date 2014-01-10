//**********************************************************
//
// DeleteOld 1.4
//
// Written by Per Jahn
//
// Necessary static link-libraries:
// -
//
// VS Settings:
// Character Set = Not Set
//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

//**********************************************************

bool g_deletedirs = false;
FILE *fhLogFile = NULL;
bool g_recurse = false;
bool g_simulate = false;
int g_count = -1;  // Maximum file count in each directory (instead of date threshold). -1 = don't use.
unsigned g_usedate = 1;  // bit pattern: bit0=last modified, bit1=created. bitwise and evaluation, deletes as few files as possible.
FILETIME g_ftOld;
int g_depth;

WIN32_FIND_DATA exclude_names[100000];  // Array with excluded entries.

int SetOptions(int argc, char *argv[]);
void PrintTime(char *szPrefix, unsigned long long time);
void PrintTime(char *szPrefix, FILETIME *ft);
void ExpandPath(char *szPath);
void ProcessFile(char *szFileName, FILETIME *ft);
int compare(const void *arg1, const void *arg2);

//**********************************************************

void main(int argc, char *argv[])
{
	int params;
	FILETIME ft;
	unsigned long long oldtime;
	unsigned long long oneday;

	params = SetOptions(argc, argv);

	if(params != 2)
	{
		printf(
			"DeleteOld 1.4\n"
			"\n"
			"Usage: deleteold [-c|-c2] [-d] [-l] [-n] [-r] [-s] <path> <days>\n"
			" -c   - match against create date instead of last modified date\n"
			" -c2  - match against newest of create and last modified dates\n"
			" -d   - delete empty dirs (not checked against specified timedate threshold)\n"
			" -l   - log to %%temp%%\\deleteold.txt\n"
			" -n   - number of files to keep instead of days\n"
			" -r   - recurse through subdirs (not matched against specified file pattern)\n"
			" -s   - simulate\n"
			" path - dir/file/pattern to delete\n"
			" days - timedate threshold\n");
		return;
	}

	if(g_count>=0)
	{
		g_count = atoi(argv[argc-1]);
		if(g_count<1)
		{
			printf("Number of files (days) must be atleast 1 when using '-n'.\n");
			return;
		}
	}
	else
	{
		GetSystemTimeAsFileTime(&ft);
		FileTimeToLocalFileTime(&ft, &g_ftOld);

		PrintTime("Now", &g_ftOld);

		oldtime = (((unsigned long long)(g_ftOld.dwHighDateTime))<<32)+g_ftOld.dwLowDateTime;
		oneday = (unsigned long long)24*3600*10000000;
		oldtime -= atoi(argv[argc-1])*oneday;

		g_ftOld.dwHighDateTime = (DWORD)(oldtime>>32);
		g_ftOld.dwLowDateTime = (DWORD)(oldtime&0x00000000FFFFFFFF);

		PrintTime("Old", &g_ftOld);
	}

	g_depth = 0;
	ExpandPath(argv[argc-2]);

	if(fhLogFile)
		fclose(fhLogFile);

	return;
}

//**********************************************************
// Return resulting parameters after options

int SetOptions(int argc, char *argv[])
{
	bool bFoundOption = true;
	int i = 1;

	while(i<argc && bFoundOption)
	{
		bFoundOption = false;
		if(i<argc && !strcmp(argv[i], "-c"))
		{
			g_usedate = 2;
			i++;
			bFoundOption = true;
		}
		if(i<argc && !strcmp(argv[i], "-c2"))
		{
			g_usedate = 3;
			i++;
			bFoundOption = true;
		}
		if(i<argc && !strcmp(argv[i], "-d"))
		{
			g_deletedirs = true;
			i++;
			bFoundOption = true;
		}
		if(i<argc && !strcmp(argv[i], "-l"))
		{
			char szTempDir[1000], szTempFileName[1000];
			GetTempPath(1000, szTempDir);
			sprintf(szTempFileName, "%s\\deleteold.txt", szTempDir);
			fhLogFile = fopen(szTempFileName, "a");
			if(fhLogFile)
				fprintf(fhLogFile, "-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-\n");
			i++;
			bFoundOption = true;
		}
		if(i<argc && !strcmp(argv[i], "-n"))
		{
			g_count = 0;  // Enable
			i++;
			bFoundOption = true;
		}
		if(i<argc && !strcmp(argv[i], "-r"))
		{
			g_recurse = true;
			i++;
			bFoundOption = true;
		}
		if(i<argc && !strcmp(argv[i], "-s"))
		{
			g_simulate = true;
			i++;
			bFoundOption = true;
		}
	}

	return argc-i;
}

//**********************************************************

void PrintTime(char *szPrefix, unsigned long long time)
{
	FILETIME ft;

	ft.dwHighDateTime = (DWORD)(time>>32);
	ft.dwLowDateTime = (DWORD)(time&0x00000000FFFFFFFF);

	PrintTime(szPrefix, &ft);

	return;
}

//**********************************************************

void PrintTime(char *szPrefix, FILETIME *ft)
{
	SYSTEMTIME st;

	FileTimeToSystemTime(ft, &st);
	printf("%s: %04hu-%02hu-%02hu %02hu:%02hu:%02hu.%03hu\n",
		szPrefix,
		st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
	if(fhLogFile)
		fprintf(fhLogFile, "%s: %04hu-%02hu-%02hu %02hu:%02hu:%02hu.%03hu\n",
		szPrefix,
		st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);

	return;
}

//**********************************************************
/*
szPath can be:
1: filepattern (i.e, including ? and/or *)
2: file
3: dir (which may end with : or \) (x: will search x:*)
4: embedded wildcards (? and *) in part of path: c:\a*b\d?e\file.txt

use szPath (i.e. argv) to search with
*/

void ExpandPath(char *szPath)
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;
	char szSearch[1000];
	char szSubDir[1000];
	char szFileName[1000];
	unsigned l;

	if(g_depth>100)
	{
		printf("Maximum recursedepth reached: '%s'\n", szPath);
		return;
	}
	g_depth++;

	// Find first occurance of x\x*x\x or x\x?x\x
	char *p, *p1, *p2;

	for(p=p1=szPath; p<szPath+strlen(szPath) && *p!='?' && *p!='*'; p++)
	{
		if(*p=='\\')
		{
			p1 = p+1;
		}
	}

	// Step to next backslash
	for(; p<szPath+strlen(szPath) && *p!='\\'; p++)
		;
	p2 = *p? p: NULL;

	if(p2)
	{
		// It exists a backslash after a wildcard character - an embedded wildcard

		// Expand embedded wildcard to directories

		// p1->first char of token, p2->backslash after token

		int size = p2-szPath;

		memcpy(szSearch, szPath, size);
		szSearch[size] = 0;  // Null terminate string

		//printf("Expanding: '%s'\n", szSearch);

		hFind = FindFirstFile(szSearch, &Data);
		if(hFind != INVALID_HANDLE_VALUE)
		{
			do
			{
				if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
				{
					if(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
					{
						// Dir

						strcpy(szSubDir, szSearch);

						for(p=szSubDir+strlen(szSubDir); p>szSubDir && *(p-1)!='\\' && *(p-1)!=':'; p--)
							;

						sprintf(p, "%s\\%s", Data.cFileName, p2+1);

						ExpandPath(szSubDir);
					}
					else
					{
						// File
					}
				}
			}
			while(FindNextFile(hFind, &Data));

			FindClose(hFind);
		}
	}
	else
	{
		if(g_count!=-1)
		{
			int countAll = 0, countMatch = 0;

			// Count total number of files in dir

			strcpy(szSearch, szPath);
			for(p=szSearch+strlen(szSearch); p>szSearch && *(p-1)!='\\' && *(p-1)!=':'; p--)
				;
			strcpy(p, "*");

			hFind = FindFirstFile(szSearch, &Data);
			if(hFind != INVALID_HANDLE_VALUE)
			{
				do
				{
					if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
					{
						if(!(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
						{
							countAll++;
						}
					}
				}
				while(FindNextFile(hFind, &Data));

				FindClose(hFind);
			}

			if(countAll>g_count)
			{
				// To many files in dir, delete the oldest matching.

				hFind = FindFirstFile(szPath, &Data);
				if(hFind != INVALID_HANDLE_VALUE)
				{
					do
					{
						if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
						{
							if(!(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
							{
								if(countMatch<100000)
								{
									exclude_names[countMatch] = Data;
									countMatch++;
								}
								else
									printf("Out of memory (%s)\n", szPath);
							}
						}
					}
					while(FindNextFile(hFind, &Data));

					FindClose(hFind);
				}

				if(countMatch>0)
				{
					// Some files has to go

					int countDelete = countAll-g_count;

					// File system is not in a constant state when enumerating files,
					// prevent to many matches.
					if(countDelete>countMatch)
						countDelete = countMatch;

					qsort(exclude_names, countMatch, sizeof(WIN32_FIND_DATA), compare);

					for(int i=0; i<countDelete; i++)
					{
						strcpy(szFileName, szPath);
						for(p=szFileName+strlen(szFileName); p>szFileName && *(p-1)!='\\' && *(p-1)!=':'; p--)
							;
						strcpy(p, exclude_names[i].cFileName);

						ProcessFile(szFileName, NULL);
					}
				}
			}
		}
		else
		{
			hFind = FindFirstFile(szPath, &Data);
			if(hFind != INVALID_HANDLE_VALUE)
			{
				do
				{
					if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
					{
						if(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
						{
							// Dir
						}
						else
						{
							// File

							strcpy(szFileName, szPath);
							for(p=szFileName+strlen(szFileName); p>szFileName && *(p-1)!='\\' && *(p-1)!=':'; p--)
								;
							strcpy(p, Data.cFileName);

							FILETIME *ft;

							if(g_usedate==1)
								ft = &(Data.ftLastWriteTime);
							else if(g_usedate==2)
								ft = &(Data.ftCreationTime);
							else
							{
								LONG l;
								l = CompareFileTime(&(Data.ftCreationTime), &(Data.ftLastWriteTime));

								// Delete as few files as possible
								ft = (l<0) ? &(Data.ftLastWriteTime): &(Data.ftCreationTime);
							}

							ProcessFile(szFileName, ft);
						}
					}
				}
				while(FindNextFile(hFind, &Data));

				FindClose(hFind);
			}
		}

	/*
		else
		{
			DWORD dwErr = GetLastError();
			char g_szError[1000];
			char szErr[1000];

			if (!FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, dwErr, 0, szErr, 1000, NULL))
			{
				*g_szError = 0;
				printf("Format message failed with 0x%08X\n", GetLastError());
			}

			printf("'%s'\n0x%08X - %s", szPath, dwErr, szErr);
		}
	*/

		if(strchr(szPath, '*') || strchr(szPath, '?'))
		{
			strcpy(szSearch, szPath);
			for(p=szSearch+strlen(szSearch); p>szSearch && *(p-1)!='\\' && *(p-1)!=':'; p--)
				;
			strcpy(p, "*");
		}
		else
		{
			strcpy(szSearch, szPath);
		}


		if(g_recurse)
		{
			hFind = FindFirstFile(szSearch, &Data);
			if(hFind != INVALID_HANDLE_VALUE)
			{
				do
				{
					if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
					{
						if(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
						{
							// Dir

							for(p=szPath+strlen(szPath); p>szPath && *(p-1)!='\\' && *(p-1)!=':'; p--)
								;

							l = (long)(p-szPath);
							memcpy(szSubDir, szPath, l);
							szSubDir[l] = 0;

							if(strchr(szPath, '*') || strchr(szPath, '?'))
							{
								sprintf(szSubDir+l, "%s\\%s", Data.cFileName, p);
							}
							else
							{
								sprintf(szSubDir+l, "%s\\%s", Data.cFileName, "*");
							}
							
							ExpandPath(szSubDir);


							if(g_deletedirs)
							{
								for(p=szSubDir+strlen(szSubDir); p>szSubDir && *(p-1)!='\\' && *(p-1)!=':'; p--)
									;
								if(p>szSubDir)
									*(p-1) = 0;
								else
									*p = 0;


								if(fhLogFile)
									fprintf(fhLogFile, "%s\n", szSubDir);

								if(!g_simulate)
								{
									RemoveDirectory(szSubDir);
								}
							}
						}
						else
						{
							// File
						}
					}
				}
				while(FindNextFile(hFind, &Data));

				FindClose(hFind);
			}
		}
	}

	g_depth--;

	return;
}

//**********************************************************
// Delete file if older than date

void ProcessFile(char *szFileName, FILETIME *ft)
{
	LONG l;
///	unsigned long long t;

	///t = (((unsigned long long)(ft->dwHighDateTime))<<32)+ft->dwLowDateTime;

	if(ft)
	{
		l = CompareFileTime(ft, &g_ftOld);
	}
	else
	{
		l = -1;
	}

	if(l<0)
	{
		printf("%s\n", szFileName);
		if(fhLogFile)
			fprintf(fhLogFile, "%s\n", szFileName);

		if(!g_simulate)
		{
			SetFileAttributes(szFileName, 0);
			DeleteFile(szFileName);
		}
	}

	return;
}

//**********************************************************

int compare(const void *arg1, const void *arg2)
{
	WIN32_FIND_DATA *entry1, *entry2;

	entry1 = (WIN32_FIND_DATA*)arg1;
	entry2 = (WIN32_FIND_DATA*)arg2;


	FILETIME *ft1, *ft2;
	LONG l;


	if(g_usedate==1)
	{
		ft1 = &(entry1->ftLastWriteTime);
		ft2 = &(entry2->ftLastWriteTime);
	}
	else if(g_usedate==2)
	{
		ft1 = &(entry1->ftCreationTime);
		ft2 = &(entry2->ftCreationTime);
	}
	else
	{
		l = CompareFileTime(&(entry1->ftCreationTime), &(entry1->ftLastWriteTime));
		ft1 = (l<0) ? &(entry1->ftLastWriteTime): &(entry1->ftCreationTime);

		l = CompareFileTime(&(entry2->ftCreationTime), &(entry2->ftLastWriteTime));
		ft2 = (l<0) ? &(entry2->ftLastWriteTime): &(entry2->ftCreationTime);
	}


	l = CompareFileTime(ft1, ft2);

	return l;
}

//**********************************************************
