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
    if(ArgCount == 2) 
    {
        printf("arg0: %s\n", Args[0]);
        printf("arg1: %s\n", Args[1]);
    }


    u64 Freq = GetOSTimerFreq();
    printf("     OS freq: %llu\n", Freq);

    u64 CPUStart = ReadCPUTimer(); // this is __rdtsc
    u64 Start = ReadOSTimer();
    u64 End = 0;
    u64 Elapsed = 0;
    while (Elapsed < Freq)
    {
        End = ReadOSTimer();
        Elapsed = End - Start;        
    }  

    u64 CPUEnd = ReadCPUTimer();
    u64 CPUElapsed = CPUEnd - CPUStart;
    
    printf("    OS Timer: %llu -> %llu = %llu elapsed \n", Start, End, Elapsed);     
    printf("  OS Seconds: %.4f\n", (f64)Elapsed/(f64)Freq);

    printf("   CPU Timer: %llu -> %llu = %llu elapsed \n", CPUStart, CPUEnd, CPUElapsed);     
}
