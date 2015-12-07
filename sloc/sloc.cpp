//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <string.h>

long g_filecount;
long g_rowcount;

void recurse_dir(char *path);
void process_file(char *filename);

//**********************************************************

void main(int argc, char *argv[])
{
	if (argc != 2)
	{
		printf("usage: sloc <path>\n");
		return;
	}

	char path[1000];

	strcpy(path, argv[1]);

	// Fix end of pattern to do what's expected.
	int length = strlen(path);
	if (!strcmp(path, "."))
	{
		strcpy(path + length, "\\*");
	}
	else if (length >= 2 && !strcmp(path + length - 2, "\\."))
	{
		strcpy(path + length - 1, "*");
	}
	else if (!strcmp(path, ".."))
	{
		strcpy(path + length, "\\*");
	}
	else if (length >= 3 && !strcmp(path + length - 3, "\\.."))
	{
		strcpy(path + length - 2, "*");
	}

	g_filecount = 0;
	g_rowcount = 0;

	recurse_dir(path);

	printf("Files: %d\n", g_filecount);
	printf("Rows: %d\n", g_rowcount);

	return;
}

//**********************************************************

void recurse_dir(char *path)
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;
	char szSubPath[1000];

	char *p, *p2;

	for (p = path + strlen(path); p > path && *(p - 1) != '\\' && *(p - 1) != ':'; p--)
		;

	strcpy(szSubPath, path);
	p2 = szSubPath + (p - path);

	if ((hFind = FindFirstFile(path, &Data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if (!(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY))
				{
					// File

					strcpy(p2, Data.cFileName);
					process_file(szSubPath);
				}
			}
		} while (FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	strcpy(p2, "*");

	if ((hFind = FindFirstFile(szSubPath, &Data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if (Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				{
					// Dir

					sprintf(p2, "%s\\%s", Data.cFileName, p);
					recurse_dir(szSubPath);
				}
			}
		} while (FindNextFile(hFind, &Data));

		FindClose(hFind);
	}

	return;
}

//**********************************************************

void process_file(char *filename)
{
	g_filecount++;

	FILE *fh;

	fh = fopen(filename, "rb");

	if (!fh)
	{
		printf("Couldn't open file (%s).\n", filename);
		return;
	}

	fseek(fh, 0, SEEK_END);

	long size = ftell(fh);

	unsigned char *buf = new unsigned char[size];
	if (!buf)
	{
		fclose(fh);
		printf("Out of memory (%ld bytes).\n", size);
		return;
	}

	fseek(fh, 0, SEEK_SET);
	fread(buf, size, 1, fh);

	long count = 1;

	for (int i = 0; i < size; i++)
	{
		if (buf[i] == '\n')
		{
			count++;
		}
	}

	fclose(fh);

	delete[] buf;

	g_rowcount += count;

	return;
}

//**********************************************************
