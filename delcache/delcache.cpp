#include <windows.h>
#include <wininet.h>
#include <stdio.h>

void main(void)
{
	DWORD t1 = GetTickCount();

	HANDLE hFind;
	DWORD size;
	INTERNET_CACHE_ENTRY_INFO icei[100];
	int count = 0;

	size = sizeof(INTERNET_CACHE_ENTRY_INFO)*100;
	if(hFind=FindFirstUrlCacheEntry(NULL, icei, &size))
	{
		do
		{
			DeleteUrlCacheEntry(icei[0].lpszSourceUrlName);
			count++;

			size = sizeof(INTERNET_CACHE_ENTRY_INFO)*100;
		}
		while(FindNextUrlCacheEntry(hFind, icei, &size));
	
		FindCloseUrlCache(hFind);
	}

	DWORD t2 = GetTickCount();

	printf("Deleted %d objects in %d.%d seconds.\n", count, (t2-t1)/1000, (t2-t1)%1000);

	return;
}
