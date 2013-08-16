//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

//**********************************************************

unsigned char *_inbuf, *_outbuf;
unsigned _inbufsize, _outbufsize;

unsigned char _find[1000], _replace[1000];
unsigned _findsize, _replacesize;
char _szRename[1000];

bool g_renamefiles;
bool g_escapehex;  // Should escape sequences (hex) in search input be converted?
bool g_recurse;
bool g_verbose;  // Verbose logging

//**********************************************************

#ifdef _DEBUG
void RemoveEvilVsJunk(char *szText);
#endif
void ProcessDir(char *szFilePattern);
void ProcessFile(char *szFileName);
bool ReadInfile(char *szFileName);
bool AllocateBuffers(void);
void DeAllocateBuffers(void);
unsigned ConvertHexToBytes(char *in, unsigned char *out);
void ParseBuf(unsigned char *inbuf, unsigned char *outbuf, unsigned inbufsize, unsigned outbufsize, unsigned *outsize, bool *modified);
void WriteOutfile(char *szFileName, unsigned char *outbuf, unsigned outsize);

//**********************************************************

int main(int argc, char *argv[])
{
	char *szUsage =
		"rep 2.7\n"
		"\n"
		"Usage: rep [-f] [-h] [-s] [-v] <searchtext> <replacetext> <filepattern>\n"
		"\n"
		"-f: Also rename files and directories.\n"
		"-h: Escape ascii values, hex (\\hh).\n"
		"    \\0A -> line feed.\n"
		"    \\0D -> carriage return.\n"
		"    \\00 -> null.\n"
		"-s: Recurse subdirectories.\n"
		"-v: Verbose logging.\n";
	char *p1, *p2, *p3;

	g_renamefiles = g_escapehex = g_recurse = g_verbose= false;
	p1 = p2 = p3 = NULL;

	int arg;
	for(arg=1; arg<argc; arg++)
	{
		if(!strcmp(argv[arg], "-f"))
		{
			g_renamefiles = true;
		}
		else if(!strcmp(argv[arg], "-h"))
		{
			g_escapehex = true;
		}
		else if(!strcmp(argv[arg], "-s"))
		{
			g_recurse = true;
		}
		else if(!strcmp(argv[arg], "-v"))
		{
			g_verbose = true;
		}
		else
		{
			break;
		}
	}

	if(g_verbose)
	{
		printf("%d %d %d %d\n",
			g_renamefiles,
			g_escapehex,
			g_recurse,
			g_verbose);
	}

	if(arg!=argc-3)
	{
		printf(szUsage);
	}

	p1 = argv[argc-3];
	p2 = argv[argc-2];
	p3 = argv[argc-1];

	if(p1 && p2 && p3)
	{
#ifdef _DEBUG
		RemoveEvilVsJunk(p1);
		RemoveEvilVsJunk(p2);
#endif

		unsigned len = strlen(p1);
		if(len>=1000)
		{
			printf("Error: searchtext too big (%u chars).", len);
			return 1;
		}
		len = strlen(p2);
		if(len>=1000)
		{
			printf("Error: replacetext too big (%u chars).", len);
			return 1;
		}

		if(g_escapehex)
		{
			_findsize = ConvertHexToBytes(p1, _find);
			_replacesize = ConvertHexToBytes(p2, _replace);
		}
		else
		{
			_findsize = strlen(p1);
			strcpy((char*)_find, p1);
			_replacesize = strlen(p2);
			strcpy((char*)_replace, p2);
		}

		ProcessDir(p3);
	}


	return 0;
}

//**********************************************************
// Remove evil Visual Studio junk inserted into debug params

#ifdef _DEBUG
void RemoveEvilVsJunk(char *text)
{
	char *junk = " xmlns=http://schemas.microsoft.com/developer/msbuild/2003";
	int textsize = strlen(text);
	int junksize = strlen(junk);

	char *p1, *p2;
	p1 = p2 = text;
	while(*p1)
	{
		if(p1<=text+textsize-junksize && !memcmp(p1, junk, junksize))
		{
			p1+=junksize;
		}
		else
		{
			*p2 = *p1;
			p1++;
			p2++;
		}
	}
	*p2=0;
}
#endif

//**********************************************************

void ProcessDir(char *szFilePattern)
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;
	char szSubPath[1000], *pPattern, *p;

	for(pPattern=szFilePattern+strlen(szFilePattern); pPattern>szFilePattern && *(pPattern-1)!='\\' && *(pPattern-1)!=':'; pPattern--)
		;

	strcpy(szSubPath, szFilePattern);
	for(p=szSubPath+strlen(szSubPath); p>szSubPath && *(p-1)!='\\' && *(p-1)!=':'; p--)
		;


	if((hFind=FindFirstFile(szFilePattern, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if(!(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
				{
					// File
					sprintf(p, "%s", Data.cFileName);
					ProcessFile(szSubPath);
				}
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	sprintf(p, "*");

	if((hFind=FindFirstFile(szSubPath, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				{
					// Dir
					if(g_recurse)
					{
						sprintf(p, "%s\\%s", Data.cFileName, pPattern);
						ProcessDir(szSubPath);
					}
				}
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	if(!g_renamefiles)
	{
		return;
	}

	sprintf(p, "*");
	memcpy(p+1, _find, _findsize);
	p[_findsize+1] = 0;  // Null terminate string
	strcat(p, "*");

	if(g_verbose)
	{
		printf("Searching for renaming '%s'\n", szSubPath);
	}


	if((hFind=FindFirstFile(szSubPath, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				unsigned outsize;
				bool modified;
				sprintf(p, "%s", Data.cFileName);
				strcpy(_szRename, szSubPath);
				ParseBuf((unsigned char *)p, (unsigned char *)(_szRename+(p-szSubPath)), strlen(Data.cFileName), 999, &outsize, &modified);
				_szRename[outsize+(p-szSubPath)] = 0;
				printf("Renaming: '%s' -> '%s'\n", szSubPath, _szRename);
				rename(szSubPath, _szRename);
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}
}

//**********************************************************

void ProcessFile(char *szFileName)
{
	// Read from file (and allocate buffers)
	if(!ReadInfile(szFileName))
	{
		return;
	}

	bool modified = false;
	unsigned outsize;

	ParseBuf(_inbuf, _outbuf, _inbufsize, _outbufsize, &outsize, &modified);

	// Write to file
	if(modified)
	{
		printf("%s\n", szFileName);

		DWORD dwAttributes = GetFileAttributes(szFileName);
		if(dwAttributes&FILE_ATTRIBUTE_READONLY)
		{
			SetFileAttributes(szFileName, dwAttributes&~FILE_ATTRIBUTE_READONLY);
		}

		WriteOutfile(szFileName, _outbuf, outsize);
	}

	DeAllocateBuffers();
}

//**********************************************************

bool ReadInfile(char *szFileName)
{
	FILE *fh;

	if(!(fh = fopen(szFileName, "rb")))
	{
		printf("Couldn't open infile (%s).\n", szFileName);
		return false;
	}

	fseek(fh, 0, SEEK_END);
	_inbufsize = ftell(fh);
	_outbufsize = _inbufsize*2+1000;
	fseek(fh, 0, SEEK_SET);

	if(!AllocateBuffers())
	{
		fclose(fh);
		return false;
	}

	fread(_inbuf, _inbufsize, 1, fh);

	fclose(fh);

	return true;
}

//**********************************************************

bool AllocateBuffers(void)
{
	_inbuf = new unsigned char[_inbufsize];
	if(!_inbuf)
	{
		printf("Out of memory (%u bytes).\n", _inbufsize);
		return false;
	}

	_outbuf = new unsigned char[_outbufsize];
	if(!_outbuf)
	{
		printf("Out of memory (%u bytes).\n", _outbufsize);
		delete[] _inbuf;
		return false;
	}

	return true;
}

//**********************************************************

void DeAllocateBuffers(void)
{
	delete[] _inbuf;
	delete[] _outbuf;
}

//**********************************************************

unsigned ConvertHexToBytes(char *in, unsigned char *out)
{
	char *p1;
	unsigned char *p2;
	unsigned size;
	unsigned char hexbuf[3];

	hexbuf[2] = 0;

	for(p1=in,p2=out; *p1; p1++,p2++)
	{
		if(*p1=='\\' && isxdigit(*(p1+1)) && isxdigit(*(p1+2)))
		{
			hexbuf[0] = *(p1+1);
			hexbuf[1] = *(p1+2);
			*p2 = (unsigned char)strtol((char *)hexbuf, NULL, 16);
			p1+=2;
		}
		else
		{
			*p2 = *p1;
		}
	}

	size = (unsigned)(p2-out);

	return size;
}

//**********************************************************

void ParseBuf(unsigned char *inbuf, unsigned char *outbuf, unsigned inbufsize, unsigned outbufsize, unsigned *outsize, bool *modified)
{
	unsigned char *p1, *p2;

	for(p1=inbuf,p2=outbuf; p1<inbuf+inbufsize; )
	{
		if(p1<=inbuf+inbufsize-_findsize && !memcmp(p1, _find, _findsize))
		{
			*modified = true;
			memcpy(p2, _replace, _replacesize);
			p1+=_findsize;
			p2+=_replacesize;
		}
		else
		{
			*p2 = *p1;
			p1++;
			p2++;
		}
	}

	*outsize = (unsigned)(p2-outbuf);

	return;
}

//**********************************************************

void WriteOutfile(char *szFileName, unsigned char *outbuf, unsigned outsize)
{
	FILE *fh;

	if(!(fh = fopen(szFileName, "wb")))
	{
		printf("Couldn't open outfile (%s).\n", szFileName);
		return;
	}

	if(outsize)
	{
		fwrite(outbuf, outsize, 1, fh);
	}

	fclose(fh);

	return;
}

//**********************************************************
