

#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>
#include <sys/stat.h>
//#include <iostream> // temporary for max debugging

typedef uint8_t u8;
typedef uint32_t u32;
typedef uint64_t u64;

typedef int32_t b32; // this is not a bool? b32 is signed 32-bit integer?

typedef float f32;
typedef double f64;

struct haversine_pair
{
    f64 X0, Y0;
    f64 X1, Y1;
};

#include "haversine_formula.cpp"
#include "buffer.cpp"
#include "lookup_json_parser.cpp"

static buffer ReadEntireFile(char *FileName)
{
    buffer Result = {};

    FILE *File = fopen(FileName, "rb");
    if (File)
    {
#if _WIN32
        struct __stat64 Stat;
        _stat64(FileName, &Stat);
#else
        struct stat Stat;
        stat(FileName, &Stat);
#endif



        Result = AllocateBuffer(Stat.st_size);
        if (Result.Data)
        {
            if(fread(Result.Data, Result.Count, 1, File) != 1)
            {
                fprintf(stderr, "ERROR: Unable to read \"%s\".\n", FileName);
                FreeBuffer(&Result);
            }

        }
        
        fclose(File);
    }
    else
    {
        fprintf(stderr, "ERROR: Unable to open \"%s\"", FileName);
    }


    return Result;
}

static f64 SumHaversineDistances(u64 PairCount, haversine_pair *Pairs)
{
    f64 Sum = 0;

    f64 SumCoef = 1 / (f64)PairCount;
    for(u64 PairIndex = 0; PairIndex < PairCount; ++PairIndex)
    {
        haversine_pair Pair = Pairs[PairIndex]; 
        f64 EarthRadius = 6372.8;
        f64 Dist = ReferenceHaversine(Pair.X0, Pair.Y0, Pair.X1, Pair.Y1, EarthRadius);
        Sum += SumCoef * Dist;
    }

    return Sum;
}


int main(int ArgCount, char **Args)
{

    fprintf(stdout, "did we start it?\n");
    int Result = 1;

    if ((ArgCount == 2) || (ArgCount == 3))
    {
        fprintf(stdout, "here?\n");
        buffer InputJSON = ReadEntireFile(Args[1]);

        u32 MinimumJSONPairEncoding = 6 * 4;
        u64 MaxPairCount = InputJSON.Count / MinimumJSONPairEncoding;
        if (MaxPairCount)
        {
            fprintf(stdout, "here?\n");
            buffer ParsedValues = AllocateBuffer(MaxPairCount * sizeof(haversine_pair));
            if (ParsedValues.Count)
            {
                fprintf(stdout, "parired values count > 0, here?\n");
                haversine_pair *Pairs = (haversine_pair *)ParsedValues.Data;
                u64 PairCount = ParseHaversinePairs(InputJSON, MaxPairCount, Pairs);
                fprintf(stdout, "pait count is %d?\n", PairCount);
                f64 Sum = SumHaversineDistances(PairCount, Pairs);

                fprintf(stdout, "Input size: %llu\n", InputJSON.Count);
                fprintf(stdout, "Pair count: %llu\n", PairCount);
                fprintf(stdout, "Haversine sum: %.16f\n", Sum);

                if(ArgCount == 3)
                {
                    fprintf(stdout, "arg count 3 here?\n");
                    buffer AnswersF64 = ReadEntireFile(Args[2]);
                    if (AnswersF64.Count >= sizeof(f64))
                    {
                        fprintf(stdout, "still here?\n");
                        f64 *AnswerValues = (f64 *)AnswersF64.Data;

                        fprintf(stdout, "\nValidations:\n");

                        u64 RefAnswerCount = (AnswersF64.Count - sizeof(f64)) / sizeof(f64);
                        if (PairCount != RefAnswerCount)
                        {
                            fprintf(stdout, "FAILED - pair count doesn't match %llu.\n", RefAnswerCount);
                        }


                        f64 RefSum = AnswerValues[RefAnswerCount];
                        fprintf(stdout, "Reference sum: %.16f\n", RefSum);
                        fprintf(stdout, "Difference: %.16f\n", Sum - RefSum);

                        fprintf(stdout, "\n");


                        fflush(stdout);
                    }
                    else fprintf(stdout, "what is argcount%d?\n", ArgCount);

                    FreeBuffer(&AnswersF64);
                }
                else fprintf(stdout, "still here?\n");

            }

            FreeBuffer(&ParsedValues);
        }
        else 
        {
            fprintf(stderr, "ERROR: Malformed input JSON\n");
        }
        
        FreeBuffer(&InputJSON);
        
        Result = 0; 
    }
    else 
    {
        fprintf(stderr, "Usage: %s [haversine_input.json]\n", Args[0]);
        fprintf(stderr, "       %s [haverinse_input.json] [answers.f64]\n", Args[0]);
    }

    fflush(stdout);
    fflush(stderr);

    fprintf(stdout, "did we make it?\n");
    return Result;
}


