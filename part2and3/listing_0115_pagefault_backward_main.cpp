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

#include "buffer.cpp"
#include "listing_0108_platform_metrics.cpp"
#include "listing_0109_pagefault_repetition_tester.cpp"
#include "read_overhead_test.cpp"
#include "listing_0110_pagefault_overhead_test.cpp"
#include "listing_0114_pagefault_backward_test.cpp"

struct test_function
{
    const char *TestName;
    read_overhead_test_func *Func;
};
test_function TestFuncs[] = 
{
    {"WriteToAllBytes", WriteToAllBytes},
    {"WriteToAllBytesBackwards", WriteToAllBytesBackwards}
};


int main(int ArgCount, char **Args)
{

    (void)&IsInBounds;
    (void)&AreEqual;
    (void)&ReadViaFRead;
    (void)&ReadViaRead;
    (void)&ReadViaReadFile;

    InitializeOSMetrics();
    u64 CPUTimerFreq = EstimateCPUFrequency();

    if (ArgCount == 2)
    {
        char *FileName = Args[1];
#if _WIN32
        struct __stat64 Stat;
        _stat64(FileName, &Stat);
#else
        struct stat Stat;
        stat(FileName, &Stat);
#endif

        read_parameters Params = {};
        Params.Dest = AllocateBuffer(Stat.st_size); 
        Params.FileName = Args[1];

        if(Params.Dest.Count > 0)
        {
            for(;;)
            {
                repetition_tester Testers[ArrayCount(TestFuncs)][AllocType_Count] = {};

                for(u32 FuncIndex = 0; FuncIndex < ArrayCount(TestFuncs); ++FuncIndex)
                {
                    for(u32 AllocType = 0; AllocType < AllocType_Count; ++AllocType)
                    {
                        Params.AllocType = (allocation_type)AllocType;

                        repetition_tester *Tester = &Testers[FuncIndex][AllocType];
                        test_function TestFunc = TestFuncs[FuncIndex];

                        printf("\n--- %s%s%s ---\n", 
                            DescribeAllocationType(Params.AllocType),
                            Params.AllocType ? " + " : "",
                            TestFunc.TestName);
                        NewTestWave(Tester, Params.Dest.Count, CPUTimerFreq); // leaving SecondsToTry defaulting to 10s
                        TestFunc.Func(Tester, &Params);
                    }
                }
            }

            (void)&FreeBuffer; // avoiding unused function warning 
        }
        else
        {
            fprintf(stderr, "ERROR: Test data must be non zero\n");
        }
    }
    else
    {
        fprintf(stderr, "Usage: %s [existing filename]\n", Args[0]);
    }

    return 0;
}
