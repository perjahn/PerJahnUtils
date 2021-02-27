//**********************************************************

#define _XOPEN_SOURCE 500
#include <ftw.h>
#include <stdio.h>
#include <string.h>
#include <malloc.h>
#include <sys/types.h>
#include <fcntl.h>
#include <unistd.h>

char *g_exclude;
long g_filecount;
long g_rowcount;

//**********************************************************

int process_entry(const char *path, const struct stat *sb, int typeflag, struct FTW *ftwbuf)
{
    if (typeflag != FTW_F)
    {
        return 0;
    }

    if (g_exclude != NULL && strstr(path, g_exclude))
    {
        return 0;
    }

    printf("'%s'\n", path);

    g_filecount++;

    off_t size = sb->st_size;

    int fd = open(path, O_RDONLY);
    if (!fd)
    {
        printf("Couldn't open file: '%s'\n", path);
        return 0;
    }

    unsigned char *buf = (unsigned char *)malloc(size);
    if (!buf)
    {
        close(fd);
        printf("Out of memory: %ld bytes\n", size);
        return 0;
    }

    read(fd, buf, size);
    close(fd);

    g_rowcount++;

    for (int i = 0; i < size; i++)
    {
        if (buf[i] == '\n')
        {
            g_rowcount++;
        }
    }

    free(buf);

    return 0;
}

//**********************************************************

int main(int argc, char *argv[])
{
    if (argc != 2 && argc != 3)
    {
        printf("usage: sloc <path> [exclude path substring]\n");
        return 1;
    }

    char *path = argv[1];

    g_exclude = argc == 3 ? argv[2] : NULL;
    g_filecount = 0;
    g_rowcount = 0;

    nftw(path, process_entry, 20, FTW_PHYS);

    printf("Files: %ld\n", g_filecount);
    printf("Rows: %ld\n", g_rowcount);

    return 0;
}

//**********************************************************
