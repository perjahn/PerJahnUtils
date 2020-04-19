#include <windows.h>
#include <wtsapi32.h>
#include <stdio.h>
#include <string.h>

int main(int argc, char* argv[])
{
	PWTS_SESSION_INFO_1 pSessionInfos;
	DWORD level;
	DWORD sessioncount;

	level = 1;
	BOOL result = WTSEnumerateSessionsEx(WTS_CURRENT_SERVER_HANDLE, &level, 0, &pSessionInfos, &sessioncount);
	if (result)
	{
		printf("Found %d sessions.\n", sessioncount);

		for (unsigned i = 0; i < sessioncount; i++)
		{
			char ComputerName[1000];
			DWORD size = 1000;
			if (!GetComputerNameEx(ComputerNameDnsHostname, ComputerName, &size))
			{
				strcmp(ComputerName, TEXT("UNKNOWN"));
			}

			if (!*(pSessionInfos[i].pSessionName) ||
				(strcmp(pSessionInfos[i].pSessionName, "RDP-Tcp") && !strncmp(pSessionInfos[i].pSessionName, "RDP-", 4)) ||
				!strncmp(pSessionInfos[i].pSessionName, "ICA-", 4))
			{
				printf("%s: Kicking: '%s', '%s'\n",
					ComputerName,
					pSessionInfos[i].pSessionName ? pSessionInfos[i].pSessionName : TEXT("-"),
					pSessionInfos[i].pUserName ? pSessionInfos[i].pUserName : TEXT("-"));
				if (argc < 2)
				{
					result = WTSLogoffSession(WTS_CURRENT_SERVER_HANDLE, pSessionInfos[i].SessionId, TRUE);
					if (!result)
					{
						printf("Couldn't kick: %d.\n", GetLastError());
					}
				}
			}
			else
			{
				printf("%s: Not kicking: '%s', '%s'\n",
					ComputerName,
					pSessionInfos[i].pSessionName ? pSessionInfos[i].pSessionName : TEXT("-"),
					pSessionInfos[i].pUserName ? pSessionInfos[i].pUserName : TEXT("-"));
			}
		}

		WTSFreeMemory(pSessionInfos);
	}
	else
	{
		printf("Couldn't enumerate rdp sessions.\n");
	}

	return 0;
}
