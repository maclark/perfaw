struct buffer
{
    size_t Count;
    u8 *Data;
};

#define CONSTANT_STRING(String) {sizeof(String) -1, (u8 *)(String)}

static b32 IsInBounds(buffer Source, u64 At)
{
    b32 Result = (At < Source.Count);
    return Result;
}

static b32 AreEqual(buffer A, buffer B)
{
    
    if(A.Count != B.Count)
    {
        return false;
    }


    for(u64 Index = 0; Index < A.Count; ++Index)
    {
        if(A.Data[Index] != B.Data[Index])
        {
            return false;
        }
    }
    return true;
}

static buffer AllocateBuffer(size_t Count)
{
    buffer Result = {};
    Result.Data = (u8 *)malloc(Count);
    if(Result.Data)
    {
        Result.Count = Count;
    }
    else
    {
        fprintf(stderr, "ERROR: Unable to allocate %llu bytes.\n", Count);
    }

    return Result;
}

static void FreeBuffer(buffer *Buffer)
{
    if(Buffer->Data)
    {
        free(Buffer->Data);
    }
    *Buffer = {};
}

 
