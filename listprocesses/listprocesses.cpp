#include <windows.h>
#include <stdio.h>
#include <tchar.h>
#include <psapi.h>

void PrintProcessNameAndID(DWORD id);

int main(void)
{
	DWORD processes[1024], needed, processcount;
	unsigned int i;

	if (!EnumProcesses(processes, sizeof(processes), &needed))
	{
		return 1;
	}

	processcount = needed / sizeof(DWORD);
	for (i = 0; i < processcount; i++)
	{
		if (processes[i])
		{
			PrintProcessNameAndID(processes[i]);
		}
	}

	return 0;
}

void PrintProcessNameAndID(DWORD id)
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

	printf("%S  (PID: %u)\n", processname, id);

	CloseHandle(process);
}
