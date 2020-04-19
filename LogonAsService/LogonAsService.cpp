#include <windows.h>
#include <stdio.h>
#include <string.h>
#include <ntsecapi.h>

int main(int argc, char* argv[])
{
	if (argc != 2)
	{
		printf("Usage: LogonAsService <accountname>\n");
		return 0;
	}

	char* AccountName = argv[1];


	LSA_HANDLE handle;
	LSA_OBJECT_ATTRIBUTES attributes;
	NTSTATUS status;

	ZeroMemory(&attributes, sizeof(LSA_OBJECT_ATTRIBUTES));


	if ((status = LsaOpenPolicy(NULL, &attributes, MAXIMUM_ALLOWED, &handle)) != ERROR_SUCCESS)
	{
		printf("Couldn't open policy: %d\n", status);
		return 1;
	}


	char* SystemName = NULL;
	unsigned char buf[1000];
	SID* psid = (SID*)buf;
	DWORD sidsize = 1000;
	char ReferencedDomainName[1000];
	DWORD cchReferencedDomainName = 1000;
	SID_NAME_USE eUse;

	ZeroMemory(psid, 1000);

	char* backslash = wcschr(AccountName, '\\');
	char* at = wcschr(AccountName, '@');
	if (backslash)
	{
		SystemName = AccountName;
		*backslash = 0;
		AccountName = backslash + 1;
	}
	else if (at)
	{
		SystemName = at + 1;
		*at = 0;
	}

	if (!LookupAccountName(SystemName, AccountName, psid, &sidsize, ReferencedDomainName, &cchReferencedDomainName, &eUse))
	{
		DWORD error = GetLastError();
		LsaClose(handle);

		if (SystemName)
		{
			printf("Couldn't lookup account name: SystemName: '%S', AccountName: '%S', Error: %d\n",
				SystemName, AccountName, error);
		}
		else
		{
			printf("Couldn't lookup account name: SystemName: <null>, AccountName: '%S', Error: %d\n",
				AccountName, error);
		}
		return 2;
	}


	LSA_UNICODE_STRING rights;
	rights.Buffer = SE_SERVICE_LOGON_NAME;
	rights.Length = wcslen(rights.Buffer) * 2;
	rights.MaximumLength = (rights.Length + 1) * 2;

	if ((status = LsaAddAccountRights(handle, psid, &rights, 1)) != ERROR_SUCCESS)
	{
		ULONG result = LsaNtStatusToWinError(status);
		LsaClose(handle);
		printf("Couldn't add account rights: %d\n", result);
		return 3;
	}

	LsaClose(handle);

	return 0;
}
