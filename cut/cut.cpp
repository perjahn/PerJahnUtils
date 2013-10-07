/*
2.1 Fixed bug: could write larger outfile than infile, appended junk.
2.2 Added return value.
*/
#include <stdio.h>
#include <stdlib.h>

int main(int argc, char *argv[])
{
	if(argc!=5)
	{
		printf(
			"cut 2.2\n"
			"\n"
			"Usage: cut <infile> <outfile> <start> <length>\n");
		return 0;
	}

	long start, length;

	start = atol(argv[3]);
	length = atol(argv[4]);

	unsigned char *buf = new unsigned char[length];
	if(!buf)
	{
		printf("Out of memory (%u bytes).\n", length);
		return 0;
	}

	FILE *fh;

	if(!(fh = fopen(argv[1], "rb")))
	{
		delete[] buf;
		printf("Couldn't open infile (%s).\n", argv[1]);
		return 0;
	}

	fseek(fh, start, SEEK_SET);
	fread(buf, length, 1, fh);
	fclose(fh);

	if(!(fh = fopen(argv[2], "wb")))
	{
		delete[] buf;
		printf("Couldn't open outfile (%s).\n", argv[2]);
		return 0;
	}

	fwrite(buf, length, 1, fh);
	fclose(fh);


	return length;
}
