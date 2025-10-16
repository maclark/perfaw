
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

#include "listing_0125_buffer.cpp"
#include "listing_0137_os_platform.cpp"
#include "listing_0109_pagefault_repetition_tester.cpp"

typedef void ASMFunction(u64 Count, u8 *Data);

extern "C" void ConditionalNOP(u64 Count, u8 *Data);

// probably not needed since i'm not compiling my asm to a lib
// i'm just including the file when i g++ it:
// g++ listing_0138_condtional_nop_loops_main.cpp listing_0136_conditional_nop_loops.o -o listing_0138_condtional_nop_loops_main.exe
#pragma comment (lib, "listing_0136_conditional_nop_loops")

struct test_function
{
    const char *TestName;
    ASMFunction *Func;
};

test_function TestFuncs[] = 
{
    {"ConditionalNOP", ConditionalNOP},
};

enum branch_pattern 
{
    BranchPattern_NeverTaken,
    BranchPattern_AlwaysTaken,
    BranchPattern_Every2,
    BranchPattern_Every3,
    BranchPattern_Every4,
    BranchPattern_CRTRandom,
    BranchPattern_OSRandom,

    BranchPattern_Count,
};

static char const *FillWithBranchPattern(branch_pattern Pattern, buffer Buffer) 
{
    char const *PatternName = "UNKNOWN";

    if (Pattern == BranchPattern_OSRandom) 
    {
        PatternName = "OSRandom";
        // do something else
        FillWithRandomBytes(Buffer);
    }
    else
    {

        for (u64 Index = 0; Index < Buffer.Count; ++Index)
        {

            u8 Value = 0;

            switch (Pattern)
            {
                case BranchPattern_NeverTaken:
                {
                    PatternName = "Never Taken";
                    Value = 0;
                } break;

                case BranchPattern_AlwaysTaken:
                {
                    PatternName = "Always Taken";
                    Value = 1;
                } break;

                case BranchPattern_Every2:
                {
                    PatternName = "Every 2";
                    Value = ((Index % 2) == 0);
                } break;

                case BranchPattern_Every3:
                {
                    PatternName = "Every 3";
                    Value = ((Index % 3) == 0);
                } break;

                case BranchPattern_Every4:
                {
                    PatternName = "Every 4";
                    Value = ((Index % 4) == 0);
                } break;

                case BranchPattern_CRTRandom:
                {
                    PatternName = "CRTRandom";
                    // casey says this isnt' a very good rng
                    Value = (u8)rand();
                } break;


                default:
                {
                    fprintf(stderr, "unrecognized branch pattern.\n");
                } break;
            }

            Buffer.Data[Index] = Value;

        }
    }

    return PatternName;
}


int main(int ArgCount, char **Args)
{
    InitializeOSPlatform();

    u64 Count = 1*1024*1024*1024;
    buffer Buffer = AllocateBuffer(Count + 8);
    if(IsValid(Buffer))
    {
        repetition_tester Testers[BranchPattern_Count][ArrayCount(TestFuncs)] = {};

        for(;;)
        {
            for(u32 Pattern = 0; Pattern < BranchPattern_Count; ++Pattern)
            {

                char const *PatternName = FillWithBranchPattern((branch_pattern)Pattern, Buffer);

                for(u32 FuncIndex = 0; FuncIndex < ArrayCount(TestFuncs); ++FuncIndex)
                {
                    repetition_tester *Tester = &Testers[Pattern][FuncIndex];
                    test_function TestFunc = TestFuncs[FuncIndex];

                    printf("\n--- %s, %s ---\n", TestFunc.TestName, PatternName);
                    NewTestWave(Tester, Buffer.Count, GetCPUTimerFreq());

                    while(IsTesting(Tester)) {
                        BeginTime(Tester);
                        TestFunc.Func(Count, Buffer.Data);
                        EndTime(Tester);
                        CountBytes(Tester, Count);
                    }
                }

            }
        }
    }
    else 
    {
        fprintf(stderr, "unable to allocate memory buffer for testing");
    }

    FreeBuffer(&Buffer);

    return 0;
}
