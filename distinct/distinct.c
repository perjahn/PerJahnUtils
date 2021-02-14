#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <unistd.h>
#include <ctype.h>
#include <limits.h>

int compare(const void *a, const void *b);

struct rowinfo
{
    unsigned char *row;
    unsigned long length;
};

unsigned char *buf;
long sort = -1;

int main(int argc, char *argv[])
{
    if (isatty(fileno(stdin)))
    {
        printf("Only piped input supported.\n");
        return 1;
    }

    if (argc == 2 && strcmp(argv[1], "-s") == 0)
    {
        sort = 0;
    }
    else if (argc == 3 && strcmp(argv[1], "-s") == 0)
    {
        sort = atol(argv[2]);
    }

    unsigned char *p;
    int c = getchar();
    if (c < 0)
    {
        printf("No input.");
        return 1;
    }

    long bufsize = 1024 * 1024 * 1024;
    buf = malloc(bufsize);
    if (!buf)
    {
        printf("Out of memory: %ld\n", bufsize);
        return 1;
    }

    long rowcount = 0;

    for (p = buf; c >= 0 && p < buf + bufsize; p++)
    {
        if (c == '\n')
        {
            rowcount++;
        }
        *p = c;
        c = getchar();
    }
    if (c >= 0)
    {
        free(buf);
        printf("Out of memory: %ld\n", bufsize);
        return 1;
    }
    long datalength = p - buf;

    struct rowinfo *rows = malloc(rowcount * sizeof(struct rowinfo));
    if (!rows)
    {
        free(buf);
        printf("Out of memory: %ld\n", rowcount * sizeof(struct rowinfo));
        return 1;
    }

    rowcount = 0;

    for (p = buf; p < buf + datalength; p++)
    {
        if (p == buf || *(p - 1) == '\n')
        {
            rows[rowcount].row = p;
            if (p > buf)
            {
                rows[rowcount - 1].length = p - rows[rowcount - 1].row;
            }
            rowcount++;
        }
    }
    if (p > buf)
    {
        rows[rowcount - 1].length = p - rows[rowcount - 1].row;
    }

    p[datalength] = 0;

    if (sort >= 0)
    {
        if (rowcount < 2)
        {
            printf("Not sorting, row count: %ld\n", rowcount);
        }
        else
        {
            qsort(rows, rowcount, sizeof(struct rowinfo), compare);
        }
    }

    for (long row = 0; row < rowcount; row++)
    {
        for (long row2 = row - 1; row2 >= 0; row2--)
        {
            if (rows[row2].row != NULL)
            {
                unsigned long l1 = rows[row].length;
                unsigned long l2 = rows[row2].length;
                if (l1 == l2 && memcmp(rows[row].row, rows[row2].row, l1) == 0)
                {
                    rows[row].row = NULL;
                    break;
                }
            }
        }
    }

    for (long row = 0; row < rowcount; row++)
    {
        if (rows[row].row != NULL)
        {
            for (p = rows[row].row; p < rows[row].row + rows[row].length; p++)
            {
                putchar(*p);
            }
        }
    }

    free(rows);
    free(buf);
}

int compare(const void *a, const void *b)
{
    const struct rowinfo *ri1 = a;
    const struct rowinfo *ri2 = b;

    int result;
    if (sort >= 0)
    {
        unsigned long min = ri1->length < ri2->length ? ri1->length : ri2->length;
        min -= sort;

        unsigned char *p1 = ri1->row + sort;
        unsigned char *p2 = ri2->row + sort;
        for (long offset = 0; offset < min;)
        {
            result = tolower(*p1++) - tolower(*p2++);
            if (result != 0)
            {
                return result;
            }
        }
        return 0;
    }
    else
    {
        result = memcmp(ri1->row, ri2->row, ri1->length < ri2->length ? ri1->length : ri2->length);
    }

    if (result != 0)
    {
        return result;
    }

    if (ri1->length == ri2->length)
    {
        return 0;
    }

    if (ri1->length > ri2->length)
    {
        return 1;
    }
    else
    {
        return -1;
    }
}
