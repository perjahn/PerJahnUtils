//**********************************************************

#include <windows.h>
#include <stdio.h>

//**********************************************************

int _depth;
int _maxdepth;
FILETIME _from, _to;

void RecurseDir(char *path, long long unsigned *size, long long unsigned *files, long long unsigned *dirs);
void PrintTime(char *prefix, FILETIME *ft);

//**********************************************************

void main(int argc, char *argv[])
{
	if (argc < 1 || argc > 4 || (argc == 4 && strlen(argv[3]) != 17))
	{
		printf(
			"du 2.3\n"
			"\n"
			"Usage: du <path> [depth] [date interval]\n"
			"\n"
			"depth:          Will not affect the sizes, only printed output.\n"
			"date interval:  Will affect the sizes.\n"
			"\n"
			"date interval should be specified using this format: yyyyMMdd-yyyyMMdd\n"
		);
		return;
	}


	long long unsigned size, files, dirs;
	size = files = dirs = 0;

	_depth = 0;

	_from.dwHighDateTime = 0;
	_from.dwLowDateTime = 0;
	_to.dwHighDateTime = 0xFFFFFFFF;
	_to.dwLowDateTime = 0xFFFFFFFF;

	if (argc == 4)
	{
		SYSTEMTIME st;

		ZeroMemory(&st, sizeof(SYSTEMTIME));

		char buf[5];

		buf[0] = argv[3][0];
		buf[1] = argv[3][1];
		buf[2] = argv[3][2];
		buf[3] = argv[3][3];
		buf[4] = 0;
		st.wYear = atoi(buf);

		buf[0] = argv[3][4];
		buf[1] = argv[3][5];
		buf[2] = 0;
		st.wMonth = atoi(buf);

		buf[0] = argv[3][6];
		buf[1] = argv[3][7];
		st.wDay = atoi(buf);

		SystemTimeToFileTime(&st, &_from);


		buf[0] = argv[3][9];
		buf[1] = argv[3][10];
		buf[2] = argv[3][11];
		buf[3] = argv[3][12];
		buf[4] = 0;
		st.wYear = atoi(buf);

		buf[0] = argv[3][13];
		buf[1] = argv[3][14];
		buf[2] = 0;
		st.wMonth = atoi(buf);

		buf[0] = argv[3][15];
		buf[1] = argv[3][16];
		st.wDay = atoi(buf);

		SystemTimeToFileTime(&st, &_to);

		_maxdepth = atoi(argv[2]);
	}
	else if (argc == 3)
	{
		_maxdepth = atoi(argv[2]);
	}
	else
	{
		_maxdepth = -1;
	}

	if (_from.dwHighDateTime != 0 && _from.dwLowDateTime != 0 && _to.dwHighDateTime != 0xFFFFFFFF && _to.dwLowDateTime != 0xFFFFFFFF)
	{
		PrintTime("From:", &_from);
		PrintTime("To:  ", &_to);
	}

	char subPath[1000];

	if (argc >= 2 && !strcmp(argv[1], ".."))
	{
		strcpy(subPath, "..\\*");
	}
	else if (argc >= 2 && strcmp(argv[1], "."))
	{
		strcpy(subPath, argv[1]);
	}
	else
	{
		strcpy(subPath, "*");
	}

	RecurseDir(subPath, &size, &files, &dirs);

	printf("Total: Size: %llu bytes (%.1fkb / %.1fmb / %.2fgb), Files: %llu, Dirs: %llu.\n",
		size, size / 1024.0, size / 1024 / 1024.0, size / 1024 / 1024 / 1024.0, files, dirs);

	return;
}

//**********************************************************

void RecurseDir(char *path, long long unsigned *size, long long unsigned *files, long long unsigned *dirs)
{
	_depth++;

	HANDLE hFind;
	WIN32_FIND_DATA Data;

	char *p;
	for (p = path + strlen(path); p > path && *(p - 1) != '\\' && *(p - 1) != ':'; p--)
		;

	if ((hFind = FindFirstFile(path, &Data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if (Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				{
					// Dir

					(*dirs)++;

					sprintf(p, "%s\\*", Data.cFileName);
					long long unsigned subsize, subfiles, subdirs;
					subsize = subfiles = subdirs = 0;

					RecurseDir(path, &subsize, &subfiles, &subdirs);

					*(p + strlen(Data.cFileName)) = 0;

					if (_maxdepth == -1 || _depth <= _maxdepth)
					{
						printf("%12llu %s\n", subsize, path);
					}

					(*size) += subsize;
					(*files) += subfiles;
					(*dirs) += subdirs;
				}
				else
				{
					// File
					if (CompareFileTime(&(Data.ftLastWriteTime), &_from) >= 0 && CompareFileTime(&(Data.ftLastWriteTime), &_to) <= 0)
					{
						(*size) += (((long long unsigned)(Data.nFileSizeHigh)) << 32) + Data.nFileSizeLow;
						(*files)++;
					}
				}
			}
		} while (FindNextFile(hFind, &Data));

		FindClose(hFind);
	}

	_depth--;
}

//**********************************************************

void PrintTime(char *prefix, FILETIME *ft)
{
	SYSTEMTIME st;

	FileTimeToSystemTime(ft, &st);
	printf("%s %04hu-%02hu-%02hu %02hu:%02hu:%02hu.%03hu\n",
		prefix,
		st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);

	return;
}

//**********************************************************
