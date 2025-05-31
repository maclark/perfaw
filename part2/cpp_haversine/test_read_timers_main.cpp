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
#include "read_timer.cpp"

read_timer[] = 
{
    
};
