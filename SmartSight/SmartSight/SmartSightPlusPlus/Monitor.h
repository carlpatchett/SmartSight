/**
 * The monitor class for SmartSight.
 */
class Monitor
{
public:
	Monitor();

	typedef void(__stdcall* PFN_MYCALLBACK)();

	int __stdcall StartMonitor();
	int __stdcall StopMonitor();

	Monitor* CreateNewMonitor();
private:
	bool mMonitorRunning;
};