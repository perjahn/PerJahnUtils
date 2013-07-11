#include <windows.h>
#include <stdio.h>

void main(void)
{
	unsigned days, hours, minutes, seconds;
	days = GetTickCount()/1000/3600/24;
	hours = GetTickCount()/1000/3600-days*24;
	minutes = GetTickCount()/1000/60-hours*60-days*24*60;
	seconds = GetTickCount()/1000-minutes*60-hours*3600-days*24*3600;

	printf("%u days, %uh, %um, %us\n", days, hours, minutes, seconds);
}
