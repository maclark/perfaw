// not that casey does some platform independent stuff that i, max, am leaving out
#if !_WIN32
cout << "not on win32\n";
#endif



#include <intrin.h>
#include <windows.h>

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
