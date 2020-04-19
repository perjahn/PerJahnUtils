//**********************************************************

#include <windows.h>
#include <stdio.h>
#include <string.h>

//**********************************************************

extern bool g_simulate;
extern bool g_verbose;
extern int g_counts[4];

char g_szLog[1000];

//**********************************************************

unsigned FixProjectBuf(unsigned char* buf, unsigned size);
unsigned FixSolutionBuf(unsigned char* buf, unsigned size);
void Log(char* szMessage, unsigned level);

//**********************************************************

void FixAttrib(char* szPath)
{
	DWORD attr = GetFileAttributes(szPath);
	if (attr & FILE_ATTRIBUTE_READONLY)
	{
		attr &= ~FILE_ATTRIBUTE_READONLY;
		sprintf(g_szLog, "SetFileAttribute: '%s'\n", szPath);
		Log(g_szLog, 0);
		g_counts[0]++;

		if (!g_simulate)
		{
			SetFileAttributes(szPath, 0);
		}
	}
}
//**********************************************************

void MyDeleteFile(char* szFileName)
{
	sprintf(g_szLog, "DeleteFile: '%s'\n", szFileName);
	Log(g_szLog, 0);
	g_counts[1]++;

	if (!g_simulate)
	{
		DeleteFile(szFileName);
	}
}

//**********************************************************

void FixProjectFile(char* szFileName)
{
	FILE* fh;

	if (!(fh = fopen(szFileName, "rb")))
	{
		sprintf(g_szLog, "Couldn't read file: '%s'.\n", szFileName);
		Log(g_szLog, 1);
		return;
	}

	fseek(fh, 0, SEEK_END);
	unsigned size = ftell(fh);

	unsigned char* buf = new unsigned char[size];
	if (!buf)
	{
		fclose(fh);
		sprintf(g_szLog, "Out of memory: %u bytes.\n", size);
		Log(g_szLog, 1);
		return;
	}

	fseek(fh, 0, SEEK_SET);
	fread(buf, size, 1, fh);
	fclose(fh);


	size = FixProjectBuf(buf, size);


	if (size)
	{
		sprintf(g_szLog, "FixProject: '%s'\n", szFileName);
		Log(g_szLog, 0);
		g_counts[2]++;

		if (!g_simulate)
		{
			if (!(fh = fopen(szFileName, "wb")))
			{
				delete[] buf;
				sprintf(g_szLog, "Couldn't write to file: '%s'.\n", szFileName);
				Log(g_szLog, 1);
				return;
			}

			fwrite(buf, size, 1, fh);
			fclose(fh);
		}
	}

	delete[] buf;

	return;
}

//**********************************************************

unsigned FixProjectBuf(unsigned char* buf, unsigned size)
{
	char* finds[] =
	{
		(char*)"    <SccProjectName>SAK</SccProjectName>\r\n",
		(char*)"    <SccLocalPath>SAK</SccLocalPath>\r\n",
		(char*)"    <SccAuxPath>SAK</SccAuxPath>\r\n",
		(char*)"    <SccProvider>SAK</SccProvider>\r\n",
		(char*)"\"SccAuxPath\" = \"8:\"\r\n",
		(char*)"\"SccAuxPath\" = \"8:SAK\"\r\n",
		(char*)"\"SccLocalPath\" = \"8:\"\r\n",
		(char*)"\"SccLocalPath\" = \"8:SAK\"\r\n",
		(char*)"\"SccProjectName\" = \"8:\"\r\n",
		(char*)"\"SccProjectName\" = \"8:SAK\"\r\n",
		(char*)"\"SccProvider\" = \"8:\"\r\n",
		(char*)"\"SccProvider\" = \"8:SAK\"\r\n",
		NULL
	};

	int findcount;
	for (findcount = 0; finds[findcount]; findcount++)
		;
	unsigned* sizes = new unsigned[findcount];

	for (int i = 0; i < findcount; i++)
	{
		sizes[i] = (unsigned)strlen(finds[i]);
	}

	unsigned char* p1, * p2;

	for (p1 = p2 = buf; p1 < buf + size;)
	{
		bool found = false;

		for (int i = 0; i < findcount && !found; i++)
		{
			if (p1 < buf + size - sizes[i] && !memcmp(p1, finds[i], sizes[i]))
			{
				p1 += sizes[i];
				found = true;
			}
		}

		if (!found)
		{
			*p2 = *p1;
			p1++;
			p2++;
		}
	}

	delete[] sizes;

	if (p1 != p2)
		return (unsigned)(p2 - buf);

	return 0;
}

//**********************************************************

void FixSolutionFile(char* szFileName)
{
	FILE* fh;

	if (!(fh = fopen(szFileName, "rb")))
	{
		sprintf(g_szLog, "Couldn't read file: '%s'.\n", szFileName);
		Log(g_szLog, 1);
		return;
	}

	fseek(fh, 0, SEEK_END);
	unsigned size = ftell(fh);

	unsigned char* buf = new unsigned char[size];
	if (!buf)
	{
		fclose(fh);
		sprintf(g_szLog, "Out of memory: %u bytes.\n", size);
		Log(g_szLog, 1);
		return;
	}

	fseek(fh, 0, SEEK_SET);
	fread(buf, size, 1, fh);
	fclose(fh);


	size = FixSolutionBuf(buf, size);


	if (size)
	{
		sprintf(g_szLog, "FixSolution: '%s'\n", szFileName);
		Log(g_szLog, 0);
		g_counts[3]++;

		if (!g_simulate)
		{
			if (!(fh = fopen(szFileName, "wb")))
			{
				delete[] buf;
				sprintf(g_szLog, "Couldn't write to file: '%s'.\n", szFileName);
				Log(g_szLog, 1);
				return;
			}

			fwrite(buf, size, 1, fh);
			fclose(fh);
		}
	}

	delete[] buf;

	return;
}

//**********************************************************

unsigned FixSolutionBuf(unsigned char* buf, unsigned size)
{
	unsigned char* p1, * p2, * pgs;
	bool scc_section;

	pgs = NULL;
	for (p1 = p2 = buf; p1 < buf + size; p1++, p2++)
	{
		if (p1 < buf + size - 15 && !memcmp(p1, "\n\tGlobalSection", 15))
		{
			// Found start of a section
			pgs = p1 + 1;
			scc_section = false;
		}

		if (p1 < buf + size - 6 && !memcmp(p1, "\n\t\tScc", 6))
		{
			// This is a ss/tfs section
			scc_section = true;
		}

		if (p1 < buf + size - 20 && !memcmp(p1, "\n\tEndGlobalSection\r\n", 20) && pgs && scc_section)
		{
			// Remove whole section
			p1 += 20;
			p2 += 20;
			p2 -= (p1 - pgs);

			pgs = NULL;
			scc_section = false;
		}
		else
		{
			*p2 = *p1;
		}
	}

	if (p1 != p2)
		return (unsigned)(p2 - buf);

	return 0;
}

//**********************************************************
// level: 0=normal, 1=error
void Log(char* szMessage, unsigned level)
{
	if (level == 0)
	{
		if (g_verbose)
		{
			printf("%s", szMessage);
		}
	}
	if (level == 1)
	{
		printf("%s", szMessage);
	}

	return;
}

//**********************************************************
