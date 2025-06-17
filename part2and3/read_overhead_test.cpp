/*
 * ok
 * we want to do fread, _read, and ReadFile, which i guess are in windows.h somewhere?
 * we need an instance of Tester struct and then we can get a pointer to it and pass that around
 *
 * we need to typedef a function that runs the program
 * i think it needs return void and accept a pointer to the Tester and then maybe data amount?
 * hm
 * we want
 * count of times run
 * average time
 * min time
 * max time
 */

#include <windows.h>
#include <fcntl.h>
#include <io.h>

enum allocation_type
{
    AllocType_none,
    AllocType_malloc,

    AllocType_Count,
};

struct read_parameters
{
    allocation_type AllocType;
    buffer Dest;
    char const *FileName;
};

typedef void read_overhead_test_func(repetition_tester *Tester, read_parameters *Params);

static const char * DescribeAllocationType(allocation_type AllocType)
{
    char const *Result;
    switch (AllocType)
    {
        case AllocType_malloc: {Result = "malloc";} break;
        case AllocType_none: {Result = "";} break;
        default: {Result = "UNKNOWN";} break; 
    }
    
    return Result;
}

static void HandleAllocation(read_parameters *Params, buffer *Buffer)
{
    switch(Params->AllocType)
    {
        case AllocType_malloc:
        {
           *Buffer = AllocateBuffer(Params->Dest.Count); 
        } break;

        case AllocType_none: 
        {

        } break;

        default: 
        {
            fprintf(stderr, "ERROR: Unrecognized allocation type");
        } break;
    }
}

static void HandleDeallocation(read_parameters *Params, buffer *Buffer)
{
    switch(Params->AllocType)
    {
        case AllocType_malloc:
        {
            FreeBuffer(Buffer);
        } break;

        case AllocType_none: 
        {

        } break;

        default: 
        {
            fprintf(stderr, "ERROR: Unrecognized allocation type");
        } break;
    }
}

static void ReadViaFRead(repetition_tester *Tester, read_parameters *Params)
{
    // do the read
    while(IsTesting(Tester))
    {
        FILE *File = fopen(Params->FileName, "rb"); 
        if(File)
        {

            buffer DestBuffer = Params->Dest;
            HandleAllocation(Params, &DestBuffer);

            BeginTime(Tester);
            size_t Result = fread(DestBuffer.Data, DestBuffer.Count, 1, File);   
            EndTime(Tester);

            if(Result == 1)
            {
                CountBytes(Tester, DestBuffer.Count);
            }
            else
            {
                Error(Tester, "fread failed");
            }

            HandleDeallocation(Params, &DestBuffer);
            fclose(File);

        }
        else 
        {
            Error(Tester, "fopen failed");    
        }
    }
}

static void ReadViaRead(repetition_tester *Tester, read_parameters *Params)
{
    while(IsTesting(Tester))
    {
        // remember, int is like a bool?
        int File = _open(Params->FileName, _O_BINARY|_O_RDONLY); 
        if(File != -1)
        {

            buffer DestBuffer = Params->Dest;
            HandleAllocation(Params, &DestBuffer);
            
            u8 *Dest = DestBuffer.Data;
            u64 SizeRemaining = DestBuffer.Count;
            while(SizeRemaining)
            {
                u32 ReadSize = INT_MAX;
                if((u64)ReadSize > SizeRemaining)
                {
                    ReadSize = (u32)SizeRemaining;
                }

                BeginTime(Tester);
                int Result = _read(File, Dest, ReadSize);   
                EndTime(Tester);

                if(Result == (int)ReadSize)
                {
                    CountBytes(Tester, ReadSize);
                }
                else
                {
                    Error(Tester, "_read failed");
                }

                SizeRemaining -= ReadSize;
                Dest += ReadSize;
            }

            HandleDeallocation(Params, &DestBuffer);
            _close(File);

        }
        else 
        {
            Error(Tester, "_open failed");    
        }
    }
}

static void ReadViaReadFile(repetition_tester *Tester, read_parameters *Params)
{
    while(IsTesting(Tester))
    {
        HANDLE File = CreateFileA(Params->FileName, GENERIC_READ, FILE_SHARE_READ|FILE_SHARE_WRITE, 0,
                                  OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, 0);
        if(File != INVALID_HANDLE_VALUE)
        {
            buffer DestBuffer = Params->Dest;
            HandleAllocation(Params, &DestBuffer);

            u8 *Dest = DestBuffer.Data;
            u64 SizeRemaining = DestBuffer.Count;
            while(SizeRemaining)
            {
                u32 ReadSize = (u32)-1;
                if((u64)ReadSize > SizeRemaining)
                {
                    ReadSize = (u32)SizeRemaining;
                }

                DWORD BytesRead = 0;
                BeginTime(Tester);
                BOOL Result = ReadFile(File, Dest, ReadSize, &BytesRead, 0);   
                EndTime(Tester);

                if(Result && (BytesRead ==  ReadSize))
                {
                    CountBytes(Tester, ReadSize);
                }
                else
                {
                    Error(Tester, "ReadFile failed");
                }

                SizeRemaining -= ReadSize;
                Dest += ReadSize;
            }
            
            HandleDeallocation(Params, &DestBuffer);
            CloseHandle(File);
        }
        else 
        {
            Error(Tester, "CreateFileA failed");    
        }
    }
}
