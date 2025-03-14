using System;
using M = mc_86;

public static class Exec {

    public static byte[] GetBytes(int i)
    {
        byte[] b = BitConverter.GetBytes(i);
        if (!BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    public static string GetHex(M.Reg reg) {
        return M.ToHex(GetInt(reg));
    }

    public static int GetInt(M.Reg reg) 
    {
        return M.ToInt16(reg.lo, reg.hi);
    }

    public static void PrintResult(string op, string cached, M.Reg dest, M.Reg src)
    {
        string result = GetHex(dest);
        if (string.IsNullOrEmpty(cached)) 
        {
            Console.WriteLine($"{op} {dest.name}, {src.name} ; {M.GetFlags()}");
        }
        else Console.WriteLine($"{op} {dest.name}, {src.name} ; {dest.name}:{cached}->{result} {M.GetFlags()}");
    }

    public static void MovRmRm(M.Reg dest, M.Reg src, bool w) {
        M.debug("executing MovRmRm");
        string cached = GetHex(dest); 
        if (w) // i'm assuming this is how we know to use 1 or 2 bytes 
        {
            dest.hi = src.hi;
            dest.lo = src.lo;
        }
        else 
        {
            if (dest.hi != 0) Console.WriteLine("are we supposed to not empty out the high bits? idk");
            dest.hi = 0;
            dest.lo = src.lo;
        }
        PrintResult("mov", cached, dest, src);
    } 

    public static void AddRmRm(M.Reg dest, M.Reg src, bool w) 
    {
        string cached = GetHex(dest); 
        if (w) 
        {
           int dData = GetInt(dest);
           int sData = GetInt(src);
           int result = dData + sData;
           byte[] bytes = GetBytes(result); 
           dest.lo = bytes[0];
           dest.hi = bytes[1];
        }
        else 
        {
            // do we discard the destination's high byets? idk
            Console.WriteLine("WARNING: do we discard dest high bytes?");
            dest.lo += src.lo;
        }
        // #TODO
        //if (dest.hi == 0b0 && dest.lo == 0b0) M.SetFlag(M.flags.Z);
        PrintResult("mov", cached, dest, src);
    }

    public static void CmpRmRm(M.Reg dest, M.Reg src, bool w) 
    {
        if (!w)
        {
            Console.WriteLine("comparing just lo bits happens?");    
            if (src.lo < dest.lo) M.SetFlag(M.RegFlag.Sign);
            else M.UnsetFlag(M.RegFlag.Sign);
        }
        else 
        {
            if (GetInt(src) < GetInt(dest)) M.SetFlag(M.RegFlag.Sign);
            else M.UnsetFlag(M.RegFlag.Sign);
        }
        PrintResult("cmp", "", dest, src);
    }

    public static void SubRmRm(M.Reg dest, M.Reg src, bool w)
    {
        M.debug("subtraction!");
        string cached = GetHex(dest);
        if (w) 
        {
            int dData = M.ToInt16(dest.lo, dest.hi);
            int sData = M.ToInt16(dest.lo, dest.hi);
            int result = dData - sData;
            byte[] bytes = GetBytes(result);
            dest.hi = bytes[0];
            dest.lo = bytes[1];
        }
        else 
        {
            if (dest.hi != 0) Console.WriteLine("WARNING: non zero high bytes being dropped?");
            dest.hi = 0;
            dest.lo = (byte)(dest.lo - src.lo);
        }
        PrintResult("sub", cached, dest, src);
    }
}

