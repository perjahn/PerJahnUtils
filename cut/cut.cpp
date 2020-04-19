/*
2.1 Fixed bug: could write larger outfile than infile, appended junk.
2.2 Added return value.
2.3 Fixed bug, again: could write larger outfile than infile, appended junk.
2.4 Fixed bug, again: crashed if start>size.
2.5 vs2019
*/
#include <stdio.h>
#include <stdlib.h>

int main(int argc, char* argv[])
{
	if (argc != 5)
	{
		printf(
			"cut 2.5\n"
			"\n"
			"Usage: cut <infile> <outfile> <start> <length>\n");
		return 0;
	}

	long long start, length;

	start = _atoi64(argv[3]);
	length = _atoi64(argv[4]);


	FILE* fh;

	if (!(fh = fopen(argv[1], "rb")))
	{
		printf("Couldn't open infile: '%s'\n", argv[1]);
		return 1;
	}

	_fseeki64(fh, 0, SEEK_END);
	long long filesize = _ftelli64(fh);

	if (start > filesize)
	{
		printf("Nothing copied: Start offset %llu are bigger than file size %llu.\n", start, filesize);
		return 1;
	}


	if (start + length > filesize)
	{
		length = filesize - start;
	}

	unsigned char* buf = new unsigned char[length];
	if (!buf)
	{
		printf("Out of memory: %llu bytes.\n", length);
		return 1;
	}


	printf("Copying %llu bytes from '%s' to '%s'.\n", length, argv[1], argv[2]);

	_fseeki64(fh, start, SEEK_SET);
	fread(buf, length, 1, fh);
	fclose(fh);

	if (!(fh = fopen(argv[2], "wb")))
	{
		delete[] buf;
		printf("Couldn't open outfile: '%s'\n", argv[2]);
		return 1;
	}

	fwrite(buf, length, 1, fh);
	fclose(fh);


	return 0;
}
