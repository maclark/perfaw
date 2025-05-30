/*
 * ok
 * we want to do fread, _read, and ReadFile, which i guess are in windows.h somewhere?
 * we need an instance of Timer struct and then we can get a pointer to it and pass that around
 *
 * we need to typedef a function that runs the program
 * i think it needs return void and accept a pointer to the Timer and then maybe data amount?
 * hm
 * we want
 * count of times run
 * average time
 * min time
 * max time
 */

struct timer_data 
{
    u64 RunCount;
    f64 AverageTime;
    f64 MinTime;
    f64 MaxTime;
};

enum time_mode 
{
    TimeMode_Uninitialized,
    TimeMode_Timing,
    TimeMode_Complete,
    TimeMode_Error
}

typedef void read_timer(timer_data *Timer, u64 Data);

struct timer
{
    time_mode Mode;
};



