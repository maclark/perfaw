using System;
//using mc_86 = mc;

public static class Exec {

    public static byte[] GetBytes(int i)
    {
        byte[] b = BitConverter.GetBytes(i);
        if (!BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    public static string GetHex(mc_86.Reg reg) {
        return mc_86.ToHex(GetInt(reg));
    }

    public static int GetInt(mc_86.Reg reg) 
    {
        return mc_86.ToInt16(reg.lo, reg.hi);
    }

    public static void PrintResult(string op, string cached, mc_86.Reg dest, mc_86.Reg src)
    {
        string result = GetHex(dest);
        Console.WriteLine($"{op} {dest.name}, {src.name} ; {dest.name}:{cached}->{result} {mc_86.GetFlags()}");
    }

    public static void MovRmRm(mc_86.Reg dest, mc_86.Reg src, bool w) {
        mc_86.debug("executing MovRmRm");
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

    public static void AddRmRm(mc_86.Reg dest, mc_86.Reg src, bool w) 
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
        //if (dest.hi == 0b0 && dest.lo == 0b0) mc_86.SetFlag(mc_86.flags.Z);
        PrintResult("mov", cached, dest, src);
    }

    public static void CmpRmRm(mc_86.Reg dest, mc_86.Reg src, bool w) 
    {
        Console.WriteLine("unhandled cmp");
        string cached = GetHex(dest);

        PrintResult("cmp", cached, dest, src);
    }

    public static void SubRmRm(mc_86.Reg dest, mc_86.Reg src, bool w)
    {
        mc_86.debug("subtraction!");
        string cached = GetHex(dest);
        if (w) 
        {
            int dData = mc_86.ToInt16(dest.lo, dest.hi);
            int sData = mc_86.ToInt16(dest.lo, dest.hi);
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

