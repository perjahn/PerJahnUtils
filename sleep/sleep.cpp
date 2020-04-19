#include <windows.h>
#include <stdio.h>
#include <stdlib.h>

int main(int argc, char* argv[])
{
	if (argc != 2)
	{
		printf("Usage: sleep.exe <milliseconds>\n");
		return 1;
	}
	Sleep(atoi(argv[1]));
	return 0;
}
