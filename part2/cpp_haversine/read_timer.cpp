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

#include <windows.h>

struct read_parameters
{
    buffer Dest;
    char const *FileName;
};

typedef void read_overhead_test_func(repetition_tester *Tester, read_parameters *Params);

static void ReadiViaFRead(timer_data *Timer, read_parameters *Params)
{
    // do the read
    while(IsTesting(Tester))
    {
        FILE *File = fopen(Params->FileName, "rb"); 
        if(File)
        {

            buffer DestBuffer = Params->Dest;

            BeginTimer(Tester);
            size_t Result = fread(DestBuffer.Data, DestBuffer.Count, 1, File);   
            EndTimer(Tester);

            if(Result == 1)
            {
                CountBytes(Tester, DestBuffer.Count);
            }
            else
            {
                Error(Tester, "fread failed");
            }


            fclose(File);

        }
        else 
        {
            Error(Tester, "fopen failed");    
        }
    }
}

static void ReadViaRead(timer_data *Timer, read_parameters *Params)
{
    while(IsTesting(Tester))
    {
        // remember, int is like a bool?
        int File = _open(Params->FileName, _0_BINARY|_0_RDONLY); 
        if(File != -1)
        {

            buffer DestBuffer = Params->Dest;

            u8 *Dest = DestBuffer.Data;
            u64 SizeRemaining = DestBuffer.Count;
            while(SizeRemaining)
            {
                if((u64)ReadSize > SizeRemaining)
                {
                    ReadSize = (u32)SizeRemaining;
                }

                BeginTimer(Tester);
                int Result = _read(File, Dest, ReadSize);   
                EndTimer(Tester);

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

            _close(File);

        }
        else 
        {
            Error(Tester, "_open failed");    
        }
    }
}

static void ReadViaReadFile(timer_data *Timer, read_parameters *Params)
{
    while(IsTesting(Tester))
    {
        HANDLE File = CreateFileA(Params->FIleName, GENERIC_READ, FILE_SHARE_READ|FILE_SHARE_WRITE, 0,
                                  OPEN_EXISTING, FILE_ATTIRBUTE_NORMAL, 0);
        if(File != INVALID_HANDLE_VALUE)
        {
            buffer DestBuffer = Params->Dest;

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
                BeginTimer(Tester);
                BOOL Result = ReadFile(File, Dest, ReadSize, &BytesRead, 0);   
                EndTimer(Tester);

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

            CloseHandle(File);
        }
        else 
        {
            Error(Tester, "CreateFileA failed");    
        }
    }
}
