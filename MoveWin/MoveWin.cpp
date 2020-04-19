#include <windows.h>
#include <stdio.h>
#include <string.h>

int main(int argc, char* argv[])
{
	if (argc != 6)
	{
		printf("Usage: MoveWin <window title> <x> <y> <w> <h>\n");
		return 1;
	}

	HWND hwnd = FindWindow(NULL, argv[1]);

	if (!hwnd)
	{
		printf("Couldn't find window, trying as handle value (hex).\n");
		hwnd = (HWND)strtoull(argv[1], NULL, 16);
	}

	int x = atoi(argv[2]);
	int y = atoi(argv[3]);
	int w = atoi(argv[4]);
	int h = atoi(argv[5]);
	MoveWindow(hwnd, x, y, w, h, TRUE);

	return 0;
}
