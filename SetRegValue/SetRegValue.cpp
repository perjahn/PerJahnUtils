// SetRegValue.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"


int main()
{
    return 0;
}


            string regpath = @"HKEY_CURRENT_USER\Software\Sysinternals\Handle";

            Log("Setting reg value: '" + regpath + @"\EulaAccepted'");
            Registry.SetValue(regpath, "EulaAccepted", 1, RegistryValueKind.DWord);