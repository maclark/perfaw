// i don't think i need this, this is for casey's bad compiler hahahaha
#define _CRT_SECURE_NO_WARNINGS

#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>
#include <math.h>
#include <sys/stat.h>

typedef uint8_t u8; 
typedef uint32_t u32;
typedef uint64_t u64;

typedef int32_t b32;

typedef float f32;
typedef double f64;

#define ArrayCount(array) (sizeof(array) / sizeof((array)[0]))

#include "listing_0108_platform_metrics.cpp"

int main(int ArgCount, char **Args)
{
    // casey's method to avoid compiler warning
    (void)&EstimateCPUFrequency;

    InitializeOSMetrics();

    if (ArgCount == 2)
    {

        u64 PageSize = 4096; // 4k
        u64 PageCount = atol(Args[1]); // "argument to long"! duh
        u64 TotalSize = PageSize * PageCount;

        for(u64 TouchCount = 0; TouchCount < PageCount; ++TouchCount)
        {
            u64 TouchSize = PageSize * TouchCount;
            u8 *Data = (u8 *)VirtualAlloc(0, TotalSize, MEM_RESERVE|MEM_COMMIT, PAGE_READWRITE);
            if(Data)
            {
                // we want to count page faults that happen after we touch a bunch of stuff
                u64 StartPageFaultCount = ReadOSPageFaultCount();
                for(u64 Index = 0; Index < TouchSize; ++Index)
                {
                    Data[TouchSize - Index - 1] = (u8)Index;
                }
                u64 FaultCount = ReadOSPageFaultCount() - StartPageFaultCount; 

                printf("%llu, %llu, %llu, %lld\n", PageCount, TouchCount, FaultCount, (FaultCount - TouchCount));

                VirtualFree(Data, 0, MEM_RELEASE);
            }
            else
            {
                fprintf(stderr, "ERROR: Unable to allocate memory");
            }
        }
    }
    else 
    {
        fprintf(stderr, "Usage: %s [# of 4k pages to allocated]\n", Args[0]);
    }
    
    return 0;
}
