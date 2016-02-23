#include <windows.h>
#include <wincred.h>
#include <stdio.h>

void main(void)
{
	unsigned long count;
	CREDENTIAL **creds;
	if (!CredEnumerate(NULL, 0, &count, &creds))
	{
		printf("Couldn't enumerate creds: %lu\n", GetLastError());
		return;
	}

	printf("Cred count: %d\n", count);

	for (unsigned long i = 0; i < count; i++)
	{
		printf("Username: '%s', password: '", creds[i]->UserName);

		for (unsigned long j = 0; j < creds[i]->CredentialBlobSize; j += sizeof(wchar_t))
		{
			if (creds[i]->CredentialBlob[j] == 0)
			{
				printf(".");
			}
			else
			{
				printf("%C", *(wchar_t*)(creds[i]->CredentialBlob + j));
			}
		}

		printf("' (%d bytes)\n", creds[i]->CredentialBlobSize);
	}

	CredFree(creds);

	getchar();
}
