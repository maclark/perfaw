#define _CRT_SECURE_NO_WARNINGS

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>
#include <sys/stat.h>

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

#define ArrayCount(array) (sizeof(array) / sizeof((array)[0]))

#include "profiler.cpp"
#include "haversine_formula.cpp"
#include "buffer.cpp"
//#include "casey_lookup_json_parser.cpp"
#include "lookup_json_parser.cpp"

static buffer ReadEntireFile(char *FileName)
{
    TimeFunction;
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
    TimeFunction;
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

static void ProfPrint(char const *Label, u64 TotalTSCElapsed, u64 Start, u64 End)
{
    u64 Elapsed = End - Start;
    f64 Percent = 100.0 * ((f64)Elapsed / (f64)TotalTSCElapsed);
    fprintf(stdout, "   %s: %llu (%.2f%%)\n", Label, Elapsed, Percent);
}



int main(int ArgCount, char **Args)
{
    BeginProfiler();

    int Result = 1;

    if ((ArgCount == 2) || (ArgCount == 3))
    {
        buffer InputJSON = ReadEntireFile(Args[1]);

        u32 MinimumJSONPairEncoding = 6 * 4;
        u64 MaxPairCount = InputJSON.Count / MinimumJSONPairEncoding;
        if (MaxPairCount)
        {
            buffer ParsedValues = AllocateBuffer(MaxPairCount * sizeof(haversine_pair));
            if (ParsedValues.Count)
            {
                haversine_pair *Pairs = (haversine_pair *)ParsedValues.Data;

                u64 PairCount = ParseHaversinePairs(InputJSON, MaxPairCount, Pairs);
                f64 Sum = SumHaversineDistances(PairCount, Pairs);

                Result = 0;

                fprintf(stdout, "Input size: %llu\n", InputJSON.Count);
                fprintf(stdout, "Pair count: %llu\n", PairCount);
                fprintf(stdout, "Haversine sum: %.16f\n", Sum);

                if(ArgCount == 3)
                {
                    buffer AnswersF64 = ReadEntireFile(Args[2]);
                    if (AnswersF64.Count >= sizeof(f64))
                    {
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
                    }

                    FreeBuffer(&AnswersF64);
                }
            }

            FreeBuffer(&ParsedValues);
        }
        else 
        {
            fprintf(stderr, "ERROR: Malformed input JSON\n");
        }
        
        FreeBuffer(&InputJSON);
    }
    else 
    {
        fprintf(stderr, "Usage: %s [haversine_input.json]\n", Args[0]);
        fprintf(stderr, "       %s [haversine_input.json] [answers.f64]\n", Args[0]);
    }

    if (Result == 0)
    {
        EndAndPrintProfiling();
    }

    return Result;
}

static_assert(__COUNTER__ < ArrayCount(profiler::Anchors));
