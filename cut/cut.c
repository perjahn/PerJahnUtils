/*
2.1 Fixed bug: could write larger outfile than infile, appended junk.
2.2 Added return value.
2.3 Fixed bug, again: could write larger outfile than infile, appended junk.
2.4 Fixed bug, again: crashed if start>size.
2.5 vs2019
3.0 Linux
*/
#include <stdio.h>
#include <stdlib.h>

int main(int argc, char *argv[])
{
	if (argc != 5)
	{
		printf(
			"cut 3.0\n"
			"\n"
			"Usage: cut <infile> <outfile> <start> <length>\n");
		return 0;
	}

	char *infile = argv[1];
	char *outfile = argv[2];
	long start = atol(argv[3]);
	long length = atol(argv[4]);

	FILE *fh;

	if (!(fh = fopen(infile, "r")))
	{
		printf("Couldn't open infile: '%s'\n", infile);
		return 1;
	}

	fseek(fh, 0, SEEK_END);
	long filesize = ftell(fh);

	if (start > filesize)
	{
		printf("Nothing copied: Start offset %lu are bigger than file size %lu.\n", start, filesize);
		return 1;
	}

	if (start + length > filesize)
	{
		length = filesize - start;
	}

	unsigned char *buf = malloc(length);
	if (!buf)
	{
		printf("Out of memory: %lu bytes.\n", length);
		return 1;
	}

	printf("Copying %lu bytes from '%s' to '%s'.\n", length, infile, outfile);

	fseek(fh, start, SEEK_SET);
	fread(buf, length, 1, fh);
	fclose(fh);

	if (!(fh = fopen(outfile, "w")))
	{
		free(buf);
		printf("Couldn't open outfile: '%s'\n", outfile);
		return 1;
	}

	fwrite(buf, length, 1, fh);
	fclose(fh);

	return 0;
}
