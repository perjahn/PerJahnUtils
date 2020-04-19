//**********************************************************
/*
Version 2.0
New implementation.
Version 2.01
Fixed buffer overflow bug
Version 2.1
max size limit as parameter (hardcoded to 16MB before). -m
Version 2.11
support for case sensitive -i
Version 2.12
trim whitespace (space & tab) of printed strings. -t
Version 2.13
print line numbers. -n
Version 2.14
refactored set_options()
changed: -t does now also trim searched string (can prevent output)
support for escaping ascii values (hex & dec). -h & -d
Version 2.15
print line number even if string trimmed to nothing
prevent beep
Version 2.16
restore console color when breaking
Version 2.17
x64
Version 2.18
Exclude dirs
Version 2.19
Optimize string matching (length instead of null termination).
Fix: strnicmp slutar vid null!
Version 2.20
Bug fix for null in search strings
Handle embedded nulls (\0) in search string.
Version 2.21
64-bit file sizes
Version 2.22
vs2019


To compile, change project settings:
Configuration Properties / General
Character Set = Not Set

To make exe vcrt dll independent:
Configuration Properties / C++ / Code Generation
Runtime Library = Multi-threaded

Todo:
BUG: Om söksträng "" anges så buggar nog process_file
BUG: Bara ett minustecken för att excludera filer hänger programmet.
support for unicode (non-obvious output today, 1 char per line).
More advanced search logic: 0. x not in file (/v) 1. x not in file, but y. 2. x not on any row, but y.
Prevent "beep". Option: Don't convert BEL to dot. -b
This is ascii value 149,
1. Is not prevented by cutting strings (user can search on ascii value)
2. Must be replaced with space/dot or empty string
3. Optional feature to replace control chars (<32) could be useful.
Allow several include patterns, "*.x, +*.y"

convert2oem after string are cut, could result in utf8 control codes splitting/or dos converting. Probably because of null char.
*/

#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifndef memicmp
#define memicmp _memicmp
#endif

//**********************************************************

// g_string_length is the maximum number of text characters in printed strings.
// g_string_length buffers must also include newline+null.
unsigned long long g_string_length = 10240;

bool g_preservecharset = false;  // Presere windows charset instead of converting to dos
bool g_escape_dec = false;       // Should escape sequences (dec) in search input be converted?
bool g_print_first_row = false;  // Should first row of file be printed, even if there's no match?
bool g_escape_hex = false;       // Should escape sequences (hex) in search input be converted?
bool g_case_sensitive = false;   // Is text comparations case sensitive?
bool g_only_filename = false;    // Should print only file names? (instead of grepping text)
unsigned long long g_maxsize = 16ULL * 1024 * 1024 * 1024;
bool g_line_numbers = false;     // Print line numbers?
bool g_recurse = false;          // Should recurse into sub dirs?
bool g_show_statistics = false;  // Show statistics.
bool g_trim_whitespace = false;  // Should space & tab be trimmed before printing?

bool* g_pflags[] =
{
	(bool*)&g_string_length, &g_preservecharset, &g_escape_dec, &g_print_first_row, &g_escape_hex, &g_case_sensitive,
	&g_only_filename, (bool*)&g_maxsize, &g_line_numbers, &g_recurse, &g_show_statistics,
	&g_trim_whitespace
};
char* g_flags = (char*)"bcdfhilmnrst";    // Flags, in the same order as g_pflags pointers

DWORD g_time1, g_time2;

unsigned char** g_aszSearch;     // Array of search tokens. This is argv+x
unsigned g_tokens;               // Number of search tokens.
unsigned* g_tokens_lengths;      // Length of search tokens.
char** g_aszExcludePatterns;     // Exclude patterns. This is argv+y
unsigned g_ExcludePatterns;      // Number of exclude patterns.

// Statistics
unsigned long long g_searched_files;
unsigned long long g_searched_dirs;
unsigned long long g_searched_bytes;

unsigned long long g_excluded_files;
unsigned long long g_excluded_dirs;
unsigned long long g_excluded_bytes;

unsigned long long g_found_files;
unsigned long long g_found_matches;

//**********************************************************

void print_color(char* szText, unsigned color)
{
	HANDLE hStdout;
	char* pszText, * txt2 = NULL;


	if (!g_preservecharset)
	{
		txt2 = new char[g_string_length + 10];
		CharToOem(szText, txt2);
		pszText = txt2;
	}
	else
	{
		pszText = szText;
	}


	if (color == 0)
	{
		color = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE;
	}


	hStdout = GetStdHandle(STD_OUTPUT_HANDLE);
	if (hStdout != INVALID_HANDLE_VALUE)
	{
		SetConsoleTextAttribute(hStdout, color);
	}

	printf("%s", pszText);

	if (txt2)
	{
		delete[] txt2;
	}

	return;
}

//**********************************************************

void process_file(char* szFileName)
{
	FILE* fh;
	unsigned long long size;
	size_t size2;
	size_t size3;
	unsigned char* buf;
	bool bFound;
	unsigned char* p, * p1, * p2;  // p1->start of line, p2->end of line
	static unsigned char* txt = NULL;
	static char* s = NULL;
	unsigned line = 1;


	if (!txt)
		txt = new unsigned char[g_string_length + 10];
	if (!s)
		s = new char[g_string_length + 10];


	bFound = false;

	fh = fopen(szFileName, "rb");
	if (fh)
	{
		_fseeki64(fh, 0, SEEK_END);
		size = _ftelli64(fh);

		if (!size)
		{
			fclose(fh);
			return;
		}

		buf = new unsigned char[size];
		if (!buf)
		{
			fclose(fh);
			return;
		}

		_fseeki64(fh, 0, SEEK_SET);
		fread(buf, size, 1, fh);

		for (p = buf; p < buf + size; p++)
		{
			for (unsigned i = 0; i < g_tokens; i++)
			{
				size2 = g_tokens_lengths[i];
				if (p + size2 >= buf + size)
					continue;


				int match;

				if (g_case_sensitive)
					match = memcmp(p, g_aszSearch[i], size2);
				else
					match = memicmp(p, g_aszSearch[i], size2);

				if (!match)
				{
					g_found_matches++;

					if (g_only_filename)
					{
						g_found_files++;
						delete[] buf;
						fclose(fh);

						sprintf(s, "%s\n", szFileName);
						print_color(s, 0);

						return;
					}
					else
					{
						if (!bFound)
						{
							g_found_files++;

							sprintf(s, "Searching: %s\n", szFileName);
							print_color(s, FOREGROUND_GREEN | FOREGROUND_INTENSITY);

							bFound = true;

							if (g_print_first_row && line > 1)
							{
								unsigned char* p3;
								char szFirstRow[10001];

								// Print first row
								for (p3 = buf; p3 < buf + 10000 && *p3 && *p3 != '\r' && *p3 != '\n'; p3++)
									;
								memcpy(szFirstRow, buf, p3 - buf);
								szFirstRow[p3 - buf] = 0;
								print_color(szFirstRow, 0);
							}
						}

						if (size2 >= g_string_length)
						{
							p1 = p;  // Start
							p2 = p + g_string_length;  // End
						}
						else
						{
							// Separate strings on control characters (ascii < 32)

							// Allow tab

							// Get start of string.
							p1 = p;
							if (*p1 >= ' ' || *p1 == '\t' || *p1 == 0)
								for (; p1 > buf && (*(p1 - 1) >= ' ' || *(p1 - 1) == '\t' || *(p1 - 1) == 0) && p1 > p - g_string_length + size2; p1--)
									;

							// Get end of string.
							p2 = p + size2;
							if (*(p2 - 1) >= ' ' || *(p2 - 1) == '\t' || *(p2 - 1) == 0)
								for (; p2 < buf + size && (*p2 >= ' ' || *p2 == '\t' || *p2 == 0); p2++)
									;

							if (g_trim_whitespace)
							{
								for (; p1 < p + size2 && (*p1 == '\t' || *p1 == '\n' || *p1 == '\r' || *p1 == ' '); p1++)
									;
								for (; p2 > p && (*(p2 - 1) == '\t' || *(p2 - 1) == '\n' || *(p2 - 1) == '\r' || *(p2 - 1) == ' '); p2--)
									;
							}
						}

						if (p2 <= p1)
						{
							// Found string trimmed to nothing
							// Filename are printed even if only this trigger.

							// Print line number even if string trimmed to nothing
							if (g_line_numbers)
							{
								sprintf(s, "%u\n", line);
								print_color(s, 0);
							}

							continue;
						}

						if ((unsigned)(p2 - p1) > g_string_length)
							size3 = g_string_length;
						else
							size3 = p2 - p1;

						memcpy(txt, p1, size3);
						txt[size3] = 0;  // Null terminate string


						// Convert beep to period
						for (p1 = txt; *p1; p1++)
						{
							if (*p1 == 149)
								*p1 = '.';
						}

						if (g_line_numbers)
							sprintf(s, "%u %s\n", line, txt);
						else
							sprintf(s, "%s\n", txt);

						print_color(s, 0);

#ifdef _DEBUG
						printf("%llu\n", p2 - buf);
						printf("%llu\n", p - buf);
#endif

						p = p2;
					}
				}
			}

			if (*p == '\n')
			{
				line++;
			}
		}

		delete[] buf;
		fclose(fh);
	}

	return;
}

//**********************************************************
// Recursive function
/*
Note:
On first level, szDir are actually an user specified argument from main.
This is an important feature, parameter to FindFirstFile() are exactly
what user writes. Always.
*/

void process_dir(char* szDir)
{
	HANDLE hFind;
	WIN32_FIND_DATA Data;
	char szPath[1000];
	char* p, * p2;
	bool bExclude;
	unsigned exclude_count;
	unsigned long long filesize;
	char** exclude_names;  // Array with excluded entries.


	if (g_ExcludePatterns > 0)
		exclude_names = new char* [100000];
	else
		exclude_names = NULL;


	// Search for files to exclude (currently, one unique file can be excluded multiple times)
	exclude_count = 0;
	for (unsigned i = 0; i < g_ExcludePatterns; i++)
	{
		strcpy(szPath, szDir);
		for (p = szPath + strlen(szPath); p > szPath && *(p - 1) != '\\' && *(p - 1) != ':'; p--);
		strcpy(p, g_aszExcludePatterns[i] + 1);
		if ((hFind = FindFirstFile(szPath, &Data)) != INVALID_HANDLE_VALUE)
		{
			do
			{
				if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
				{
					// File & Dir

					if (exclude_count < 100000)
					{
						exclude_names[exclude_count] = new char[strlen(Data.cFileName) + 1];

						if (exclude_names[exclude_count])
						{
							strcpy(exclude_names[exclude_count], Data.cFileName);
							exclude_count++;
						}
					}
					else
					{
						print_color((char*)"-=-=- MAX EXCLUDE ENTRIES REACHED -=-=-\n", FOREGROUND_RED | FOREGROUND_INTENSITY);
					}
				}
			} while (FindNextFile(hFind, &Data));

			FindClose(hFind);
		}
	}


	// Search for files (and exclude previous found, and by size)
	if ((hFind = FindFirstFile(szDir, &Data)) != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
			{
				if (Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				{
					// Dir
				}
				else
				{
					// File

					filesize = (((unsigned long long)(Data.nFileSizeHigh)) << 32) | Data.nFileSizeLow;

					///g_total_files++;
					///g_total_bytes+=Data.nFileSizeLow+(((unsigned long long)(Data.nFileSizeHigh))<<32);

					bExclude = false;
					for (unsigned i = 0; i < exclude_count; i++)
					{
						if (!strcmp(Data.cFileName, exclude_names[i]))
						{
							bExclude = true;
						}
					}

					// Exclude "by size" in this loop, else multiple patterns could
					// cause the file size be counted several times.

					if (filesize > g_maxsize)
					{
						//						sprintf(s, "-=-=- File to large, ignoring: '%s' -=-=-\n", szPath);
						//						print_color(s, FOREGROUND_RED|FOREGROUND_INTENSITY);

						bExclude = true;
					}

					if (bExclude)
					{
						g_excluded_files++;
						g_excluded_bytes += filesize;
					}
					else
					{
						g_searched_files++;
						g_searched_bytes += filesize;

						strcpy(szPath, szDir);
						for (p = szPath + strlen(szPath); p > szPath && *(p - 1) != '\\' && *(p - 1) != ':'; p--);
						strcpy(p, Data.cFileName);

						process_file(szPath);
					}
				}
			}
		} while (FindNextFile(hFind, &Data));

		FindClose(hFind);
	}


	// Search for subdirs
	if (g_recurse)
	{
		strcpy(szPath, szDir);
		for (p = szPath + strlen(szPath); p > szPath && *(p - 1) != '\\' && *(p - 1) != ':'; p--);
		sprintf(p, "*");  // Search in all subdirs

		if ((hFind = FindFirstFile(szPath, &Data)) != INVALID_HANDLE_VALUE)
		{
			do
			{
				if (*(Data.cFileName) && strcmp(Data.cFileName, ".") && strcmp(Data.cFileName, ".."))
				{
					if (Data.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
					{
						// Dir

						bExclude = false;
						for (unsigned i = 0; i < exclude_count; i++)
						{
							if (!strcmp(Data.cFileName, exclude_names[i]))
							{
								bExclude = true;
							}
						}

						if (bExclude)
						{
							g_excluded_dirs++;
						}
						else
						{
							g_searched_dirs++;

							// Locate file pattern
							strcpy(szPath, szDir);
							for (p = szPath + strlen(szPath); p > szPath && *(p - 1) != '\\' && *(p - 1) != ':'; p--);
							// Get original file pattern
							for (p2 = szDir + strlen(szDir); p2 > szDir && *(p2 - 1) != '\\' && *(p2 - 1) != ':'; p2--);

							// Replace file pattern with subdir+file pattern
							sprintf(p, "%s\\%s", Data.cFileName, p2);

							process_dir(szPath);
						}
					}
				}
			} while (FindNextFile(hFind, &Data));

			FindClose(hFind);
		}
	}

	// Deallocate exclude file buffers
	for (unsigned i = 0; i < exclude_count; i++)
	{
		delete[] exclude_names[i];
	}

	if (exclude_names)
		delete[] exclude_names;


	return;
}

//**********************************************************
// Format per cent string

void get_percent(unsigned long long total, unsigned long long value, char* buf)
{
	int percent;

	if (total && value)
	{
		percent = (int)(value * 100 / total);
		if (percent)
			sprintf(buf, "%d", percent);
		else
			strcpy(buf, "<1");
	}
	else
		strcpy(buf, "0");
}

//**********************************************************

void show_statistics(void)
{
	if (g_show_statistics)
	{
		char p1[10], p2[10];

		unsigned long long total_files = g_searched_files + g_excluded_files;
		unsigned long long total_bytes = g_searched_bytes + g_excluded_bytes;

		char s[1000];


		//*** Searched ***

		get_percent(total_bytes, g_searched_bytes, p1);
		get_percent(total_files, g_searched_files, p2);

		sprintf(s, "Searched: %11llu (%3s%%) bytes in %7llu (%3s%%) files. %7llu dirs.\n",
			g_searched_bytes, p1, g_searched_files, p2, g_searched_dirs);
		print_color(s, FOREGROUND_BLUE | FOREGROUND_RED | FOREGROUND_INTENSITY);


		//*** Found ***

		get_percent(total_files, g_found_files, p2);

		sprintf(s, "Found:    %11llu      matches in %7llu (%3s%%) files.\n",
			g_found_matches, g_found_files, p2);
		print_color(s, FOREGROUND_BLUE | FOREGROUND_RED | FOREGROUND_INTENSITY);


		//*** Excluded ***

		get_percent(total_bytes, g_excluded_bytes, p1);
		get_percent(total_files, g_excluded_files, p2);

		sprintf(s, "Excluded: %11llu (%3s%%) bytes in %7llu (%3s%%) files. %7llu dirs.\n",
			g_excluded_bytes, p1, g_excluded_files, p2, g_excluded_dirs);
		print_color(s, FOREGROUND_BLUE | FOREGROUND_RED | FOREGROUND_INTENSITY);


		//*** Total ***

		sprintf(s, "Total:    %11llu        bytes in %7llu        files.\n",
			total_bytes, total_files);
		print_color(s, FOREGROUND_BLUE | FOREGROUND_RED | FOREGROUND_INTENSITY);


		//*** Performance ***

		DWORD diff = g_time2 - g_time1;

		if (diff)
		{
			sprintf(s, "Time: %.1f s, scanned %.1f kB/s.\n",
				diff / 1000.0, g_searched_bytes * 1000.0 / diff / 1024);
		}
		else
		{
			sprintf(s, "Time: 0 s.\n");
		}
		print_color(s, FOREGROUND_BLUE | FOREGROUND_RED | FOREGROUND_INTENSITY);
	}

	return;
}

//**********************************************************
// Remove valid flags from search token array

void set_options(void)
{
	bool foundflag;


	foundflag = true;
	while (foundflag && g_tokens > 0)
	{
		foundflag = false;

		unsigned char* arg = g_aszSearch[0];

		if (arg[0] == '-')
		{
			for (unsigned i = 0; g_flags[i]; i++)
			{
				if (arg[1] == g_flags[i])
				{
					// Found a matching flag

					if (arg[1] == 'b' || arg[1] == 'm')
					{
						if (arg[2])
						{
							*(unsigned long long*)(g_pflags[i]) = _atoi64((char*)(arg + 2));
						}
						// Else ignore "-b"/"-m" argument
					}
					else
					{
						*g_pflags[i] = true;
					}

					g_aszSearch++;
					g_tokens--;
					foundflag = true;
				}
			}
		}
	}

	return;
}

//**********************************************************
// Rewrite ascii values in search tokens, if g_escape_dec or g_escape_hex are set
//
// Also calculates token lengths (g_tokens_lengths)
//
// Possible syntaxes for an escape sequences:
// \D
// \DD
// \DDD
// \H
// \HH
//
void rewrite_tokens(void)
{
	unsigned char* p1, * p2, * p3;
	char buf[4];
	long value;
	int base = 0;


	g_tokens_lengths = new unsigned[g_tokens];


	if (g_escape_dec)
		base = 10;
	if (g_escape_hex)
		base = 16;

	for (unsigned i = 0; i < g_tokens; i++)
	{
		for (p1 = p2 = g_aszSearch[i]; *p1; p2++)
		{
			p3 = p1;
			if (g_escape_dec)
			{
				if (p1[0] == '\\' && isdigit(p1[1]))
				{
					// Set end pointer
					if (isdigit(p1[2]))
					{
						if (isdigit(p1[3]))
							p3 = p1 + 4;
						else
							p3 = p1 + 3;
					}
					else
						p3 = p1 + 2;
				}
			}
			if (g_escape_hex)
			{
				if (p1[0] == '\\' && isxdigit(p1[1]))
				{
					// Set end pointer
					if (isxdigit(p1[2]))
						p3 = p1 + 3;
					else
						p3 = p1 + 2;
				}
			}

			if (p1 != p3)
			{
				memcpy(buf, p1 + 1, p3 - p1 - 1);
				buf[p3 - p1 - 1] = 0;  // Null terminate numeric value

				value = strtol(buf, NULL, base);
				if (value > 255)
					value = 255;
				*p2 = (unsigned char)value;

				p1 += (p3 - p1);
			}
			else
			{
				*p2 = *p1;
				p1++;
			}
		}

		g_tokens_lengths[i] = (unsigned)(p2 - g_aszSearch[i]);
	}

	return;
}

//**********************************************************

WORD g_wOldColorAttrs = 0;

// Save the current text color

void SaveColor(void)
{
	HANDLE hStdout;
	CONSOLE_SCREEN_BUFFER_INFO csbiInfo;

	hStdout = GetStdHandle(STD_OUTPUT_HANDLE);
	if (hStdout == INVALID_HANDLE_VALUE)
	{
		g_wOldColorAttrs = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE;
		return;
	}

	ZeroMemory(&csbiInfo, sizeof(CONSOLE_SCREEN_BUFFER_INFO));
	if (!GetConsoleScreenBufferInfo(hStdout, &csbiInfo))
	{
		g_wOldColorAttrs = FOREGROUND_RED | FOREGROUND_GREEN | FOREGROUND_BLUE;
		return;
	}

	g_wOldColorAttrs = csbiInfo.wAttributes;
}

// Restore color before exit

void RestoreColor(void)
{
	HANDLE hStdout;

	hStdout = GetStdHandle(STD_OUTPUT_HANDLE);
	if (hStdout != INVALID_HANDLE_VALUE)
	{
		SetConsoleTextAttribute(hStdout, g_wOldColorAttrs);
	}
}

// Handle the CTRL signals

BOOL CtrlHandler(DWORD fdwCtrlType)
{
	RestoreColor();

	return FALSE;  // Continue generic ctrl handler (exit)
}

//**********************************************************
// Program entry point

int main(int argc, char* argv[])
{
	bool foundex;
	char* usage =
		(char*)"mgrep 2.22\n"
		"\n"
		"Usage: mgrep [-c] [-d|-h] [-i] [-l] [-mX] [-n] [-r] [-s] [-t]\n"
		"             <search strings> <file pattern> [exclude file/dir patterns]\n"
		"\n"
		"-b: Maximum line buffer size to print (in bytes, default 10 kB).\n"
		"-c: Disable DOS charset conversion.\n"
		"-d: Escape ascii values, dec (\\ddd).\n"
		"-f: Always print first row of file.\n"
		"-h: Escape ascii values, hex (\\hh).\n"
		"-i: Case sensitive.\n"
		"-l: Print only filenames, not content.\n"
		"-m: Maximum file size to search (in bytes, default 16 MB).\n"
		"-n: Print line numbers (1-indexed).\n"
		"-r: Recurse subdirectories.\n"
		"-s: Show statistics.\n"
		"-t: Trim whitespace on output.\n"
		"\n"
		"Example: mgrep -c -i -l -m1000000 -r -s text1 text2 text3\n"
		"               d:\\files\\*.j* -*.jpg -*.jpeg -*.js\n";
	char* p;

	SaveColor();
	SetConsoleCtrlHandler((PHANDLER_ROUTINE)CtrlHandler, TRUE);


	if (argc < 3)
	{
		print_color(usage, 0);
		return 1;
	}

	g_tokens = argc - 2;
	g_aszSearch = (unsigned char**)(argv + 1);


	set_options();


	// Remove exclude patterns from search token array
	g_ExcludePatterns = 0;
	foundex = true;
	while (foundex && g_tokens > 0)
	{
		foundex = false;
		p = argv[argc - 1 - g_ExcludePatterns];
		if (p[0] == '-' && p[1])
		{
			g_tokens--;
			g_ExcludePatterns++;
			g_aszExcludePatterns = argv + argc - g_ExcludePatterns;
			foundex = true;
		}
	}

	if (g_tokens < 1)
	{
		print_color(usage, 0);
		return 1;
	}

	if (g_escape_dec && g_escape_hex)
	{
		print_color((char*)"Error: Option '-d' and '-h' cannot be used together.\n", FOREGROUND_RED | FOREGROUND_INTENSITY);
		return 1;
	}

	if (g_tokens < 1)
	{
		print_color(usage, 0);
		return 1;
	}

	rewrite_tokens();

#ifdef _DEBUG
	for (unsigned i = 0; i < 10; i++)
	{
		if (*g_pflags[i])
			printf("Flag '%c': %u\n", g_flags[i], *g_pflags[i]);
	}
	for (unsigned i = 0; i < g_tokens; i++)
	{
		printf("Search: %u '", g_tokens_lengths[i]);

		for (unsigned j = 0; j < g_tokens_lengths[i]; j++)
		{
			if (g_aszSearch[i][j] >= 32)
			{
				char buf[2];
				sprintf(buf, "%c", g_aszSearch[i][j]);
				print_color(buf, 0);
			}
			else
			{
				print_color((char*)".", FOREGROUND_RED | FOREGROUND_INTENSITY);
			}
		}

		print_color((char*)"'\n", 0);
	}
	for (unsigned i = 0; i < g_ExcludePatterns; i++)
	{
		printf("Exclude: '%s'\n", g_aszExcludePatterns[i]);
	}
#endif

	g_searched_files = g_searched_dirs = g_searched_bytes = 0;
	g_excluded_files = g_excluded_dirs = g_excluded_bytes = 0;
	g_found_files = g_found_matches = 0;

	g_time1 = GetTickCount();

	// Recurse path
	process_dir(argv[argc - 1 - g_ExcludePatterns]);

	g_time2 = GetTickCount();

	delete[] g_tokens_lengths;

	show_statistics();

	RestoreColor();

	return 0;
}

//**********************************************************
