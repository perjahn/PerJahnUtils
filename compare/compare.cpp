//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <string.h>

//**********************************************************

void compare_paths(char *szPath1, char *szPath2);
void compare_files(char *szFileName1, char *szFileName2);
void compare_bufs(unsigned char *buf1, unsigned char *buf2, unsigned bufsize);
void print_stats(void);

WIN32_FIND_DATA g_dir1[100000];
WIN32_FIND_DATA g_dir2[100000];

bool g_verbose;

__int64 g_sum_diffdirs;
__int64 g_sum_difffiles_total;
__int64 g_sum_difffiles_diff;
__int64 g_sum_diffbytes_total;
__int64 g_sum_diffbytes_diff;
DWORD g_t1, g_t2;

//**********************************************************

int main(int argc, char *argv[])
{
	char *usage =
		"Compare 2.0\n"
		"\n"
		"Usage: compare [-v] <file pattern 1> <file pattern 2>\n"
		"\n"
		" -v: Verbose\n";

	if(argc<3 || argc>4)
	{
		printf(usage);
		return 2;
	}

	g_sum_diffdirs = 0;
	g_sum_difffiles_total = 0;
	g_sum_difffiles_diff = 0;
	g_sum_diffbytes_total = 0;
	g_sum_diffbytes_diff = 0;


	if(argc==4)
	{
		if(strcmp(argv[1], "-v"))
		{
			printf(usage);
			return 2;
		}
		else
		{
			g_verbose = true;
		}

		if(strchr(argv[2], '?') || strchr(argv[2], '*') || strchr(argv[3], '?') || strchr(argv[3], '*'))
		{
			g_t1 = GetTickCount();
			compare_files(argv[2], argv[3]);
			g_t2 = GetTickCount();
			g_sum_difffiles_total++;
		}
		else
		{
			g_t1 = GetTickCount();
			compare_paths(argv[2], argv[3]);
			g_t2 = GetTickCount();
		}
	}
	else
	{
		g_verbose = false;

		if(!strchr(argv[1], '?') && !strchr(argv[1], '*') && !strchr(argv[2], '?') && !strchr(argv[2], '*'))
		{
			g_t1 = GetTickCount();
			compare_files(argv[1], argv[2]);
			g_t2 = GetTickCount();
			g_sum_difffiles_total++;
		}
		else
		{
			g_t1 = GetTickCount();
			compare_paths(argv[1], argv[2]);
			g_t2 = GetTickCount();
		}
	}

	print_stats();

	if(g_sum_difffiles_diff>0)
		return 1;

	return 0;
}

//**********************************************************

int compare(const void *arg1, const void *arg2)
{
	WIN32_FIND_DATA *pe1, *pe2;

	pe1 = (WIN32_FIND_DATA*)arg1;
	pe2 = (WIN32_FIND_DATA*)arg2;

	int result = _stricmp(pe1->cFileName, pe2->cFileName);
	if(result)
		return result;

	return strcmp(pe1->cFileName, pe2->cFileName);
}

void compare_paths(char *szPath1, char *szPath2)
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;
	char *p1, *p2;
	int entries1, entries2;
	WIN32_FIND_DATA *entries;

/*
	if(strchr(szPath1, '?') || strchr(szPath1, '*'))
		many1 = true;
	else
		many1 = false;

	if(strchr(szPath2, '?') || strchr(szPath2, '*'))
		many2 = true;
	else
		many2 = false;

	if(!many1 && !many2)
	{
		compare_files(szPath1, szPath2);
		return;
	}
*/

	// Else assume many-many comparison


	// Enumerate entries in path1
	entries1 = 0;
	if((hFind=FindFirstFile(szPath1, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				g_dir1[entries1++] = Data;
			}
		}
		while(FindNextFile(hFind, &Data) && entries1<100000);

		FindClose(hFind);
	}

	// Enumerate entries in path2
	entries2 = 0;
	if((hFind=FindFirstFile(szPath2, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				g_dir2[entries2++] = Data;
			}
		}
		while(FindNextFile(hFind, &Data) && entries2<100000);

		FindClose(hFind);
	}

	char s[1000];
	GetCurrentDirectory(1000, s);

	qsort(g_dir1, entries1, sizeof(WIN32_FIND_DATA), compare);
	qsort(g_dir2, entries2, sizeof(WIN32_FIND_DATA), compare);


	unsigned e=0;
	for(int e1=0; e1<entries1; e1++)
	{
		for(int e2=0; e2<entries2; e2++)
		{
			if(!_stricmp(g_dir1[e1].cFileName, g_dir2[e2].cFileName))
			{
				e++;
			}
		}
	}

	entries = new WIN32_FIND_DATA[e];
	if(!entries)
	{
		printf("Out of memory (%u).\n", e);
		return;
	}

	e=0;
	for(int e1=0; e1<entries1; e1++)
	{
		for(int e2=0; e2<entries2; e2++)
		{
			if(!_stricmp(g_dir1[e1].cFileName, g_dir2[e2].cFileName))
			{
				entries[e] = g_dir1[e1];
				e++;
			}
		}
	}


	// Compare entries
	if(g_verbose)
		printf("Comparing %u common entries between '%s' and '%s'...\n", e, szPath1, szPath2);

	char szFileName1[1000], szFileName2[1000];

	for(unsigned i=0; i<e; i++)
	{
		strcpy(szFileName1, szPath1);
		strcpy(szFileName2, szPath2);

		for(p1=szFileName1+strlen(szFileName1); p1>szFileName1 && *(p1-1)!='\\' && *(p1-1)!=':'; p1--);
		for(p2=szFileName2+strlen(szFileName2); p2>szFileName2 && *(p2-1)!='\\' && *(p2-1)!=':'; p2--);

		strcpy(p1, entries[i].cFileName);
		strcpy(p2, entries[i].cFileName);

		if(entries[i].dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
		{
			// Dir

			g_sum_diffdirs++;

			if(g_verbose)
				printf("Recursing into '%s' and '%s'\n", szFileName1, szFileName2);

			strcat(szFileName1, "\\*");
			strcat(szFileName2, "\\*");

			compare_paths(szFileName1, szFileName2);
		}
		else
		{
			// File

			g_sum_difffiles_total++;

			if(g_verbose)
				printf("Comparing '%s' and '%s'\n", szFileName1, szFileName2);

			compare_files(szFileName1, szFileName2);
		}
	}


	delete[] entries;


	return;
}

//**********************************************************

__int64 g_diffbytes;

void compare_files(char *szFileName1, char *szFileName2)
{
	FILE *fh1, *fh2;
	__int64 l1, l2, l;


	fh1 = fopen(szFileName1, "rb");
	if(!fh1)
	{
		printf("Couldn't open file (%s).\n", szFileName1);
		return;
	}

	fh2 = fopen(szFileName2, "rb");
	if(!fh2)
	{
		fclose(fh1);
		printf("Couldn't open file (%s).\n", szFileName2);
		return;
	}


	_fseeki64(fh1, 0, SEEK_END);
	l1 = _ftelli64(fh1);
	fseek(fh1, 0, SEEK_SET);

	_fseeki64(fh2, 0, SEEK_END);
	l2 = _ftelli64(fh2);
	fseek(fh2, 0, SEEK_SET);

	l = l1<l2? l1: l2;



	unsigned bufsize = 16*1024*1024;
	unsigned char *buf1, *buf2;
	buf1 = new unsigned char[bufsize];
	if(!buf1)
	{
		fclose(fh1);
		fclose(fh2);
		printf("Out of memory (%u bytes).\n", bufsize);
		return;
	}
	buf2 = new unsigned char[bufsize];
	if(!buf2)
	{
		fclose(fh1);
		fclose(fh2);
		delete[] buf1;
		printf("Out of memory (%u bytes).\n", bufsize);
		return;
	}

	printf("Comparing: '%s' to '%s': ", szFileName1, szFileName2);

	g_diffbytes = 0;
	__int64 i;

	DWORD t1 = GetTickCount();
	for(i=0; i<l; i+=bufsize)
	{
		unsigned blocksize = bufsize;
		if(i+bufsize>l)
			blocksize = (unsigned)(l-i);

		fread(buf1, blocksize, 1, fh1);
		fread(buf2, blocksize, 1, fh2);

		compare_bufs(buf1, buf2, blocksize);
	}
	DWORD t2 = GetTickCount();

	delete[] buf1;
	delete[] buf2;

	fclose(fh1);
	fclose(fh2);

	unsigned percent;
	if(l1!=l2)
	{
		if(g_diffbytes && l>0)
		{
			percent = (unsigned)(g_diffbytes*100/l);
			if(percent>0)
				printf("Size diff. Common part: %I64d bytes diff (%u%%).", g_diffbytes, percent);
			else
				printf("Size diff. Common part: %I64d bytes diff (<1%%).", g_diffbytes);
		}
		else
		{
			printf("Size diff. Common part identical.");
		}

		g_sum_difffiles_diff++;
	}
	else
	{
		if(g_diffbytes && l>0)
		{
			percent = (unsigned)(g_diffbytes*100/l);
			if(percent>0)
				printf("Size identical. %I64d bytes diff (%u%%).", g_diffbytes, percent);
			else
				printf("Size identical. %I64d bytes diff (<1%%).", g_diffbytes);

			g_sum_difffiles_diff++;
		}
		else
		{
			printf("Identical.");
		}
	}

	if(t1!=t2)
	{
		double t = (t2-t1)/1000.0;
		double mb = l*2.0/1024/1024;
		printf(" Read %.1f mb/s.\n", mb/t);
	}
	else
	{
		printf("\n");
	}

	g_sum_diffbytes_diff += g_diffbytes;
	g_sum_diffbytes_total += l;

	return;
}

//**********************************************************

void compare_bufs(unsigned char *buf1, unsigned char *buf2, unsigned bufsize)
{
	unsigned char *p1, *p2;


	for(p1=buf1,p2=buf2; p1<buf1+bufsize; p1++,p2++)
	{
		if(*p1!=*p2)
		{
			g_diffbytes++;
		}
	}


	return;
}

//**********************************************************

void print_stats(void)
{
	printf(
		"\n"
		"Sum:\n"
		"Common directories scanned: %6I64d\n"
		"\n"
		"Common files compared:      %6I64d\n"
		"Common files identical:     %6I64d\n"
		"Common files diffed:        %6I64d\n"
		"\n"
		"Bytes compared:       %12I64d\n"
		"Bytes identical:      %12I64d\n"
		"Bytes diffed:         %12I64d\n"
		"\n"
		"Total time: %u s\n",
		g_sum_diffdirs,

		g_sum_difffiles_total,
		g_sum_difffiles_total-g_sum_difffiles_diff,
		g_sum_difffiles_diff,

		g_sum_diffbytes_total,
		g_sum_diffbytes_total-g_sum_diffbytes_diff,
		g_sum_diffbytes_diff,

		(g_t2-g_t1)/1000);


	return;
}

//**********************************************************
