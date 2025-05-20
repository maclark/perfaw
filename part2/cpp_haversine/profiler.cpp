#include "platform_metrics.cpp"

struct profile_anchor
{
    u64 TSCElapsed;
    u64 TSCElapsedChildren;
    u64 HitCount;
    const char *Label;
};


struct profiler
{
    profile_anchor Anchors[4096];

    u64 StartTSC;
    u64 EndTSC;    
};
static profiler GlobalProfiler;

u64 GlobalParentIndex;

struct profile_block
{
    profile_block(const char *Label_, u64 AnchorIndex_)
    {
        ParentIndex = GlobalParentIndex; 

        AnchorIndex = AnchorIndex_;
        Label = Label_;

        GlobalParentIndex = AnchorIndex_;
        Start = ReadCPUTimer();
    }

    ~profile_block()
    {
        u64 Elapsed = ReadCPUTimer() - Start;
        GlobalParentIndex = ParentIndex;

        profile_anchor *Parent = GlobalProfiler.Anchors + ParentIndex;
        profile_anchor *Anchor = GlobalProfiler.Anchors + AnchorIndex;

        // we need to check if the parent is the same
        if (ParentIndex != AnchorIndex) Parent->TSCElapsedChildren += Elapsed;
        Anchor->TSCElapsed += Elapsed;
        ++Anchor->HitCount;

        // casey had some weak ass excuse for why he
        // didn't take care of this at compile time :)
        Anchor->Label = Label;
    }


    const char *Label;
    u64 Start;
    u64 ParentIndex;
    u64 AnchorIndex;
};

#define NameConcat2(A, B) A##B
#define NameConcat(A, B) NameConcat2(A, B)
#define TimeBlock(Name) profile_block NameConcat(BLOCK, __LINE__)(Name, __COUNTER__ + 1) 
#define TimeFunction TimeBlock(__func__)

static void PrintTimeElapsed(u64 TotalTSCElapsed, profile_anchor *Anchor)
{
    u64 Elapsed = Anchor->TSCElapsed - Anchor->TSCElapsedChildren;
    f64 Percent = 100.0 * ((f64)Elapsed / (f64)TotalTSCElapsed);
    printf("   %s[%llu]: %llu (%.2f%%", Anchor->Label, Anchor->HitCount, Elapsed, Percent);
    if (Anchor->TSCElapsedChildren)
    {
        f64 PercentWithChildren = 100.0 * ((f64)Anchor->TSCElapsed / (f64)TotalTSCElapsed);
        printf(", %0.2f%% w/children", PercentWithChildren);
    }
    printf(")\n");
}

static void BeginProfiler(void)
{
   GlobalProfiler.StartTSC = ReadCPUTimer(); 
}

static void EndAndPrintProfiling()
{
    GlobalProfiler.EndTSC = ReadCPUTimer();
    u64 TotalElapsed = GlobalProfiler.EndTSC - GlobalProfiler.StartTSC; 
    u64 CPUFreq = EstimateCPUFrequency();
    printf("Done! %llu\n");

    if (CPUFreq)
    {
        printf("\nTotal Time: %0.4fms (CPU freq %llu)\n", 1000.0 * (f64)TotalElapsed / (f64)CPUFreq, CPUFreq);  
    }

    for(u32 ArrayIndex = 0; ArrayIndex < ArrayCount(GlobalProfiler.Anchors); ++ArrayIndex)
    {
        profile_anchor *Anchor = GlobalProfiler.Anchors + ArrayIndex; // ptr math so weird 
        if (Anchor->TSCElapsed > 0)
        {
            PrintTimeElapsed(TotalElapsed, Anchor);
        }
    }
}
