#include <windows.h>
#include <stdio.h>
#include <stdlib.h>
#include <tchar.h>
#include <psapi.h>

void PrintProcessNameAndID(DWORD id, char *logfile);
void Log(char *logfile, char *message);

DWORD _prevprocesses[1024];
DWORD _prevprocesscount = 0;

int compare(const void *arg1, const void *arg2)
{
	return *(DWORD*)arg1 - *(DWORD*)arg2;
}

int ListProcesses(char *logfile)
{
	DWORD processes[1024], needed, processcount;
	unsigned int i, j;

	if (!EnumProcesses(processes, sizeof(processes), &needed))
	{
		return 1;
	}
	processcount = needed / sizeof(DWORD);

	char message[1000];
	sprintf(message, "Found %u processes.", processcount);
	Log(logfile, message);

	qsort(processes, processcount, sizeof(DWORD), compare);

	Log(logfile, "Added:");
	for (i = 0; i < processcount; i++)
	{
		bool found = false;
		for (j = 0; j < _prevprocesscount; j++)
		{
			if (processes[i] == _prevprocesses[j])
			{
				found = true;
			}
		}
		if (!found)
		{
			PrintProcessNameAndID(processes[i], logfile);
		}
	}

	Log(logfile, "Removed:");
	for (i = 0; i < _prevprocesscount; i++)
	{
		bool found = false;
		for (j = 0; j < processcount; j++)
		{
			if (processes[i] == _prevprocesses[j])
			{
				found = true;
			}
		}
		if (!found)
		{
			PrintProcessNameAndID(_prevprocesses[j], logfile);
		}
	}

	memcpy(_prevprocesses, processes, sizeof(DWORD) * processcount);
	_prevprocesscount = processcount;

	return 0;
}

void PrintProcessNameAndID(DWORD id, char *logfile)
{
	TCHAR processname[MAX_PATH] = TEXT("<unknown>");

	HANDLE process = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, FALSE, id);

	if (process)
	{
		HMODULE module;
		DWORD needed;

		if (EnumProcessModules(process, &module, sizeof(module), &needed))
		{
			GetModuleBaseName(process, module, processname, sizeof(processname) / sizeof(TCHAR));
		}
	}

	char message[1000];
	sprintf(message, "%s  (PID: %u)", processname, id);
	Log(logfile, message);

	CloseHandle(process);
}

void Log(char *logfile, char *message)
{
	FILE *fh;
	if (fh = fopen(logfile, "a"))
	{
		printf("%s\n", message);
	}
}
