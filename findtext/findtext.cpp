#include <windows.h>
#include <malloc.h>
#include <stdio.h>

#define MAX_TEXTS 1000

void FindInFiles(char* path);
int ParseFile(char* filename);
int ParseBuf(char* filename, unsigned char* buf, long long bufsize);

char* texts[MAX_TEXTS];
int textcount;
long long textsizes[MAX_TEXTS];

long long filesfound = 0;
long long totalhits = 0;

int main(int argc, char* argv[])
{
	if (argc < 3)
	{
		printf("Usage: findtext <path> <texts...>\n");
		return 1;
	}


	if (argc > MAX_TEXTS)
	{
		printf("Too many texts.\n");
		return 1;
	}


	textcount = argc - 2;
	for (int i = 0; i < textcount; i++)
	{
		texts[i] = argv[i + 2];
		textsizes[i] = strlen(texts[i]);
	}


	FindInFiles(argv[1]);


	printf("Files found: %lld\nTotal hits: %lld\n", filesfound, totalhits);

	return 0;
}

void FindInFiles(char* path)
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;
	char subpath[1000];

	if ((hFind = FindFirstFile(path, &Data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if (Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				{
				}
				else
				{
					if (strchr(path, '\\'))
					{
						strcpy(subpath, path);
						char* p = strrchr(subpath, '\\');
						strcpy(p + 1, Data.cFileName);
					}
					else
					{
						strcpy(subpath, Data.cFileName);
					}
					ParseFile(subpath);
				}
			}
		} while (FindNextFile(hFind, &Data));

		FindClose(hFind);
	}

	return;
}

int ParseFile(char* filename)
{
	FILE* fh;

	if (fopen_s(&fh, filename, "rb") || !fh)
	{
		printf("Couldn't open file: '%s'\n", filename);
		return 1;
	}

	if (_fseeki64(fh, 0, SEEK_END))
	{
		printf("Couldn't seek to end: '%s'\n", filename);
		return 2;
	}

	long long filesize = _ftelli64(fh);
	if (filesize == 0)
	{
		fclose(fh);
		return 0;
	}

	unsigned char* buf = (unsigned char*)malloc(filesize);
	if (!buf)
	{
		printf("Out of memory: '%s', %lld\n", filename, filesize);
		return 3;
	}

	if (_fseeki64(fh, 0, SEEK_SET))
	{
		free(buf);
		printf("Couldn't seek to start: '%s'\n", filename);
		return 4;
	}

	size_t readresult = fread(buf, filesize, 1, fh);
	fclose(fh);
	if (readresult != 1)
	{
		free(buf);
		printf("Couldn't read file: %zd\n", readresult);
		return 5;
	}

	ParseBuf(filename, buf, filesize);

	free(buf);

	return 0;
}

int ParseBuf(char* filename, unsigned char* buf, long long bufsize)
{
	long long hits = 0;

	for (unsigned char* p = buf; p < buf + bufsize; p++)
	{
		for (int i = 0; i < textcount; i++)
		{
			if (p + textsizes[i] > buf + bufsize)
			{
				continue;
			}

			if (!_memicmp(p, texts[i], textsizes[i]))
			{
				unsigned char* pStart, * pEnd, printBuf[1001];

				for (pStart = p; pStart > buf&& pStart > p - 500 && *(pStart - 1) != '\r' && *(pStart - 1) != '\n'; pStart--)
					;
				for (pEnd = p; pEnd < buf + bufsize && pEnd < p + 500 && *pEnd != '\r' && *pEnd != '\n'; pEnd++)
					;
				long long size = pEnd - pStart;
				memcpy(printBuf, pStart, size);
				printBuf[size] = 0;
				printf("%s %lld: %s\n", filename, pStart - buf, printBuf);

				hits++;
			}
		}
	}

	if (hits > 0)
	{
		filesfound++;
		totalhits += hits;
		//printf("'%s': %lld\n", filename, hits);
	}

	return 0;
}
