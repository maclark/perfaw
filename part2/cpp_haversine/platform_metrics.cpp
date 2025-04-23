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

