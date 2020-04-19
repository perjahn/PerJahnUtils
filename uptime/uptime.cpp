#include <windows.h>
#include <stdio.h>

int main(void)
{
	unsigned days, hours, minutes, seconds;
	DWORD uptime = GetTickCount();
	days = uptime/1000/3600/24;
	hours = uptime/1000/3600-days*24;
	minutes = uptime/1000/60-hours*60-days*24*60;
	seconds = uptime/1000-minutes*60-hours*3600-days*24*3600;

	printf("%u days, %uh, %um, %us\n", days, hours, minutes, seconds);

	return 0;
}
