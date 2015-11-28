#include <windows.h>
#include <stdio.h>
#include <stdlib.h>

void main(int argc, char *argv[])
{
	if (argc != 6)
	{
		printf("Usage: MoveWin <window title> <x> <y> <w> <h>\n");
		return;
	}

	HWND hwnd = FindWindow(argv[1], NULL);

	if (!hwnd)
	{
		printf("Couldn't find window: '%s'\n", argv[1]);
	}

	hwnd = (HWND)atoi(argv[1]);

	int x = atoi(argv[2]);
	int y = atoi(argv[3]);
	int w = atoi(argv[4]);
	int h = atoi(argv[5]);
	MoveWindow(hwnd, x, y, w, h, TRUE);
}
