//**********************************************************
/*
Application for de-linking solutions & projects with TFS.

1.
attrib -r -s -h -a *.* /s
for /d /r %%a in (*) do attrib -r -s -h -a "%%a"

2.
del *.vssscc /s
del *.vspscc /s

3.
rep -s "    <SccProjectName>SAK</SccProjectName>\0D\0A" "" *
rep -s "    <SccLocalPath>SAK</SccLocalPath>\0D\0A" "" *
rep -s "    <SccAuxPath>SAK</SccAuxPath>\0D\0A" "" *
rep -s "    <SccProvider>SAK</SccProvider>\0D\0A" "" *

4.
remove global sections with scc in *.sln
*/
//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <string.h>

//**********************************************************

bool g_simulate;
bool g_verbose;
int g_counts[4];

//**********************************************************

void DelinkTFS(char *szPath);
void PrintStat(void);

void FixAttrib(char *szSubPath);
void MyDeleteFile(char *szFileName);
void FixProjectFile(char *szFileName);
void FixSolutionFile(char *szFileName);

//**********************************************************

void main(int argc, char *argv[])
{
	if(argc==2)
	{
		g_simulate = false;
		DelinkTFS(argv[1]);
	}
	else if(argc==3 && !strcmp(argv[1], "-s"))
	{
		g_simulate = true;
		g_verbose = false;
		DelinkTFS(argv[2]);
	}
	else if(argc==3 && !strcmp(argv[1], "-v"))
	{
		g_simulate = false;
		g_verbose = true;
		DelinkTFS(argv[2]);
	}
	else if(argc==4 &&
		((!strcmp(argv[1], "-s") && !strcmp(argv[2], "-v")) ||
		(!strcmp(argv[1], "-v") && !strcmp(argv[2], "-s"))))
	{
		g_simulate = true;
		g_verbose = true;
		DelinkTFS(argv[3]);
	}
	else
	{
		printf(
			"Usage: detfs [-s] [-v] <path>\n"
			"\n"
			"-s  Simulate\n"
			"-v  Verbose logging\n");
		return;
	}

	PrintStat();
}

//**********************************************************

void DelinkTFS(char *szPath)
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;
	char *p, szSubPath[1000];

	strcpy(szSubPath, szPath);
	p = szSubPath+strlen(szSubPath);


	// Remove r/o attrib
	strcpy(p, "\\*");
	if((hFind=FindFirstFile(szSubPath, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				sprintf(p, "\\%s", Data.cFileName);
				FixAttrib(szSubPath);
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	// Del *.vspscc
	strcpy(p, "\\*.vspscc");
	if((hFind=FindFirstFile(szSubPath, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if(!(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
				{
					sprintf(p, "\\%s", Data.cFileName);
					MyDeleteFile(szSubPath);
				}
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	// Del *.vssscc
	strcpy(p, "\\*.vssscc");
	if((hFind=FindFirstFile(szSubPath, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if(!(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
				{
					sprintf(p, "\\%s", Data.cFileName);
					MyDeleteFile(szSubPath);
				}
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	// Fix projects
	// todo: Add support for .vbproj/.vcxproj
	strcpy(p, "\\*.csproj");
	if((hFind=FindFirstFile(szSubPath, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if(!(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
				{
					sprintf(p, "\\%s", Data.cFileName);
					FixProjectFile(szSubPath);
				}
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	// Fix solutions
	strcpy(p, "\\*.sln");
	if((hFind=FindFirstFile(szSubPath, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if(!(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
				{
					sprintf(p, "\\%s", Data.cFileName);
					FixSolutionFile(szSubPath);
				}
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	// Recurse subdirs
	strcpy(p, "\\*");
	if((hFind=FindFirstFile(szSubPath, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				{
					sprintf(p, "\\%s", Data.cFileName);
					DelinkTFS(szSubPath);
				}
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	return;
}

//**********************************************************

void PrintStat(void)
{
	printf(
		"Attributes changed: %u\n"
		"Files deleted:      %u\n"
		"Projects fixed:     %u\n"
		"Solutions fixed:    %u\n",
		g_counts[0], g_counts[1], g_counts[2], g_counts[3]);

	return;
}

//**********************************************************
