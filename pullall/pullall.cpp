#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <direct.h>

void PullAllFolders(char* path);

char folders[10000][1000];

int main(int argc, char* argv[])
{
	if (argc != 2)
	{
		printf("Usage: pullall <path>\n");
		return 1;
	}

	char* path = argv[1];

	PullAllFolders(path);
}

void CombineWildcardPaths(char* output, char* input1, char* input2, char* input3)
{
	memcpy(output, input1, strlen(input1));
	char* p = output + strlen(input1);
	while (p > output && *(p - 1) != '\\')
	{
		p--;
	}

	memcpy(p, input2, strlen(input2));
	p += strlen(input2);
	*p++ = '\\';

	memcpy(p, input3, strlen(input3));
	p += strlen(input3);
	*p = 0;
}

void GetAllFolders(char* path)
{
	int count = 0;
	HANDLE find;
	WIN32_FIND_DATA finddata;
	if ((find = FindFirstFile(path, &finddata)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (count == 10000)
			{
				return;
			}

			if (strcmp(finddata.cFileName, ".") && strcmp(finddata.cFileName, ".."))
			{
				if (strlen(path) + strlen(finddata.cFileName) >= 995)
				{
					printf("Ignoring folder: '%s'\n", finddata.cFileName);
					continue;
				}
				CombineWildcardPaths(folders[count], path, finddata.cFileName, (char*)".git");
				HANDLE findgit;
				WIN32_FIND_DATA finddatagit;
				if ((findgit = FindFirstFile(folders[count], &finddatagit)) != INVALID_HANDLE_VALUE)
				{
					folders[count][strlen(folders[count]) - 5] = 0;
					count++;
					FindClose(findgit);
				}
			}
		} while (FindNextFile(find, &finddata));
		FindClose(find);
	}

	folders[count][0] = 0;
	printf("Found %d repo folders.\n", count);
}

void PullAllFolders(char* path)
{
	GetAllFolders(path);

	char curdir[1000];
	if (!_getcwd(curdir, 1000))
	{
		printf("Couldn't get current dir.\n");
		return;
	}

	for (int i = 0; folders[i][0]; i++)
	{
		char* dir = folders[i];
		printf("Pulling: '%s'\n", dir);
		if (_chdir(dir))
		{
			printf("Couldn't change directory: '%s'.\n", dir);
			continue;
		}
		system("git pull -r");
		Sleep(1000);
	}

	_chdir(curdir);
}
