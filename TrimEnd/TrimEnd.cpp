//**********************************************************

#include <windows.h>
#include <malloc.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

//**********************************************************

void ProcessDir(char* path);
void ProcessFile(char* filename);
size_t TrimBuf(unsigned char* buf, unsigned bufsize);

//**********************************************************

int main(int argc, char* argv[])
{
	char* usage =
		(char*)"TrimEnd 0.001 gamma\n"
		"\n"
		"Usage: trimend <path>\n";

	if (argc != 2)
	{
		printf("%s", usage);
		return 1;
	}

	ProcessDir(argv[1]);

	return 0;
}

//**********************************************************

void ProcessDir(char* path)
{
	HANDLE find;
	WIN32_FIND_DATA data;
	char subpath[1000];

	sprintf(subpath, "%s\\*", path);

	if ((find = FindFirstFile(subpath, &data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			sprintf(subpath, "%s\\%s", path, data.cFileName);

			if (data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
			{
				ProcessDir(subpath);
			}
			else
			{
				sprintf(subpath, "%s\\%s", path, data.cFileName);
				ProcessFile(subpath);
			}

		} while (FindNextFile(find, &data));
	}
}

//**********************************************************

void ProcessFile(char* filename)
{
	FILE* fh;

	if (!(fh = fopen(filename, "rb")))
	{
		printf("Couldn't open infile: '%s'\n", filename);
		return;
	}

	fseek(fh, 0, SEEK_END);
	size_t bufsize = ftell(fh);
	fseek(fh, 0, SEEK_SET);

	unsigned char* buf = (unsigned char*)malloc(bufsize);
	if (!buf)
	{
		fclose(fh);
		printf("Out of memory: %lld bytes.\n", bufsize);
		return;
	}

	fread(buf, bufsize, 1, fh);
	fclose(fh);

	size_t outsize = TrimBuf(buf, bufsize);

	if (bufsize != outsize)
	{
		printf("%s\n", filename);
		if ((fh = fopen(filename, "wb")))
		{
			fwrite(buf, bufsize, 1, fh);
			fclose(fh);
		}
	}

	free(buf);
}

//**********************************************************

size_t TrimBuf(unsigned char* buf, unsigned bufsize)
{
	unsigned char* p, * p2, * start;

	for (p = start = buf; p < buf + bufsize; p++)
	{
		if (*p == '\r' || *p == '\n')
		{
			for (p2 = p; p > start && (*(p2 - 1) == ' ' || *(p2 - 1) == '\t'); p2--)
			{
				memmove(p2, p, buf + bufsize - p);
			}

			start = p + 1;
		}
	}

	return p - buf;
}

//**********************************************************
