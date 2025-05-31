#include <stdint.h>
#include <stdlib.h>
#include <stdio.h>
#include <math.h>
#include <sys/stat.h>

typedef uint8_t u8; 
typedef uint64_t u64;
typedef uint32_t u32;

typedef int32_t b32;

typedef float f32;
typedef double f64;

#define ArrayCount(array) (sizeof(array) / sizeof((array)[0]))

#include "buffer.cpp"
#include "platform_metrics.cpp"
#include "repetition_tester.cpp"
#include "read_overhead_test.cpp"

struct test_function
{
    const char *TestName;
    read_overhead_test_func *Func;
};
test_function TestFuncs[] = 
{
    {"fread", ReadViaFRead},
    {"_read", ReadViaRead},
    {"ReadFile", ReadViaReadFile}
};


int main(int ArgCount, char **Args)
{

    (void)&IsInBounds;
    (void)&AreEqual;

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
                repetition_tester Testers[ArrayCount(TestFuncs)] = {};

                for(u32 FuncIndex = 0; FuncIndex < ArrayCount(TestFuncs); ++FuncIndex)
                {
                    repetition_tester *Tester = Testers + FuncIndex;
                    test_function TestFunc = TestFuncs[FuncIndex];

                    printf("\n--- %s ---\n", TestFunc.TestName);
                    NewTestWave(Tester, Params.Dest.Count, CPUTimerFreq); // leaving SecondsToTry defaulting to 10s
                    TestFunc.Func(Tester, &Params);
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
