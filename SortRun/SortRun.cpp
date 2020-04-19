#include <windows.h>
#include <stdio.h>
#include <string.h>

int main(int argc, char* argv[])
{
	char* usage =
		(char*)"SortRun 0.001 gamma"
		"\n"
		"Usage: SortRun <file1> <file2> <command>\n"
		"\n"
		"Example: sortrun myfile1 myfile2 diff\n";

	if (argc != 4)
	{
		printf(usage);
		return 1;
	}

	if (strlen(argv[1]) > 1000)
	{
		printf("Filename 1 too long.");
		return 1;
	}
	if (strlen(argv[2]) > 1000)
	{
		printf("Filename 2 too long.");
		return 1;
	}
	if (strlen(argv[3]) > 1000)
	{
		printf("Command too long.");
		return 1;
	}

	char tempdir[1000];

	if (!GetTempPath(500, tempdir))
	{
		printf("Couldn't get temp dir (%u).\n", GetLastError());
		return 1;
	}

	char command[5000];

	sprintf(command, "sort \"%s\" > \"%ssortfile1.txt\"", argv[1], tempdir);
	//printf(">>>%s<<<\n", command);
	system(command);

	sprintf(command, "sort \"%s\" > \"%ssortfile2.txt\"", argv[2], tempdir);
	//printf(">>>%s<<<\n", command);
	system(command);

	sprintf(command, "%s \"%ssortfile1.txt\" \"%ssortfile2.txt\"", argv[3], tempdir, tempdir);
	//printf(">>>%s<<<\n", command);
	system(command);

	return 0;
}
