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

unsigned FixProjectBuf(unsigned char *buf, unsigned size);
unsigned FixSolutionBuf(unsigned char *buf, unsigned size);
void Log(char *szMessage, unsigned level);

//**********************************************************

void FixAttrib(char *szPath)
{
	DWORD attr = GetFileAttributes(szPath);
	if(attr & FILE_ATTRIBUTE_READONLY)
	{
		attr &= ~FILE_ATTRIBUTE_READONLY;
		sprintf(g_szLog, "SetFileAttribute: '%s'\n", szPath);
		Log(g_szLog, 0);
		g_counts[0]++;

		if(!g_simulate)
		{
			SetFileAttributes(szPath, 0);
		}
	}
}
//**********************************************************

void MyDeleteFile(char *szFileName)
{
	sprintf(g_szLog, "DeleteFile: '%s'\n", szFileName);
	Log(g_szLog, 0);
	g_counts[1]++;

	if(!g_simulate)
	{
		DeleteFile(szFileName);
	}
}

//**********************************************************

void FixProjectFile(char *szFileName)
{
	FILE *fh;

	if(!(fh = fopen(szFileName, "rb")))
	{
		sprintf(g_szLog, "Couldn't read file (%s).\n", szFileName);
		Log(g_szLog, 1);
		return;
	}

	fseek(fh, 0, SEEK_END);
	unsigned size = ftell(fh);

	unsigned char *buf = new unsigned char[size];
	if(!buf)
	{
		fclose(fh);
		sprintf(g_szLog, "Out of memory (%u bytes).\n", size);
		Log(g_szLog, 1);
		return;
	}

	fseek(fh, 0, SEEK_SET);
	fread(buf, size, 1, fh);
	fclose(fh);


	size = FixProjectBuf(buf, size);


	if(size)
	{
		sprintf(g_szLog, "FixProject: '%s'\n", szFileName);
		Log(g_szLog, 0);
		g_counts[2]++;

		if(!g_simulate)
		{
			if(!(fh = fopen(szFileName, "wb")))
			{
				delete[] buf;
				sprintf(g_szLog, "Couldn't write to file (%s).\n", szFileName);
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

unsigned FixProjectBuf(unsigned char *buf, unsigned size)
{
	char szFind[4][100];
	unsigned sizes[4];

	strcpy(szFind[0], "    <SccProjectName>SAK</SccProjectName>\r\n");
	sizes[0] = strlen(szFind[0]);

	strcpy(szFind[1], "    <SccLocalPath>SAK</SccLocalPath>\r\n");
	sizes[1] = strlen(szFind[1]);

	strcpy(szFind[2], "    <SccAuxPath>SAK</SccAuxPath>\r\n");
	sizes[2] = strlen(szFind[2]);

	strcpy(szFind[3], "    <SccProvider>SAK</SccProvider>\r\n");
	sizes[3] = strlen(szFind[3]);

	unsigned char *p1, *p2;

	for(p1=p2=buf; p1<buf+size; )
	{
		if(p1<buf+size-sizes[0] && !memcmp(p1, szFind[0], sizes[0]))
		{
			p1+=sizes[0];
		}
		else if(p1<buf+size-sizes[1] && !memcmp(p1, szFind[1], sizes[1]))
		{
			p1+=sizes[1];
		}
		else if(p1<buf+size-sizes[2] && !memcmp(p1, szFind[2], sizes[2]))
		{
			p1+=sizes[2];
		}
		else if(p1<buf+size-sizes[3] && !memcmp(p1, szFind[3], sizes[3]))
		{
			p1+=sizes[3];
		}
		else
		{
			*p2 = *p1;
			p1++;
			p2++;
		}
	}

	if(p1!=p2)
		return p2-buf;

	return 0;
}

//**********************************************************

void FixSolutionFile(char *szFileName)
{
	FILE *fh;

	if(!(fh = fopen(szFileName, "rb")))
	{
		sprintf(g_szLog, "Couldn't read file (%s).\n", szFileName);
		Log(g_szLog, 1);
		return;
	}

	fseek(fh, 0, SEEK_END);
	unsigned size = ftell(fh);

	unsigned char *buf = new unsigned char[size];
	if(!buf)
	{
		fclose(fh);
		sprintf(g_szLog, "Out of memory (%u bytes).\n", size);
		Log(g_szLog, 1);
		return;
	}

	fseek(fh, 0, SEEK_SET);
	fread(buf, size, 1, fh);
	fclose(fh);


	size = FixSolutionBuf(buf, size);


	if(size)
	{
		sprintf(g_szLog, "FixSolution: '%s'\n", szFileName);
		Log(g_szLog, 0);
		g_counts[3]++;

		if(!g_simulate)
		{
			if(!(fh = fopen(szFileName, "wb")))
			{
				delete[] buf;
				sprintf(g_szLog, "Couldn't write to file (%s).\n", szFileName);
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
/*
AAA
	GlobalSectionBBB
		SccCCC
		SccDDD
	EndGlobalSection
FFF
*/

unsigned FixSolutionBuf(unsigned char *buf, unsigned size)
{
	unsigned char *p1, *p2, *ps;
	bool scc_section;

	ps = NULL;
	scc_section = false;

	for(p1=p2=buf; p1<buf+size; p1++)
	{
		if(p1>0 && p1<buf+size-14 && !memcmp(p1-1, "\n\tGlobalSection", 15))
		{
			// Found start of a (global) section
			ps = p1;
			scc_section = false;
			printf("A: p1:%X p2:%X\n", p1-buf, p2-buf);
		}
		if(p1>0 && p1<buf+size-15 && !memcmp(p1-1, "\n\tProjectSection", 16))
		{
			// Found start of a (project) section
			ps = p1;
			scc_section = false;
			printf("B: p1:%X p2:%X\n", p1-buf, p2-buf);
		}

		if(p1>0 && p1<buf+size-5 && !memcmp(p1-1, "\n\t\tScc", 6))
		{
			// This is a ss/tfs section
			scc_section = true;
		}

		if(p1>0 && p1<buf+size-19 && !memcmp(p1-1, "\n\tEndGlobalSection\r\n", 20) && ps && scc_section)
		{
			printf("C: p1:%X p2:%X\n", p1-buf, p2-buf);
			// Remove whole (global) section
			p1+=18;  // for loop will add 1 to this
			p2 -= (p1-ps);

			ps = NULL;
			scc_section = false;
			printf("D: p1:%d p2:%X\n", p1-buf, p2-buf);
			continue;
		}

		if(p1>0 && p1<buf+size-20 && !memcmp(p1-1, "\n\tEndProjectSection\r\n", 21) && ps && scc_section)
		{
			printf("E: p1:%X p2:%X\n", p1-buf, p2-buf);
			// Remove whole (project) section
			p1+=19;  // for loop will add 1 to this
			p2 -= (p1-ps);

			ps = NULL;
			scc_section = false;
			printf("F: p1:%X p2:%X\n", p1-buf, p2-buf);
			continue;
		}

		*p2 = *p1;
		p2++;
	}

	if(p1!=p2)
		return p2-buf;

	return 0;
}

//**********************************************************
// level: 0=normal, 1=error
void Log(char *szMessage, unsigned level)
{
	if(level==0)
	{
		if(g_verbose)
		{
			printf("%s", szMessage);
		}
	}
	if(level==1)
	{
		printf("%s", szMessage);
	}

	return;
}

//**********************************************************
