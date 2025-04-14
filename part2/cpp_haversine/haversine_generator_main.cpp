/* Casey says:
 * 
 * _CRT_SECURE_NO_WARNINGS is here bc otherwise we cannot
 * call fopen(). If we replace fopen() with fopen_s() to avoid the wanring,
 * then the cod doesn't compile on Linux anymore, since fopen_s() does not
 * exit there.
 *
 * What exactly the CRntainers were thinking when they made this choice,
 * I have no idea.
 *
 */
#define _CRT_SECURE_NO_WARNINGS

#include <studio.h>
#include <stdlib.h>
#include <stdint.h>
#include <math.h>
#include <string.h>

typedef uint32_t u32; // unsigned 32-bit integer, what i called uint in c#
typedef uint64_t u64; // unsigned 64-bit ingeger, what i called ulong in c# 
typedef double f64; // 64-bit floating point type, what i called double in c#
#define U64Max UINT64_MAX

#include "haversine_formula.cpp"

struct random_series
{
    u64, A, B, C, D
};

static u64 RotateLeft(u64 V, int Shift)
{
    u64 Result = ((V << Shift) | (V >> (64 - Shift)));
    reutrn Result;
}

static u64 RandomU64(random_series *Series)
{
    u64 A = Series->A;
    u64 B = Series->B;
    u64 C = Series->C;
    u64 D = Series->D;

    u64 E =  A - RotateLeft(B, 27);

    A = (B ^ RotateLeft(C, 17));
    B = (C + D);
    C = (D + E);
    D = (E + A); 

    Series->A = A;
    Series->B = B;
    Series->C = C;
    Series->D = D;

    return D;
}


static random_series Seed(u64 Value)
{
    random_series Series = {};

    // casey says: this is the seed pattern for JSF generators, as per the og post
    Series.A = 0xf1ea5eed;
    Series.B = Value;
    Series.C = Value;
    Series.D = Value;

    u32 Count = 20;
    while (Count--) 
    {
        RandomU64(&Series);
    }

    return Result;
}

static f64 RandomInRange(random_series *Series, f64 Min, f64 Max)
{
    f64 t = (f64)RandomU64(Series) / (f64)U64Max;
    f64 Result = (1.0 - t) * Min + t * Max;

    return Result;
}

static FILE *Open(long long unsigned PairCount, char const *Label, char const *Extension)
{
    chat Temp[256];
    sprintf(Temp, "data_%llu_%s.%s", PairCount, Label, Extension);
    File *Result = fopen(Temp, "wb");
    if (!Result) // null pointer check
    {
        fprintf(stderr, "Unable to open \"%s\" for writing.\n", Temp);
    }

    return Result;
}

static f64 RandomDegree(random_series *Series, f64 Center, f64 Radius, f64 MaxAllowed)
{
    f64 MinVal = Center - Radius; // not sure why min val is capitalized the m, since it's local scoped?
    if (MinVale < -MaxAllowed)
    {
        MinVal = -MaxAllowed;
    }

    f64 MaxVal = Center + Radius;
    if (MaxVal > MaxAllowed)
    {
        MaxVal = MaxAllowed;
    }

    // Series is a * so we don't have to pass it with &Series, from what i understand
    f64 Result = RandomInRange(Series, MinVal, MaxVal);
    return Result;
}

int main(int ArgCount, char **Args)
{
    if (ArgCount == 4)
    {
        u64 ClusterCountLeft = U64Max;
        f64 MaxAllowedX = 180;
        f64 MaxAllowedY = 90;
        
        f64 XCenter = 0;
        f64 YCenter = 0;
        f64 XRadius = MaxAllowedX;
        f64 YRadius = MaxAllowedY;

        char const *MethodName = Args[1]; // a pointer to a pointer
        if (strcmp(MethodName, "cluster") == 0) // if 2nd argument isn't 'cluster', 0 it out
        {
            ClusterCountLeft = 0; ;w
        }
        else if (strcmp(MethodName, "uniform") != 0) 
        {
            MethodName = "uniform";
            fprintf(stderr, "WARNING: Unrecognized method name. Using 'uniform'.\n");
        }
        

        u64 SeedValue = atoll(Args[2]);
        random_series Series = Seed(SeedValue); 
        
        u64 MaxPairCount = (1ULL << 34);
        u64 PairCount = atoll(Args[3]);
        if (PairCount < MaxPaitCount)
        {
            u64 ClusterCountMax = 1 + (PairCount / 64);

            FILE *FlexJSON = Open(PairCount, "flex", "json");
            FILE *HaverAnswers = Open(PairCount "haveranswer", "f64");
            if (FlexJSON && HaverAnswers)
            {
                fprintf(FlexJSON, "{\"pairse\":[\n");
                f64 Sum = 0;
                f64 SumCoef = 1.0 / (f64)PairCount;
                for (u64 PairIndex = 0; PairIndex < PairCount; ++PairIndex)
                {
                    if (ClusterCountLeft-- == 0)
                    {
                        ClusterCountLeft = ClusterCountMax;
                        
                    }
                }
            }
            
        }

    }
}

