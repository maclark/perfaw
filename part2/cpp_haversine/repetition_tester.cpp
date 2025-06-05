enum test_mode : u32
{
    TestMode_Uninitialized,
    TestMode_Testing,
    TestMode_Completed,
    TestMode_Error
};

struct repetition_test_results 
{
    u64 TestCount;
    u64 TotalTime;
    u64 MinTime;
    u64 MaxTime;
};

struct repetition_tester
{
    u64 TargetProcessedByteCount;
    u64 CPUTimerFreq;
    u64 TryForTime;
    u64 TestsStartedAt;


    test_mode Mode;
    b32 PrintNewMinimums;
    u32 OpenBlockCount;
    u32 CloseBlockCount;
    u64 TimeAccumulatedOnThisTest;
    u64 BytesAccumulatedOnThisTest;

    repetition_test_results Results;
};

static f64 SecondsFromCPUTime(f64 CPUTime, u64 CPUTimerFreq)
{
    f64 Result = 0.0;
    if (CPUTimerFreq)
    {
        Result = (CPUTime / (f64)CPUTimerFreq);
    }

    return Result;
}

static void PrintTime(char const *Label, f64 CPUTime, u64 CPUTimerFreq, u64 ByteCount)
{
    printf("%s: %.0f", Label, CPUTime);
    if(CPUTimerFreq)
    {
        f64 Seconds = SecondsFromCPUTime(CPUTime, CPUTimerFreq);
        printf(" (%fms)", 1000.0f*Seconds);
        
        if(ByteCount)
        {
            f64 Gigabyte = (1024.0f * 1024.0f);
            f64 BestBandwidth = ByteCount / (Gigabyte * Seconds);
            printf(" %fgb/s", BestBandwidth);
        }
    }
}

// this is just CPUTime as u64 instead of f64
static void PrintTime(char const *Label, u64 CPUTime, u64 CPUTimerFreq, u64 ByteCount)
{
    PrintTime(Label, (f64)CPUTime, CPUTimerFreq, ByteCount);
}

static void PrintResults(repetition_test_results Results, u64 CPUTimerFreq, u64 ByteCount)
{
    PrintTime("Min", (f64)Results.MinTime, CPUTimerFreq, ByteCount);
    printf("\n");

    PrintTime("Max", (f64)Results.MaxTime, CPUTimerFreq, ByteCount);
    printf("\n");

    if (Results.TestCount)
    {
        PrintTime("Avg", (f64)Results.TotalTime / (f64)Results.TestCount, CPUTimerFreq, ByteCount);
        printf("\n");
    }
}

static void Error(repetition_tester *Tester, const char *msg)
{
    Tester->Mode = TestMode_Error;
    fprintf(stderr, "%s\n", msg);
}

static void NewTestWave(repetition_tester *Tester, u64 TargetProcessedByteCount, u64 CPUTimerFreq, u32 SecondsToTry = 10)
{
    if (Tester->Mode == TestMode_Uninitialized)
    {
        Tester->Mode = TestMode_Testing;
        Tester->TargetProcessedByteCount = TargetProcessedByteCount;
        Tester->CPUTimerFreq = CPUTimerFreq;
        Tester->PrintNewMinimums = true;
        Tester->Results.MinTime = (u64)-1; // hm isn't -1 going to be super big?
    }
    else if (Tester->Mode == TestMode_Completed)
    {
        Tester->Mode = TestMode_Testing;

        if (Tester->TargetProcessedByteCount != TargetProcessedByteCount)
        {
            Error(Tester, "TesterProcessedByteCount changed");
        }

        if (Tester->CPUTimerFreq != CPUTimerFreq)
        {
            Error(Tester, "CPUTimerFreq changed");
        }
    }

    Tester->TryForTime = SecondsToTry*CPUTimerFreq;
    Tester->TestsStartedAt = ReadCPUTimer();
}

static void BeginTime(repetition_tester *Tester)
{
    ++Tester->OpenBlockCount;
    Tester->TimeAccumulatedOnThisTest -= ReadCPUTimer();
}

static void EndTime(repetition_tester *Tester)
{
    ++Tester->CloseBlockCount;
    Tester->TimeAccumulatedOnThisTest += ReadCPUTimer();
} 

static void CountBytes(repetition_tester *Tester, size_t ByteCount)
{
    Tester->BytesAccumulatedOnThisTest += ByteCount;
}

static b32 IsTesting(repetition_tester *Tester)
{
    if (Tester->Mode == TestMode_Testing)
    {
        u64 CurrentTime = ReadCPUTimer();

        if(Tester->OpenBlockCount) // casey says we ignore timeblocks that took another path
        {
            if(Tester->OpenBlockCount != Tester->CloseBlockCount)
            {
                Error(Tester, "Unbalanced BeginTime/EndTime");
            }

            if(Tester->BytesAccumulatedOnThisTest != Tester->TargetProcessedByteCount)
            {
                Error(Tester, "Processed byte count mismatch");
            }

            if(Tester->Mode == TestMode_Testing)
            {
                repetition_test_results *Results = &Tester->Results;
                u64 ElapsedTime = Tester->TimeAccumulatedOnThisTest;
                Results->TestCount += 1;
                Results->TotalTime += ElapsedTime;
                if(Results->MaxTime < ElapsedTime)
                {
                    Results->MaxTime = ElapsedTime;
                }

                if(Results->MinTime > ElapsedTime)
                {
                    Results->MinTime = ElapsedTime;

                    Tester->TestsStartedAt = CurrentTime;

                    if(Tester->PrintNewMinimums)
                    {
                        PrintTime("Min", Results->MinTime, Tester->CPUTimerFreq, Tester->BytesAccumulatedOnThisTest);
                        printf("                                                          \r");
                    }
                }

                Tester->OpenBlockCount = 0;
                Tester->CloseBlockCount = 0;
                Tester->TimeAccumulatedOnThisTest = 0;
                Tester->BytesAccumulatedOnThisTest = 0;
            }
        }
        
        if((CurrentTime - Tester->TestsStartedAt) > Tester->TryForTime) 
        {
            Tester->Mode = TestMode_Completed;

            printf("                                                          \r");
            PrintResults(Tester->Results, Tester->CPUTimerFreq, Tester->TargetProcessedByteCount);  
        }
    }
    b32 Result = (Tester->Mode == TestMode_Testing);
    return Result;
}

