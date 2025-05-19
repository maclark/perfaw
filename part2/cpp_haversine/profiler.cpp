#include "platform_metrics.cpp"

struct profile_anchor
{
    u64 TSCStart;
    u64 Elapsed;
    const char *Label;
};


struct profiler
{
    profile_anchor Anchors[4096];
};

profiler GlobalProfiler;

struct profile_block
{
    profile_block(const char *Label_, u64 AnchorIndex_)
    {
        Anchor->Start = ReadCPUTimer();
        AnchorIndex = AnchorIndex_;
        Label = Label_;
    }


    ~profile_block()
    {
        
        Anchor->Elapsed = Anchor->Start - ReadCPUTimer();
        Anchor = GlobalProfilers.Anchors + AnchorIndex;
        Anchor->HitCount++;

        // casey had some waek ass excuse for why he didn't this at compile time :)
        Anchor->Label = Label;
    }


    u64 AnchorIndex;
    u64 HitCount;
    profile_anchor *Anchor;  
    const char *Label;

};

#define Concat2(A, B) A##B
#define Concat(A, B) Concat2(A, B)
#define TimeBlock profile_block(__func__, __COUNTER__ + 1) 
#define TimeFunction TimeBlock(__func__)
