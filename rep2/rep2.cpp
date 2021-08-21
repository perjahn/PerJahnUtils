#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#ifdef WIN32
#define filemoderead "rb"
#define filemodewrite "wb"
#else
#define filemoderead "r"
#define filemodewrite "w"
#endif

int replacefile(char *filename, char *find, char *replace);
bool replacebuf(char *buf, char *find, char *replace, int insize, int findsize, int replacesize, int *outsize);

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
	if (!(fh = fopen(filename, filemoderead)))
	{
		printf("Couldn't open file for reading: '%s'\n", filename);
		return 1;
	}

	fseek(fh, 0, SEEK_END);
	int insize = ftell(fh);
	int findsize = (int)strlen(find);
	int replacesize = (int)strlen(replace);
	int bufsize = (int)((long)insize * (long)replacesize / findsize);

	printf("bufsize: %d\n", bufsize);

	char *buf = (char *)malloc(bufsize);
	if (!buf)
	{
		fclose(fh);
		printf("Out of memory: %d bytes.\n", bufsize);
		return 1;
	}

	fseek(fh, 0, SEEK_SET);
	fread(buf, insize, 1, fh);
	fclose(fh);

	int outsize;
	bool modified = replacebuf(buf, find, replace, insize, findsize, replacesize, &outsize);

	if (modified)
	{
		if (!(fh = fopen(filename, filemodewrite)))
		{
			free(buf);
			printf("Couldn't open file for writing: '%s'\n", filename);
			return 1;
		}

		fwrite(buf, outsize, 1, fh);
		fclose(fh);
	}

	free(buf);

	return 0;
}

bool replacebuf(char *buf, char *find, char *replace, int insize, int findsize, int replacesize, int *outsize)
{
	printf("insize: %d, findsize: %d, replacesize: %d\n", insize, findsize, replacesize);

	bool modified = false;
	char *p;
	*outsize = insize;
	for (p = buf; p < buf + *outsize - findsize; p++)
	{
		if (!memcmp(p, find, findsize))
		{
			modified = true;
			printf("Replacing at %d\n", (int)(p - buf));
			if (findsize != replacesize)
			{
				memmove(p + replacesize, p + findsize, replacesize);
				*outsize = *outsize + replacesize - findsize;
			}
			memcpy(p, replace, replacesize);
			p += replacesize;
		}
	}

	printf("outsize: %d\n", *outsize);

	return modified;
}
