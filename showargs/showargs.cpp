#include <windows.h>
#include <stdio.h>

int main(int argc, char* argv[])
{
	char* p = GetCommandLine();

	printf("Windows command line: >>>%s<<<\n\n", p);

	for (int i = 0; i < argc; i++)
	{
		printf("%d >>>%s<<<\n", i, argv[i]);
	}

	getchar();

	return 0;
}
