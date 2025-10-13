
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
#include "listing_0126_os_platform.cpp"
#include "listing_0109_pagefault_repetition_tester.cpp"

typedef void ASMFunction(u64 Count, u8 *Data);

extern "C" void NOP3x1AllBytes(u64 Count, u8 *Data);
extern "C" void NOP1x3AllBytes(u64 Count, u8 *Data);
extern "C" void NOP1x9AllBytes(u64 Count, u8 *Data);

// probably not needed since i'm not compiling my asm to a lib
// i'm just including the file when i g++ it:
// g++ listing_0135_multinop_loops.main.cpp listing_0134_multinop.o -o listing_0135_multinop_loops.exe
#pragma comment (lib, "listing_0134_multinop_loops")

struct test_function
{
    const char *TestName;
    ASMFunction *Func;
};

test_function TestFuncs[] = 
{
    {"NOP3x1AllBytes", NOP3x1AllBytes},
    {"NOP1x3AllBytes", NOP1x3AllBytes},
    {"NOP1x9AllBytes", NOP1x9AllBytes},
};


int main(int ArgCount, char **Args)
{
    InitializeOSPlatform();

    buffer Buffer = AllocateBuffer(1*1024*1024*1024);
    if(IsValid(Buffer))
    {
        for(;;)
        {
            repetition_tester Testers[ArrayCount(TestFuncs)] = {};

            for(u32 FuncIndex = 0; FuncIndex < ArrayCount(TestFuncs); ++FuncIndex)
            {
                repetition_tester *Tester = &Testers[FuncIndex];
                test_function TestFunc = TestFuncs[FuncIndex];

                printf("\n--- %s ---\n", TestFunc.TestName);
                NewTestWave(Tester, Buffer.Count, GetCPUTimerFreq());

                while(IsTesting(Tester)) {
                    BeginTime(Tester);
                    TestFunc.Func(Buffer.Count, Buffer.Data);
                    EndTime(Tester);
                    CountBytes(Tester, Buffer.Count);
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
