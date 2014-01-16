//**********************************************************
/*
Application for delinking solutions & projects with TFS.

1.0 - Initial release.
1.1 - Updated to handle projects in sln files.
1.2 - Reverted to old logic.
1.3 - Fixed bug in sln handling. Not yet :)
1.4 - Added support for wixproj.

Program corresponds somewhat to the following 4 tasks:

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
+vdproj

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
	char *path;

	if (argc == 2)
	{
		g_simulate = false;
		path = argv[1];
	}
	else if (argc == 3 && !strcmp(argv[1], "-s"))
	{
		g_simulate = true;
		g_verbose = false;
		path = argv[2];
	}
	else if (argc == 3 && !strcmp(argv[1], "-v"))
	{
		g_simulate = false;
		g_verbose = true;
		path = argv[2];
	}
	else if (argc == 4 &&
		((!strcmp(argv[1], "-s") && !strcmp(argv[2], "-v")) ||
		(!strcmp(argv[1], "-v") && !strcmp(argv[2], "-s"))))
	{
		g_simulate = true;
		g_verbose = true;
		path = argv[3];
	}
	else
	{
		printf(
			"detfs 1.4\n"
			"\n"
			"Usage: detfs [-s] [-v] <path>\n"
			"\n"
			"-s  Simulate\n"
			"-v  Verbose logging\n");
		return;
	}

	DelinkTFS(path);

	PrintStat();
}

//**********************************************************

void EnumAll(char *buf, char *p, char *pattern, void(*Action)(char *path))
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;

	*p = '\\';
	strcpy(p + 1, pattern);
	if ((hFind = FindFirstFile(buf, &Data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				sprintf(p, "\\%s", Data.cFileName);
				Action(buf);
			}
		} while (FindNextFile(hFind, &Data));

		FindClose(hFind);
	}
}

//**********************************************************

void EnumFiles(char *buf, char *p, char *pattern, void(*FileAction)(char *path))
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;

	*p = '\\';
	strcpy(p + 1, pattern);
	if ((hFind = FindFirstFile(buf, &Data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if (!(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
				{
					sprintf(p, "\\%s", Data.cFileName);
					FileAction(buf);
				}
			}
		} while (FindNextFile(hFind, &Data));

		FindClose(hFind);
	}
}

//**********************************************************

void EnumDirs(char *buf, char *p, char *pattern, void(*DirAction)(char *path))
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;

	*p = '\\';
	strcpy(p + 1, pattern);
	if ((hFind = FindFirstFile(buf, &Data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if (Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				{
					sprintf(p, "\\%s", Data.cFileName);
					DirAction(buf);
				}
			}
		} while (FindNextFile(hFind, &Data));

		FindClose(hFind);
	}
}

//**********************************************************

void DelinkTFS(char *szPath)
{
	char *p, szSubPath[1000];

	strcpy(szSubPath, szPath);
	p = szSubPath + strlen(szSubPath);


	EnumAll(szSubPath, p, "*", FixAttrib);

	EnumFiles(szSubPath, p, "*.vspscc", MyDeleteFile);
	EnumFiles(szSubPath, p, "*.vssscc", MyDeleteFile);

	EnumFiles(szSubPath, p, "*.csproj", FixProjectFile);
	EnumFiles(szSubPath, p, "*.vbproj", FixProjectFile);
	EnumFiles(szSubPath, p, "*.vcxproj", FixProjectFile);
	EnumFiles(szSubPath, p, "*.wixproj", FixProjectFile);
	EnumFiles(szSubPath, p, "*.modelproj", FixProjectFile);
	EnumFiles(szSubPath, p, "*.vdproj", FixProjectFile);

	EnumFiles(szSubPath, p, "*.sln", FixSolutionFile);

	EnumDirs(szSubPath, p, "*", DelinkTFS);


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
