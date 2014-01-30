#include <windows.h>
#include <wtsapi32.h>
#include <stdio.h>

void main(int argc, char *argv[])
{
	PWTS_SESSION_INFO_1 pSessionInfos;
	DWORD level;
	DWORD sessioncount;

	level = 1;
	BOOL result = WTSEnumerateSessionsEx(WTS_CURRENT_SERVER_HANDLE, &level, 0, &pSessionInfos, &sessioncount);
	if (result)
	{
		printf("Found %d sessions.\n", sessioncount);

		for (unsigned i=0; i<sessioncount; i++)
		{
			wchar_t ComputerName[1000];
			DWORD size = 1000;
			if (!GetComputerNameEx(ComputerNameDnsHostname, ComputerName, &size))
			{
				wcscmp(ComputerName, TEXT("UNKNOWN"));
			}

			if (!*(pSessionInfos[i].pSessionName) ||
				(wcscmp(pSessionInfos[i].pSessionName, L"RDP-Tcp") && !wcsncmp(pSessionInfos[i].pSessionName, L"RDP-", 4)) ||
				!wcsncmp(pSessionInfos[i].pSessionName, L"ICA-", 4))
			{
				printf("%S: Kicking: '%S', '%S'\n",
					ComputerName,
					pSessionInfos[i].pSessionName? pSessionInfos[i].pSessionName: TEXT("-"),
					pSessionInfos[i].pUserName? pSessionInfos[i].pUserName: TEXT("-"));
				if (argc<2)
				{
					WTSLogoffSession(WTS_CURRENT_SERVER_HANDLE, pSessionInfos[i].SessionId, TRUE);
				}
			}
			else
			{
				printf("%S: Not kicking: '%S', '%S'\n",
					ComputerName,
					pSessionInfos[i].pSessionName? pSessionInfos[i].pSessionName: TEXT("-"),
					pSessionInfos[i].pUserName? pSessionInfos[i].pUserName: TEXT("-"));
			}
		}

		WTSFreeMemory(pSessionInfos);
	}
	else
	{
		printf("Couldn't enumerate rdp sessions.\n");
	}
}
