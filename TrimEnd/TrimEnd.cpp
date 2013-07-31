//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <string.h>

//**********************************************************

unsigned char *_buf;
unsigned _bufsize;

unsigned char _find[1000];
unsigned _findsize;

bool g_escape_hex;  // Should escape sequences (hex) in search input be converted?
bool g_repeattrim;
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
unsigned Expand(char *in, unsigned char *out);
unsigned ParseBuf(unsigned char *buf, unsigned bufsize);
void WriteOutfile(char *szFileName, unsigned char *buf, unsigned bufsize);

//**********************************************************

void main(int argc, char *argv[])
{
	char *szUsage =
		"TrimEnd 0.03 gamma\n"
		"\n"
		"Usage: rep [-h] [-r] [-s] [-v] <searchtext> <filepattern>\n"
		"\n"
		"-h: Escape ascii values, hex (\\hh).\n"
		"    \\0A -> line feed.\n"
		"    \\0D -> carriage return.\n"
		"    \\00 -> null.\n"
		"-r: Repeat trim.\n"
		"-s: Recurse subdirectories.\n"
		"-v: Verbose logging.\n";
	char *p1, *p2;

	g_escape_hex = g_repeattrim = g_recurse = g_verbose = false;
	p1 = p2 = NULL;

	int arg;
	for(arg=1; arg<argc; arg++)
	{
		if(!strcmp(argv[arg], "-h"))
		{
			g_escape_hex = true;
		}
		else if(!strcmp(argv[arg], "-r"))
		{
			g_repeattrim = true;
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

	if(arg==argc-2)
	{
		p1 = argv[argc-2];
		p2 = argv[argc-1];
	}

	if(g_verbose)
	{
		printf("%d %d %d %d\n",
			g_escape_hex,
			g_repeattrim,
			g_recurse,
			g_verbose);
	}

	if(p1 && p2)
	{
#ifdef _DEBUG
		RemoveEvilVsJunk(p1);
#endif

		if(g_escape_hex)
		{
			_findsize = Expand(p1, _find);
		}
		else
		{
			_findsize = strlen(p1);
			strcpy((char*)_find, p1);
		}

		ProcessDir(p2);
	}
	else
	{
		printf(szUsage);
	}


	return;
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
}

//**********************************************************

void ProcessFile(char *szFileName)
{
	// Read from file (and allocate buffers)
	if(!ReadInfile(szFileName))
	{
		return;
	}

	unsigned outsize = ParseBuf(_buf, _bufsize);

	// Write to file
	if(_bufsize != outsize)
	{
		if(g_verbose)
		{
			printf("%s: Trimmed %d bytes.\n", szFileName, _bufsize-outsize);
		}
		else
		{
			printf("%s\n", szFileName);
		}

		DWORD dwAttributes = GetFileAttributes(szFileName);
		if(dwAttributes&FILE_ATTRIBUTE_READONLY)
		{
			SetFileAttributes(szFileName, dwAttributes&~FILE_ATTRIBUTE_READONLY);
		}

		WriteOutfile(szFileName, _buf, outsize);
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
	_bufsize = ftell(fh);
	fseek(fh, 0, SEEK_SET);

	if(!AllocateBuffers())
	{
		fclose(fh);
		return false;
	}

	fread(_buf, _bufsize, 1, fh);

	fclose(fh);

	return true;
}

//**********************************************************

bool AllocateBuffers(void)
{
	_buf = new unsigned char[_bufsize];
	if(!_buf)
	{
		printf("Out of memory (%u bytes).\n", _bufsize);
		return false;
	}

	return true;
}

//**********************************************************

void DeAllocateBuffers(void)
{
	delete[] _buf;
}

//**********************************************************

unsigned Expand(char *in, unsigned char *out)
{
	char *p1;
	unsigned char *p2;
	unsigned size;
	unsigned char buf[3];

	buf[2] = 0;

	for(p1=in,p2=out; *p1; p1++,p2++)
	{
		if(*p1=='\\' && isxdigit(*(p1+1)) && isxdigit(*(p1+2)))
		{
			buf[0] = *(p1+1);
			buf[1] = *(p1+2);
			*p2 = (unsigned char)strtol((char *)buf, NULL, 16);
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

unsigned ParseBuf(unsigned char *buf, unsigned bufsize)
{
	if(bufsize<_findsize)
	{
		return bufsize;
	}

	unsigned char *p=buf+bufsize;

	do
	{
		if(memcmp(p-_findsize, _find, _findsize))
		{
			break;
		}

		p-=_findsize;
	}
	while(p>=buf+_findsize && g_repeattrim);

	return p-buf;
}

//**********************************************************

void WriteOutfile(char *szFileName, unsigned char *buf, unsigned bufsize)
{
	FILE *fh;

	if(!(fh = fopen(szFileName, "wb")))
	{
		printf("Couldn't open outfile (%s).\n", szFileName);
		return;
	}

	if(bufsize)
	{
		fwrite(buf, bufsize, 1, fh);
	}

	fclose(fh);

	return;
}

//**********************************************************
