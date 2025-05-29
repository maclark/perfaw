#include "platform_metrics.cpp"

#ifndef PROFILER
#define PROFILER 0
#endif

#ifndef READ_BLOCK_TIMER
#define READ_BLOCK_TIMER ReadCPUTimer 
#endif

#if PROFILER

struct profile_anchor
{
    u64 TSCElapsedInclusive;
    u64 TSCElapsedExclusive;
    u64 HitCount;
    u64 ProcessedByteCount;
    const char *Label;
};
static profile_anchor GlobalProfilerAnchors[4096];
u64 GlobalParentIndex;

struct profile_block
{
    profile_block(const char *Label_, u64 AnchorIndex_, u64 ByteCount)
    {
        if (ByteCount) printf("profiling %s with ByteCount: %llu\n", Label_, ByteCount);
        else printf("profiling %s\n", Label_);
        ParentIndex = GlobalParentIndex; 

        AnchorIndex = AnchorIndex_;
        Label = Label_;

        profile_anchor *Anchor = GlobalProfilerAnchors + AnchorIndex;
        OldTSCElapsedInclusive = Anchor->TSCElapsedInclusive;
        Anchor->ProcessedByteCount += ByteCount;

        GlobalParentIndex = AnchorIndex_;
        Start = ReadCPUTimer();
    }

    ~profile_block()
    {
        u64 Elapsed = ReadCPUTimer() - Start;
        GlobalParentIndex = ParentIndex;

        profile_anchor *Parent = GlobalProfilerAnchors + ParentIndex;
        profile_anchor *Anchor = GlobalProfilerAnchors + AnchorIndex;

        Parent->TSCElapsedExclusive -= Elapsed;
        Anchor->TSCElapsedExclusive += Elapsed;
        Anchor->TSCElapsedInclusive = OldTSCElapsedInclusive + Elapsed; 
        ++Anchor->HitCount;

        // casey had some weak ass excuse for why he
        // didn't take care of this at compile time :)
        Anchor->Label = Label;
    }


    const char *Label;
    u64 Start;
    u64 ParentIndex;
    u64 AnchorIndex;
    u64 OldTSCElapsedInclusive;
};

#define NameConcat2(A, B) A##B
#define NameConcat(A, B) NameConcat2(A, B)
#define TimeBandwidth(Name, ByteCount) profile_block NameConcat(BLOCK, __LINE__)(Name, __COUNTER__ + 1, ByteCount) 
#define FinalAssert static_assert(__COUNTER__ < ArrayCount(GlobalProfilerAnchors));

static void PrintTimeElapsed(u64 TotalTSCElapsed, profile_anchor *Anchor, u64 TimerFreq)
{
    f64 Percent = 100.0 * ((f64)Anchor->TSCElapsedExclusive / (f64)TotalTSCElapsed);
    printf("   %s[%llu]: %llu (%.2f%%", Anchor->Label, Anchor->HitCount, Anchor->TSCElapsedExclusive, Percent);
    if (Anchor->TSCElapsedInclusive != Anchor->TSCElapsedExclusive)
    {
        f64 PercentWithChildren = 100.0 * ((f64)Anchor->TSCElapsedInclusive / (f64)TotalTSCElapsed);
        printf(", %0.2f%% w/children", PercentWithChildren);
    }
    if (Anchor->ProcessedByteCount)
    {
        f64 Megabyte = 1024.0f * 1024.0f;
        f64 Gigabyte = 1024.0f * Megabyte;

        f64 Seconds = (f64)Anchor->TSCElapsedInclusive / (f64)TimerFreq;
        f64 BytesPerSecond = (f64)Anchor->ProcessedByteCount / Seconds;
        f64 Megabytes = (f64)Anchor->ProcessedByteCount / (f64)Megabyte;
        f64 GigabytesPerSecond = BytesPerSecond / Gigabyte;
        
        printf("  %0.3f mb at %0.2f gb/s", Megabytes, GigabytesPerSecond);
    }
    printf(")\n");
}

static void PrintAnchorData(u64 TotalTSCElapsed, u64 TimerFreq)
{
    for(u32 AnchorIndex = 0; AnchorIndex < ArrayCount(GlobalProfilerAnchors); ++AnchorIndex)
    {
        profile_anchor *Anchor = GlobalProfilerAnchors + AnchorIndex; // ptr math so weird 
        if (Anchor->TSCElapsedInclusive > 0)
        {
            PrintTimeElapsed(TotalTSCElapsed, Anchor, TimerFreq);
        }
    }
}

#else

#define TimeBandwidth(...)
#define PrintAnchorData(...)
#define FinalAssert

#endif

struct profiler
{
    u64 StartTSC;
    u64 EndTSC;    
};
static profiler GlobalProfiler;


#define TimeBlock(Name) TimeBandwidth(Name, 0) 
#define TimeFunction TimeBlock(__func__)

static u64 EstimateBlockTimerFrequency(void)
{
    (void)&EstimateCPUFrequency; // to squelch compiler warning according to casey!

    u64 HowLongToWait = 100; 
    u64 OSFreq = GetOSTimerFreq();

    u64 BlockStart = READ_BLOCK_TIMER();
    u64 OSStart = ReadOSTimer();
    u64 OSElapsed = 0;
    u64 OSEnd = 0;
    u64 OSWaitTime = HowLongToWait * OSFreq / 1000;

    while(OSElapsed < OSWaitTime)
    {
        OSEnd = ReadOSTimer();
        OSElapsed = OSEnd - OSStart;
    }   

    u64 BlockEnd = READ_BLOCK_TIMER();
    u64 BlockElapsed = BlockEnd - BlockStart;  

    u64 BlockFreq = 0;
    if (OSElapsed)
    {
        BlockFreq = BlockElapsed * OSFreq / OSElapsed;
    }    

    return BlockFreq;
}

static void BeginProfiler(void)
{
    printf("BeginProfiler...\n");
    GlobalProfiler.StartTSC = READ_BLOCK_TIMER(); 
}

static void EndAndPrintProfiling()
{
    GlobalProfiler.EndTSC = READ_BLOCK_TIMER(); 
    u64 TimerFreq = EstimateBlockTimerFrequency();

    u64 TotalTSCElapsed = GlobalProfiler.EndTSC - GlobalProfiler.StartTSC; 

    if (TimerFreq)
    {
        printf("\nTotal Time: %0.4fms (CPU freq %llu)\n", 1000.0 * (f64)TotalTSCElapsed / (f64)TimerFreq, TimerFreq);  
    }

    PrintAnchorData(TotalTSCElapsed, TimerFreq);
}
