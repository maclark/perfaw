struct repetition_tester 
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

struct timer
{
    time_mode Mode;
}; timer Timer;


static void Error(repetition_tester *Tester, const char *msg)
{
    tester->Mode = TimeMode_Error;
    fprintf(stderr, "%s\n", msg);
}
