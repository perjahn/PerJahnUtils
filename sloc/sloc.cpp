//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <string.h>

long g_filecount;
long g_rowcount;

void recurse_dir(char *szPath);
void process_file(char *szFileName);

//**********************************************************

void main(int argc, char *argv[])
{
	if (argc != 2)
	{
		printf("usage: sloc <path>\n");
		return;
	}

	char szPath[1000];

	strcpy(szPath, argv[1]);

	// Fix end of pattern to do what's expected.
	int length = strlen(szPath);
	if (!strcmp(szPath, "."))
	{
		strcpy(szPath + length, "\\*");
	}
	else if (length >= 2 && !strcmp(szPath + length - 2, "\\."))
	{
		strcpy(szPath + length - 1, "*");
	}
	else if (!strcmp(szPath, ".."))
	{
		strcpy(szPath + length, "\\*");
	}
	else if (length >= 3 && !strcmp(szPath + length - 3, "\\.."))
	{
		strcpy(szPath + length - 2, "*");
	}

	g_filecount = 0;
	g_rowcount = 0;

	recurse_dir(szPath);

	printf("Files: %d\n", g_filecount);
	printf("Rows: %d\n", g_rowcount);

	return;
}

//**********************************************************

void recurse_dir(char *szPath)
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;
	char szSubPath[1000];

	char *p, *p2;

	for (p = szPath + strlen(szPath); p > szPath && *(p - 1) != '\\' && *(p - 1) != ':'; p--)
		;

	strcpy(szSubPath, szPath);
	p2 = szSubPath + (p - szPath);

	if ((hFind = FindFirstFile(szPath, &Data)) != INVALID_HANDLE_VALUE)
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

void process_file(char *szFileName)
{
	g_filecount++;

	FILE *fh;

	fh = fopen(szFileName, "rb");

	if (!fh)
	{
		printf("Couldn't open file (%s).\n", szFileName);
		return;
	}

	fseek(fh, 0, SEEK_END);

	long size = ftell(fh);

	unsigned char *buf = new unsigned char[size];
	if (!buf)
	{
		fclose(fh);
		printf("Out of memory (%s bytes).\n", size);
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
