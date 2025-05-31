#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>
#include <sys/stat.h>

typedef uint8_t u8; 
typedef uint64_t u64;
typedef uint32_t u32;

typedef int32_t b32;

typedef float f32;
typedef double f64;

#define ArrayCount(arry) (sizeof(array) / sizeof((array)[0]))

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

    // we create parallel array of read_parameters?
    // we need to get the file from the args

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
        Params.Dest = AllocateBuffer(State.st_size); 
        Params.FileName = Args[1];

        for(;;)
        {
            repetition_tester *Testers[ArrayCount(TestFuncs)] = {}; 


            // reset the the global tester
            for(int FuncIndex = 0; FuncIndex < ArrayCount(TestFuncs); ++FuncIndex)
            {

                repetition_tester *Tester = Testers + FuncIndex;

                test_function *Func = TestFuncs[FuncIndex];

                // check if time to go has you know been too long
                TestFunction.Func(Tester, &Params);

            }
        }

        


    }
    else
    {
        fprintf(stderr, "Usage: %s [existing filename]\n", Args[0]);
    }

    return 0;
}
