// not that casey does some platform independent stuff that i, max, am leaving out
#if !_WIN32
cout << "not on win32\n";
#endif



#include <intrin.h>
#include <windows.h>
#include <psapi.h>

struct os_metrics 
{
    b32 Initialized;
    HANDLE ProcessHandle;
};
os_metrics GlobalMetrics;

static u64 GetOSTimerFreq(void)
{
    LARGE_INTEGER Freq; // is a union, a 64-bit, but u can get lo and hi
    QueryPerformanceFrequency(&Freq);
    return Freq.QuadPart;
}

static u64 ReadOSTimer(void)
{
    LARGE_INTEGER Value;
    QueryPerformanceCounter(&Value);
    return Value.QuadPart;
}

static u64 ReadOSPageFaultCount(void)
{
    PROCESS_MEMORY_EX MemoryCounters = {};
    MemoryCounters.cb = sizeof(MemoryCounters);
    // Looks like we're casting _EX to its parent type maybe?
    GetProcessMemoryInfo(GlobalMetrics.ProcessHandle, (PROCESS_MEMORY_COUNTERS *)&MemoryCounters, sizeof(MemoryCounters);

    u64 Result = MemoryCounters.PageFaultCount;
    return Result;
}

static void InitializeOSMetrics(void)
{
    if(!GlobalMetrics.Initialized)
    {
        GlobalMetrics.Initialized = true;
        GlobaleMetrics.ProcessHandle = OpenProcess(PROCSES_QUERY_INFORMATION | PROCSES_VM_READ, FALSE, GetCurrentProcessId());
    }
}


// casey note: this does not need to be "inline", it could just "static"
// because compilers will inline it anyway. But compilers will warn about
// static functions that aren't used. So "inline" i just the simplest way
// to tell them to stop complaining about that.
inline u64 ReadCPUTimer(void)
{
    // casey note: if you were on ARM, you would need to replace __rdtsc
    // with one of their performance counter read instructions, depending
    // on which ones are available on your platform
    return __rdtsc();
}


static u64 EstimateCPUFrequency(void)
{
    u64 HowLongToWait = 100; 
    u64 OSFreq = GetOSTimerFreq();

    u64 CPUStart = ReadCPUTimer();
    u64 OSStart = ReadOSTimer();
    u64 OSElapsed = 0;
    u64 OSEnd = 0;
    u64 OSWaitTime = HowLongToWait * OSFreq / 1000;

    while(OSElapsed < OSWaitTime)
    {
        OSEnd = ReadOSTimer();
        OSElapsed = OSEnd - OSStart;
    }   

    u64 CPUElapsed = ReadCPUTimer() - CPUStart;  

    u64 CPUFreq = 0;
    if (OSElapsed)
    {
        CPUFreq = CPUElapsed * OSFreq / OSElapsed;
    }    

    return CPUFreq;
}
