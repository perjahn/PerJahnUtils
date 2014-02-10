//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <stdlib.h>

//**********************************************************

struct file
{
	char szFullPath[MAX_PATH];
	WIN32_FIND_DATA Data;
	bool diff;
} g_filedata[1000000];

char **g_excludepatterns;
unsigned g_excludepatterncount;

unsigned g_filecount;
unsigned g_totalexcluded;
unsigned long long g_totalsize;
unsigned g_diffcount;

unsigned char *g_buf1;
unsigned char *g_buf2;

WORD colorred = FOREGROUND_RED|FOREGROUND_INTENSITY;
WORD coloryellow = FOREGROUND_RED|FOREGROUND_GREEN|FOREGROUND_INTENSITY;
WORD colorgray = FOREGROUND_RED|FOREGROUND_GREEN|FOREGROUND_BLUE;
WORD colorwhite = FOREGROUND_RED|FOREGROUND_GREEN|FOREGROUND_BLUE|FOREGROUND_INTENSITY;
HANDLE hStdout = GetStdHandle(STD_OUTPUT_HANDLE);


void gather(char *szPath);
int compare(const void *arg1, const void *arg2);
bool analyze(void);
void compare_entries(int i, int j);
bool compare_files(char *szFileName1, char *szFileName2);

//**********************************************************

int main(int argc, char *argv[])
{
	if(argc<2)
	{
		printf(
			"difffind 3.0 - Program for finding mismatched files.\n"
			"               Useful when an exact copy of a file must exist in several locations.\n"
			"\n"
			"Usage: difffind <pattern> [-pattern1 -pattern2 ...]\n"
			"\n"
			" -patten:  Exclude files and folders.\n"
			"\n"
			"Example: difffind *.dll -Deploy -*.resources.dll\n"
			"\n"
			"Return value: 1 if any mismatch found, else 0. 2 if error.\n");
		return 0;
	}


	char *pszPattern = NULL;

	pszPattern = argv[1];

	if(argc<3)
	{
		g_excludepatterns = NULL;
		g_excludepatterncount = 0;
	}
	else
	{
		g_excludepatterns = argv+2;
		g_excludepatterncount = argc-2;
	}


	g_filecount = 0;
	g_totalsize = 0;
	g_totalexcluded = 0;

	DWORD t1 = GetTickCount();

	gather(pszPattern);

	qsort(g_filedata, g_filecount, sizeof(file), compare);

	bool result = analyze();

	DWORD t2 = GetTickCount();

	if(t1 == t2)
	{
		printf("Read: %llu MB.\n", g_totalsize/1024/1024);
	}
	else
	{
		// b/ms -> kb/s:   /1024*1000
		DWORD t = t2-t1;
		unsigned long long speed = g_totalsize/(t2-t1)*10000/1024/1024;
		printf("Read: %llu MB. (%llu.%llu MB/s).\n",
			g_totalsize/1024/1024,
			speed/10, speed%10);
	}

	if(result)
	{
		return 1;
	}

	return 0;
}

//**********************************************************

void gather(char *szPath)
{
	char szSubPath[1000], *p, *pPattern;
	WIN32_FIND_DATA FindDir, *FindFile, FindExclude;
	HANDLE hFind;

	char **excluded = new char*[100000];
	unsigned excludedcount = 0;


	for(pPattern=szPath+strlen(szPath); pPattern>szPath && *(pPattern-1)!='\\' && *(pPattern-1)!=':'; pPattern--)
		;


	strcpy(szSubPath, szPath);
	for(p=szSubPath+strlen(szSubPath); p>szSubPath && *(p-1)!='\\' && *(p-1)!=':'; p--)
		;


	// Create exclude array
	for(unsigned patt=0; patt<g_excludepatterncount; patt++)
	{
		sprintf(p, "%s", g_excludepatterns[patt]+1);

		if((hFind=FindFirstFile(szSubPath, &FindExclude)) != INVALID_HANDLE_VALUE)
		{
			do
			{
				if(FindExclude.cFileName && strcmp(FindExclude.cFileName, ".") && strcmp(FindExclude.cFileName, ".."))
				{
					excluded[excludedcount] = new char[strlen(FindExclude.cFileName)+1];

					strcpy(excluded[excludedcount], FindExclude.cFileName);
					excludedcount++;
					g_totalexcluded++;
				}
			}
			while(FindNextFile(hFind, &FindExclude));

			FindClose(hFind);
		}
	}


	// Recurse folders
	sprintf(p, "*");

	if((hFind=FindFirstFile(szSubPath, &FindDir)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if(FindDir.cFileName && strcmp(FindDir.cFileName, ".") && strcmp(FindDir.cFileName, ".."))
			{
				if(FindDir.dwFileAttributes&FILE_ATTRIBUTE_DIRECTORY)
				{
					bool foundex = false;
					for(unsigned ex=0; ex<excludedcount; ex++)
					{
						if(!strcmp(excluded[ex], FindDir.cFileName))
						{
							foundex = true;
							break;
						}
					}
					if(!foundex)
					{
						sprintf(p, "%s\\%s", FindDir.cFileName, pPattern);
						gather(szSubPath);
					}
				}
			}
		}
		while(FindNextFile(hFind, &FindDir));

		FindClose(hFind);
	}


	// Gather files
	sprintf(p, "%s", pPattern);

	FindFile = &(g_filedata[g_filecount].Data);
	if((hFind=FindFirstFile(szSubPath, FindFile)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if(FindFile->cFileName && strcmp(FindFile->cFileName, ".") && strcmp(FindFile->cFileName, ".."))
			{
				if(!(FindFile->dwFileAttributes&FILE_ATTRIBUTE_DIRECTORY))
				{
					bool foundex = false;
					for(unsigned ex=0; ex<excludedcount; ex++)
					{
						if(!strcmp(excluded[ex], FindFile->cFileName))
						{
							foundex = true;
							break;
						}
					}
					if(!foundex)
					{
						int size = pPattern-szPath;
						memcpy(g_filedata[g_filecount].szFullPath, szPath, size);
						sprintf(g_filedata[g_filecount].szFullPath+size, "%s", FindFile->cFileName);
						g_filecount++;
					}
				}
			}

			FindFile = &(g_filedata[g_filecount].Data);
		}
		while(FindNextFile(hFind, FindFile));

		FindClose(hFind);
	}


	for(unsigned i=0; i<excludedcount; i++)
	{
		delete[] excluded[i];
	}

	delete[] excluded;


	return;
}

//**********************************************************

int compare(const void *arg1, const void *arg2)
{
	//return _stricmp(*(char**)arg1, *(char**)arg2);
	file *f1 = (file*)arg1;
	file *f2 = (file*)arg2;

	int diff = strcmp(f1->Data.cFileName, f2->Data.cFileName);
	if(diff)
		return diff;

	return strcmp(f1->szFullPath, f2->szFullPath);
}

//**********************************************************
// Return: true=found diff, false=everything identical

bool analyze(void)
{
	unsigned maxsize = 0;

	for(unsigned i=0; i<g_filecount; i++)
	{
		g_filedata[i].diff = false;

		if(g_filedata[i].Data.nFileSizeLow>maxsize)
		{
			maxsize = g_filedata[i].Data.nFileSizeLow;
		}
	}

	g_buf1 = new unsigned char[maxsize];
	g_buf2 = new unsigned char[maxsize];

	g_diffcount = 0;

	for(unsigned i=0; i<g_filecount; i++)
	{
		for(unsigned j=i; j<g_filecount; j++)
		{
			compare_entries(i, j);
		}
	}


	delete[] g_buf1;
	delete[] g_buf2;


	unsigned difffiles = 0;
	for(unsigned i=0; i<g_filecount; i++)
	{
		if(g_filedata[i].diff)
			difffiles++;
	}

	SetConsoleTextAttribute(hStdout, coloryellow);
	printf("Diffs: %u. Diff files: %u. Excluded entries: %u. Total files: %u.\n",
		g_diffcount, difffiles, g_totalexcluded, g_filecount);

	SetConsoleTextAttribute(hStdout, colorgray);

	if(g_diffcount>0)
		return true;

	return false;
}

//**********************************************************
// Compare two entries in g_filedata array

void compare_entries(int i, int j)
{
	static char *pLast = "";

	if(i==j)
	{
		return;
	}

	file *f1, *f2;
	f1 = &g_filedata[i];
	f2 = &g_filedata[j];

	if(strcmp(f1->Data.cFileName, f2->Data.cFileName))
	{
		return;
	}

	long long size1, size2;
	size1 = (((long long)(f1->Data.nFileSizeHigh))<<32)+f1->Data.nFileSizeLow;
	size2 = (((long long)(f2->Data.nFileSizeHigh))<<32)+f2->Data.nFileSizeLow;

	if(size1 == size2)
	{
		if(!compare_files(f1->szFullPath, f2->szFullPath))
		{
			return;
		}
	}


	f1->diff = true;
	f2->diff = true;

	if(strcmp(f1->Data.cFileName, pLast))
	{
		SetConsoleTextAttribute(hStdout, colorgray);
		printf("'");
		SetConsoleTextAttribute(hStdout, colorred);
		printf("%s", f1->Data.cFileName);
		SetConsoleTextAttribute(hStdout, colorgray);
		printf("':\n");

		pLast = f1->Data.cFileName;
	}


	char szDir1[1000], szDir2[1000], *p;

	strcpy(szDir1, f1->szFullPath);
	for(p=szDir1+strlen(szDir1); p>szDir1 && *(p-1)!='\\' && *(p-1)!=':'; p--)
		;
	if(p>szDir1 && *(p-1)=='\\')
		p--;
	*p = 0;

	strcpy(szDir2, f2->szFullPath);
	for(p=szDir2+strlen(szDir2); p>szDir2 && *(p-1)!='\\' && *(p-1)!=':'; p--)
		;
	if(p>szDir2 && *(p-1)=='\\')
		p--;
	*p = 0;

	SetConsoleTextAttribute(hStdout, colorgray);
	printf("  '");
	SetConsoleTextAttribute(hStdout, colorwhite);
	printf("%s", szDir1);
	SetConsoleTextAttribute(hStdout, colorgray);
	printf("\\%s' '", f1->Data.cFileName);
	SetConsoleTextAttribute(hStdout, colorwhite);
	printf("%s", szDir2);
	SetConsoleTextAttribute(hStdout, colorgray);
	printf("\\%s': Size1: %llu, Size2: %llu\n", f2->Data.cFileName,
		size1,
		size2);

	g_diffcount++;

	return;
}

//**********************************************************
// Return: true=diff, false=identical

bool compare_files(char *szFileName1, char *szFileName2)
{
	FILE *fh1, *fh2;

	if(!(fh1 = fopen(szFileName1, "rb")))
	{
		printf("Couldn't open file: '%s'\n", szFileName1);
		return true;
	}

	if(!(fh2 = fopen(szFileName2, "rb")))
	{
		fclose(fh1);
		printf("Couldn't open file: '%s'\n", szFileName2);
		return true;
	}

	fseek(fh1, 0, SEEK_END);
	unsigned size = ftell(fh1);

	g_totalsize += size;

	fseek(fh1, 0, SEEK_SET);

	fread(g_buf1, size, 1, fh1);
	fread(g_buf2, size, 1, fh2);

	fclose(fh1);
	fclose(fh2);

	int result = memcmp(g_buf1, g_buf2, size);

	return result?true:false;
}

//**********************************************************
