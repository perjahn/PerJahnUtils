#include <windows.h>
#include <assert.h>
#include <memory>
#include <stdio.h>
#include <strsafe.h>

class Service
{
public:
	Service(char* servicename, BOOL fCanStop = TRUE, BOOL fCanShutdown = TRUE, BOOL fCanPauseContinue = FALSE);
	~Service(void);
	static BOOL Run(Service& service);
	void Stop();

protected:
	void SetStatus(DWORD currentstate, DWORD dwWin32ExitCode = NO_ERROR, DWORD dwWaitHint = 0);
	void WriteEventLogEntry(char* message, WORD type);
	void WriteErrorLogEntry(char* function, DWORD error = GetLastError());
	void ServiceWorkerThread(void);

private:
	static void WINAPI ServiceMain(DWORD argc, char* argv[]);
	static void WINAPI ServiceCtrlHandler(DWORD ctrl);
	void Start(DWORD argc, char* argv[]);
	void Shutdown();
	static Service* s_service;
	char* _name;
	SERVICE_STATUS _status;
	SERVICE_STATUS_HANDLE _statusHandle;
	BOOL _stopping;
	HANDLE _hStoppedEvent;
};

Service* Service::s_service = NULL;

class CThreadPool
{
public:
	template <typename T>
	static void QueueUserWorkItem(void (T::* function)(void), T* object, ULONG flags = WT_EXECUTELONGFUNCTION)
	{
		typedef std::pair<void (T::*)(), T*> CallbackType;
		std::auto_ptr<CallbackType> p(new CallbackType(function, object));

		if (::QueueUserWorkItem(ThreadProc<T>, p.get(), flags))
		{
			p.release();
		}
		else
		{
			throw GetLastError();
		}
	}

private:
	template <typename T>
	static DWORD WINAPI ThreadProc(PVOID context)
	{
		typedef std::pair<void (T::*)(), T*> CallbackType;

		std::auto_ptr<CallbackType> p(static_cast<CallbackType*>(context));

		(p->second->*p->first)();
		return 0;
	}
};

Service::Service(char* servicename, BOOL fCanStop, BOOL fCanShutdown, BOOL fCanPauseContinue)
{
	_name = (servicename == NULL) ? (char*)"" : servicename;
	_statusHandle = NULL;
	_status.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
	_status.dwCurrentState = SERVICE_START_PENDING;

	DWORD dwControlsAccepted = 0;
	if (fCanStop)
		dwControlsAccepted |= SERVICE_ACCEPT_STOP;
	if (fCanShutdown)
		dwControlsAccepted |= SERVICE_ACCEPT_SHUTDOWN;
	if (fCanPauseContinue)
		dwControlsAccepted |= SERVICE_ACCEPT_PAUSE_CONTINUE;
	_status.dwControlsAccepted = dwControlsAccepted;

	_status.dwWin32ExitCode = NO_ERROR;
	_status.dwServiceSpecificExitCode = 0;
	_status.dwCheckPoint = 0;
	_status.dwWaitHint = 0;

	_stopping = FALSE;
	_hStoppedEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	if (_hStoppedEvent == NULL)
	{
		throw GetLastError();
	}
}

Service::~Service(void)
{
	if (_hStoppedEvent)
	{
		CloseHandle(_hStoppedEvent);
		_hStoppedEvent = NULL;
	}
}

void Service::Start(DWORD argc, char* argv[])
{
	try
	{
		SetStatus(SERVICE_START_PENDING);
		WriteEventLogEntry((char*)"ListProcesses Start", EVENTLOG_INFORMATION_TYPE);
		CThreadPool::QueueUserWorkItem(&Service::ServiceWorkerThread, this);
		SetStatus(SERVICE_RUNNING);
	}
	catch (DWORD dwError)
	{
		WriteErrorLogEntry((char*)"ListProcesses Start", dwError);
		SetStatus(SERVICE_STOPPED, dwError);
	}
	catch (...)
	{
		WriteEventLogEntry((char*)"ListProcesses failed to start.", EVENTLOG_ERROR_TYPE);
		SetStatus(SERVICE_STOPPED);
	}
}

void Service::Stop()
{
	DWORD dwOriginalState = _status.dwCurrentState;
	try
	{
		SetStatus(SERVICE_STOP_PENDING);
		WriteEventLogEntry((char*)"ListProcesses Stop", EVENTLOG_INFORMATION_TYPE);

		_stopping = TRUE;
		if (WaitForSingleObject(_hStoppedEvent, INFINITE) != WAIT_OBJECT_0)
		{
			throw GetLastError();
		}
		SetStatus(SERVICE_STOPPED);
	}
	catch (DWORD dwError)
	{
		WriteErrorLogEntry((char*)"ListProcesses Stop", dwError);
		SetStatus(dwOriginalState);
	}
	catch (...)
	{
		WriteEventLogEntry((char*)"ListProcesses failed to stop.", EVENTLOG_ERROR_TYPE);
		SetStatus(dwOriginalState);
	}
}

BOOL Service::Run(Service& service)
{
	s_service = &service;

	SERVICE_TABLE_ENTRY serviceTable[] =
	{
		{ service._name, ServiceMain },
		{ NULL, NULL }
	};

	return StartServiceCtrlDispatcher(serviceTable);
}

void WINAPI Service::ServiceMain(DWORD argc, char* argv[])
{
	assert(s_service != NULL);

	s_service->_statusHandle = RegisterServiceCtrlHandler(s_service->_name, ServiceCtrlHandler);
	if (s_service->_statusHandle == NULL)
	{
		throw GetLastError();
	}

	s_service->Start(argc, argv);
}

void WINAPI Service::ServiceCtrlHandler(DWORD dwCtrl)
{
	switch (dwCtrl)
	{
	case SERVICE_CONTROL_STOP: s_service->Stop(); break;
	case SERVICE_CONTROL_SHUTDOWN: s_service->Shutdown(); break;
	case SERVICE_CONTROL_INTERROGATE: break;
	default: break;
	}
}

void Service::Shutdown()
{
	try
	{
		SetStatus(SERVICE_STOPPED);
	}
	catch (DWORD dwError)
	{
		WriteErrorLogEntry((char*)"ListProcesses Shutdown", dwError);
	}
	catch (...)
	{
		WriteEventLogEntry((char*)"ListProcesses failed to shut down.", EVENTLOG_ERROR_TYPE);
	}
}

void Service::SetStatus(DWORD dwCurrentState, DWORD dwWin32ExitCode, DWORD dwWaitHint)
{
	static DWORD dwCheckPoint = 1;

	_status.dwCurrentState = dwCurrentState;
	_status.dwWin32ExitCode = dwWin32ExitCode;
	_status.dwWaitHint = dwWaitHint;
	_status.dwCheckPoint = ((dwCurrentState == SERVICE_RUNNING) || (dwCurrentState == SERVICE_STOPPED)) ? 0 : dwCheckPoint++;

	SetServiceStatus(_statusHandle, &_status);
}

void Service::WriteEventLogEntry(char* message, WORD type)
{
	HANDLE hEventSource = NULL;
	const char* strings[2] = { NULL, NULL };

	hEventSource = RegisterEventSource(NULL, _name);
	if (hEventSource)
	{
		strings[0] = _name;
		strings[1] = message;

		ReportEvent(hEventSource, type, 0, 0, NULL, 2, 0, strings, NULL);

		DeregisterEventSource(hEventSource);
	}
}

void Service::WriteErrorLogEntry(char* function, DWORD error)
{
	char message[260];
	StringCchPrintf(message, ARRAYSIZE(message), "%s failed w/err 0x%08lx", function, error);
	WriteEventLogEntry(message, EVENTLOG_ERROR_TYPE);
}

void Service::ServiceWorkerThread(void)
{
	while (!_stopping)
	{
		char curdir[1000];
		GetCurrentDirectory(1000, curdir);

		FILE* fh;
		if (fh = fopen("C:\\test\\logfile.log", "a"))
		{
			fprintf(fh, "%d: '%s'\n", GetTickCount(), curdir);
			fclose(fh);
		}
		Sleep(2000);
	}

	SetEvent(_hStoppedEvent);
}

void InstallService(char* servicename, char* displayname, DWORD starttype, char* dependencies, char* account, char* password)
{
	char path[MAX_PATH];
	SC_HANDLE SCManager = NULL;
	SC_HANDLE service = NULL;

	if (GetModuleFileName(NULL, path, ARRAYSIZE(path)) == 0)
	{
		printf("GetModuleFileName failed w/err 0x%08lx\n", GetLastError());
		goto Cleanup;
	}

	SCManager = OpenSCManager(NULL, NULL, SC_MANAGER_CONNECT | SC_MANAGER_CREATE_SERVICE);
	if (SCManager == NULL)
	{
		printf("OpenSCManager failed w/err 0x%08lx\n", GetLastError());
		goto Cleanup;
	}

	service = CreateService(SCManager, servicename, displayname, SERVICE_QUERY_STATUS, SERVICE_WIN32_OWN_PROCESS, starttype, SERVICE_ERROR_NORMAL, path, NULL, NULL, dependencies, account, password);
	if (service == NULL)
	{
		printf("CreateService failed w/err 0x%08lx\n", GetLastError());
		goto Cleanup;
	}

	printf("%s installed.\n", servicename);

Cleanup:
	if (SCManager)
	{
		CloseServiceHandle(SCManager);
		SCManager = NULL;
	}
	if (service)
	{
		CloseServiceHandle(service);
		service = NULL;
	}
}

void UninstallService(char* servicename)
{
	SC_HANDLE schSCManager = NULL;
	SC_HANDLE schService = NULL;
	SERVICE_STATUS ssSvcStatus = {};

	schSCManager = OpenSCManager(NULL, NULL, SC_MANAGER_CONNECT);
	if (schSCManager == NULL)
	{
		printf("OpenSCManager failed w/err 0x%08lx\n", GetLastError());
		goto Cleanup;
	}

	schService = OpenService(schSCManager, servicename, SERVICE_STOP | SERVICE_QUERY_STATUS | DELETE);
	if (schService == NULL)
	{
		printf("OpenService failed w/err 0x%08lx\n", GetLastError());
		goto Cleanup;
	}

	if (ControlService(schService, SERVICE_CONTROL_STOP, &ssSvcStatus))
	{
		printf("Stopping %s.", servicename);
		Sleep(1000);

		while (QueryServiceStatus(schService, &ssSvcStatus))
		{
			if (ssSvcStatus.dwCurrentState == SERVICE_STOP_PENDING)
			{
				printf(".");
				Sleep(1000);
			}
			else
			{
				break;
			}
		}

		if (ssSvcStatus.dwCurrentState == SERVICE_STOPPED)
		{
			printf("\n%s is stopped.\n", servicename);
		}
		else
		{
			printf("\n%s failed to stop.\n", servicename);
		}
	}

	if (!DeleteService(schService))
	{
		printf("%s failed w/err 0x%08lx\n", servicename, GetLastError());
		goto Cleanup;
	}

	printf("%s is removed.\n", servicename);

Cleanup:
	if (schSCManager)
	{
		CloseServiceHandle(schSCManager);
		schSCManager = NULL;
	}
	if (schService)
	{
		CloseServiceHandle(schService);
		schService = NULL;
	}
}

int ListProcesses(char* logfile);

int main(int argc, char* argv[])
{
	char* servicename = (char*)"ListProcesses";
	char* displayname = (char*)"ListProcesses";
	DWORD starttype = SERVICE_AUTO_START;
	char* dependencies = (char*)"";
	char* account = (char*)"NT AUTHORITY\\LocalService";
	char* password = NULL;

	if ((argc > 1) && ((*argv[1] == '-' || (*argv[1] == '/'))))
	{
		if (_stricmp("install", argv[1] + 1) == 0)
		{
			InstallService(servicename, displayname, starttype, dependencies, account, password);
		}
		else if (_stricmp("remove", argv[1] + 1) == 0)
		{
			UninstallService(servicename);
		}
		else if (_stricmp("list", argv[1] + 1) == 0)
		{
			ListProcesses((char*)"C:\\test\\logfile.log");
			Sleep(5000);
			ListProcesses((char*)"C:\\test\\logfile.log");
			Sleep(5000);
			ListProcesses((char*)"C:\\test\\logfile.log");
		}
	}
	else
	{
		printf("Parameters:\n");
		printf(" -install  to install the service.\n");
		printf(" -remove   to remove the service.\n");

		Service service(servicename);
		if (!Service::Run(service))
		{
			printf("%s failed to run w/err 0x%08lx\n", servicename, GetLastError());
		}
	}

	return 0;
}
