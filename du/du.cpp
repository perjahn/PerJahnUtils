//**********************************************************

#include <windows.h>
#include <stdio.h>

//**********************************************************

int _depth;
int _maxdepth;

void RecurseDir(wchar_t *szPath, long long unsigned *size, long long unsigned *files, long long unsigned *dirs);

//**********************************************************

void wmain(int argc, wchar_t *argv[])
{
	if(argc<1 || argc>3)
	{
		printf(
			"du 2.2\n"
			"\n"
			"Usage: du [path] [depth]\n");
		return;
	}


	long long unsigned size, files, dirs;
	size = files = dirs = 0;

	_depth = 0;

	if(argc==3)
		_maxdepth = _wtoi(argv[2]);
	else
		_maxdepth = -1;

	wchar_t szSubPath[1000];

	if(argc>=2 && !wcscmp(argv[1], L".."))
	{
		wcscpy(szSubPath, L"..\\*");
	}
	else if(argc>=2 && wcscmp(argv[1], L"."))
	{
		wcscpy(szSubPath, argv[1]);
	}
	else
	{
		wcscpy(szSubPath, L"*");
	}

	RecurseDir(szSubPath, &size, &files, &dirs);

	if(size)
	{
		wprintf(L"Total: Size: %llu bytes (%.1fkb / %.1fmb / %.2fgb), Files: %llu, Dirs: %llu.\n",
			size, size/1024.0, size/1024/1024.0, size/1024/1024/1024.0, files, dirs);
	}
	else
	{
		wprintf(L"Total: Size: 0 bytes, Files: 0, Dirs: 0.\n");
	}

	return;
}

//**********************************************************

void RecurseDir(wchar_t *szPath, long long unsigned *size, long long unsigned *files, long long unsigned *dirs)
{
	_depth++;

	HANDLE hFind;
	WIN32_FIND_DATA Data;

	wchar_t *p;
	for(p=szPath+wcslen(szPath); p>szPath && *(p-1)!='\\' && *(p-1)!=':'; p--)
		;
	
	//wprintf(L"'%s'\n", szPath);
	if((hFind=FindFirstFile(szPath, &Data))!=INVALID_HANDLE_VALUE)
	{
		do
		{
			if(*(Data.cFileName) && wcscmp(Data.cFileName, L".") && wcscmp(Data.cFileName, L".."))
			{
				if(Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				{
					// Dir

					(*dirs)++;

					swprintf(p, L"%s\\*", Data.cFileName);
					long long unsigned subsize, subfiles, subdirs;
					subsize = subfiles = subdirs = 0;

					RecurseDir(szPath, &subsize, &subfiles, &subdirs);
					
					//wprintf(L"%d\n", _depth);

					*(p+wcslen(Data.cFileName)) = 0;

					if(_maxdepth==-1 || _depth<=_maxdepth)
					{
						wprintf(L"%11llu %s\n", subsize, szPath);
					}

					(*size) += subsize;
					(*files) += subfiles;
					(*dirs) += subdirs;
				}
				else
				{
					// File
					(*size) += (((long long unsigned)(Data.nFileSizeHigh))<<32) + Data.nFileSizeLow;
					(*files)++;
				}
			}
		}
		while(FindNextFile(hFind, &Data));

		FindClose(hFind);
	}

	_depth--;
}

//**********************************************************
