#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#if _WIN32
#include <windows.h>
#else
#include <unistd.h>
#endif

int replacefile(char *filename, char *find, char *replace);
int replacebuf(char *buf, char *find, char *replace, int insize, int findsize, int replacesize);

int main(int argc, char *argv[])
{
	if (argc != 4)
	{
		printf("Usage: replace <find> <replace> <filename>\n");
		return 1;
	}

	int result = replacefile(argv[3], argv[1], argv[2]);

	return result;
}

int replacefile(char *filename, char *find, char *replace)
{
	FILE *fh;
	if (!(fh = fopen(filename, "r")))
	{
		printf("Couldn't open file for reading: '%s'\n", filename);
		return 1;
	}

	fseek(fh, 0, SEEK_END);
	int insize = ftell(fh);
	int findsize = strlen(find);
	int replacesize = strlen(replace);
	int bufsize = (int)((long)insize*(long)replacesize / findsize);

	printf("bufsize: %d\n", bufsize);

	char *buf = (char*)malloc(bufsize);
	if (!buf)
	{
		fclose(fh);
		printf("Out of memory: %d bytes.\n", bufsize);
		return 1;
	}

	fseek(fh, 0, SEEK_SET);
	fread(buf, insize, 1, fh);
	fclose(fh);

	int outsize = replacebuf(buf, find, replace, insize, findsize, replacesize);

	if (!(fh = fopen(filename, "w")))
	{
		free(buf);
		printf("Couldn't open file for writing: '%s'\n", filename);
		return 1;
	}

	fwrite(buf, outsize, 1, fh);

	free(buf);

	fclose(fh);

#if _WIN32
	HANDLE handle = CreateFile(filename, GENERIC_WRITE, 0, NULL, 0, 0, NULL);
	LARGE_INTEGER outsizeLarge;
	outsizeLarge.QuadPart = outsize;
	SetFilePointerEx(handle, outsizeLarge, NULL, FILE_BEGIN);
	SetEndOfFile(handle);
	printf("%d\n", outsizeLarge);
	CloseHandle(handle);
#else
	truncate(filename, outsize);
#endif

	return 0;
}

int replacebuf(char *buf, char *find, char *replace, int insize, int findsize, int replacesize)
{
	printf("insize: %d, findsize: %d, replacesize: %d\n", insize, findsize, replacesize);

	char *p;
	int outsize = insize;
	for (p = buf; p < buf + outsize - findsize; p++)
	{
		if (!memcmp(p, find, findsize))
		{
			printf("Replacing at %d\n", (int)(p - buf));
			if (findsize != replacesize)
			{
				memmove(p + replacesize, p + findsize, replacesize);
				outsize = outsize + replacesize - findsize;
			}
			memcpy(p, replace, replacesize);
			p += replacesize;
		}
	}

	printf("outsize: %d\n", outsize);

	return outsize;
}
