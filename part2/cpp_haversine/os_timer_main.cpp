// get frequency (how many timer ticks per second)
// get elapsed time that exceed frequency
// print the values, convert to seconds

#include <stdio.h>
#include <stdint.h>

typedef uint64_t u64; 
typedef double f64;

#include "platform_metrics.cpp"

int main(int ArgCount, char **Args) // arg0 is "os_timer_main.cpp"
{
    u64 Duration = 1000;
    if(ArgCount == 2) 
    {
        Duration = atoi(Args[1]);
        printf("duration %d\n", Duration);
    }

    u64 OSFreq = GetOSTimerFreq();
    printf("     OS freq: %llu\n", OSFreq);

    u64 CPUStart = ReadCPUTimer(); // this is __rdtsc
    u64 OSStart = ReadOSTimer();
    u64 OSEnd = 0;
    u64 OSElapsed = 0;

    while (OSElapsed < Duration)
    {
        OSEnd = ReadOSTimer();
        OSElapsed = OSEnd - OSStart;        
    }

    u64 CPUEnd = ReadCPUTimer();
    u64 CPUElapsed = CPUEnd - CPUStart;
    
    u64 CPUFreq = 0;
    if (OSElapsed)
    {
        CPUFreq = CPUElapsed * OSFreq / OSElapsed;
    }


    printf("    OS Timer: %llu -> %llu = %llu elapsed \n", OSStart, OSEnd, OSElapsed);     
    printf("  OS Seconds: %.4f\n", (f64)OSElapsed/(f64)OSFreq);

    printf("   CPU Timer: %llu -> %llu = %llu elapsed \n", CPUStart, CPUEnd, CPUElapsed);     
    printf("    CPU Freq: %llu (guessed)\n", CPUFreq);
}
