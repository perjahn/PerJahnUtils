//**********************************************************

#include <ctype.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

//**********************************************************

#ifdef _DEBUG
void RemoveEvilVsJunk(char* szText);
#endif

size_t Expand(char* in, unsigned char* out);
void ProcessFile(char* szInFile, char* szOutFile,
	char unsigned* find1, size_t findsize1,
	char unsigned* find2, size_t findsize2);

//**********************************************************

unsigned char find1[10000], find2[10000];

//**********************************************************

int main(int argc, char* argv[])
{
	char* usage =
		(char*)"cutstring 1.3\n"
		"\n"
		"Usage: cutstring [-h] <infile> <outfile> <start search> <end search>\n"
		"\n"
		"-h: Escape ascii values, hex (\\hh).\n"
		"    \\0A -> line feed.\n"
		"    \\0D -> carriage return.\n"
		"    \\00 -> null.\n";

	bool escape_hex = false;

	int arg;
	for (arg = 1; arg < argc; arg++)
	{
		if (!strcmp(argv[arg], "-h"))
		{
			escape_hex = true;
		}
		else
		{
			break;
		}
	}

	char* p1, * p2, * p3, * p4;
	p1 = p2 = p3 = p4 = NULL;

	if (arg == argc - 4)
	{
		p1 = argv[argc - 4];
		p2 = argv[argc - 3];
		p3 = argv[argc - 2];
		p4 = argv[argc - 1];
	}

	if (p1 && p2 && p3 && p4)
	{
#ifdef _DEBUG
		RemoveEvilVsJunk(p3);
		RemoveEvilVsJunk(p4);
#endif

		size_t findsize1, findsize2;


		if (escape_hex)
		{
			findsize1 = Expand(p3, find1);
			findsize2 = Expand(p4, find2);
		}
		else
		{
			findsize1 = strlen(p3);
			strcpy((char*)find1, p3);
			findsize2 = strlen(p4);
			strcpy((char*)find2, p4);
		}

		ProcessFile(p1, p2, find1, findsize1, find2, findsize2);
	}
	else
	{
		printf(usage);
	}

	return 0;
}

//**********************************************************
// Remove evil Visual Studio junk inserted into debug params

#ifdef _DEBUG
void RemoveEvilVsJunk(char* text)
{
	char* junk = (char*)" xmlns=http://schemas.microsoft.com/developer/msbuild/2003";
	size_t textsize = strlen(text);
	size_t junksize = strlen(junk);

	char* p1, * p2;
	p1 = p2 = text;
	while (*p1)
	{
		if (p1 <= text + textsize - junksize && !memcmp(p1, junk, junksize))
		{
			p1 += junksize;
		}
		else
		{
			*p2 = *p1;
			p1++;
			p2++;
		}
	}
	*p2 = 0;
}
#endif

//**********************************************************

size_t Expand(char* in, unsigned char* out)
{
	char* p1;
	unsigned char* p2;
	size_t size;
	unsigned char buf[3];

	buf[2] = 0;

	for (p1 = in, p2 = out; *p1; p1++, p2++)
	{
		if (*p1 == '\\' && isxdigit(*(p1 + 1)) && isxdigit(*(p1 + 2)))
		{
			buf[0] = *(p1 + 1);
			buf[1] = *(p1 + 2);
			*p2 = (unsigned char)strtol((char*)buf, NULL, 16);
			p1 += 2;
		}
		else
		{
			*p2 = *p1;
		}
	}

	size = p2 - out;

	return size;
}

//**********************************************************

void ProcessFile(char* szInFile, char* szOutFile,
	char unsigned* find1, size_t findsize1,
	char unsigned* find2, size_t findsize2)
{
	FILE* fh;

	if (!(fh = fopen(szInFile, "rb")))
	{
		printf("Couldn't open infile (%s).\n", szInFile);
		return;
	}

	fseek(fh, 0, SEEK_END);
	int size = ftell(fh);

	unsigned char* buf = new unsigned char[size];
	if (!buf)
	{
		printf("Out of memory (%u bytes).\n", size);
		return;
	}

	fseek(fh, 0, SEEK_SET);
	fread(buf, size, 1, fh);
	fclose(fh);

	unsigned char* p1, * p2;
	int i = 0;
	p1 = p2 = NULL;

	for (unsigned char* p = buf; p < buf + size; p++)
	{
		if (p1 == NULL && p <= buf + size - findsize1)
		{
			if (!memcmp(p, find1, findsize1))
			{
				p1 = p;
			}
		}

		if (p1 != NULL && p2 == NULL && p <= buf + size - findsize2)
		{
			if (!memcmp(p, find2, findsize2))
			{
				p2 = p;
			}
		}

		if (p1 && p2)
		{
			char filename[1000];
			if (i == 0)
			{
				sprintf(filename, "%s", szOutFile);
			}
			else
			{
				sprintf(filename, "%s_%d", szOutFile, i);
			}

			if (!(fh = fopen(filename, "wb")))
			{
				delete[] buf;
				printf("Couldn't open outfile (%s).\n", filename);
				return;
			}

			fwrite(p1 + findsize1, p2 - p1 - findsize1, 1, fh);
			fclose(fh);

			printf("'%s': Wrote %lld bytes: %lld to %lld\n", filename, p2 - p1 - findsize1, p1 - buf + findsize1, p2 - buf);

			i++;
			p1 = p2 = NULL;
		}
	}

	delete[] buf;

	return;
}

//**********************************************************
