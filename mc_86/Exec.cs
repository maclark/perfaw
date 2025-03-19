using System;
using M = mc_86;

public static class Exec {

    public static byte[] memory = new byte[1024 * 1024];

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
        return GetInt(reg.lo, reg.hi);
    }

    public static int GetInt(byte lo, byte hi)
    {
        return M.ToInt16(lo, hi);
    }

    public static void PrintResult(string op, string cached, string destLoc, string srcLoc, string destResult, bool printFlags=false)
    {
        string line = $"{op} {destLoc}, {srcLoc} ;";
        if (!string.IsNullOrEmpty(cached)) line += $" {destLoc}:{cached}->{destResult}";
        line += GetIp(); 
        if (printFlags) line += $" flags:->{M.GetFlags()}"; 
        Console.WriteLine(line);
    }

    public static string GetIp() 
    {
        return " ip:" + M.ToHex(M.cachedIndex) + "->" + M.ToHex(M.index);
    }

    public static void MovRmMem(bool w, bool d, M.Reg reg, int address, string addDesc)
    {
        // moving a word from register to memory
        // moving a word from memory to register
        // moving a byte from register to memory
        // moving a byte from memory to register
        string cached = GetHex(reg);
        if (w) 
        {
           if (d) 
           {
                // to a register
                reg.lo = memory[address];
                if (address == memory.Length - 1) 
                {
                    Console.WriteLine("oob address index: " + (address + 1)); 
                    address = 0;
                }
                reg.hi = memory[address + 1]; // could be oob!
                string result = GetHex(reg);
                M.debug("now at address: " + address + " " + result);
                Console.WriteLine($"mov {reg.name}, {addDesc} ; {reg.name}:{cached}->{result} {GetIp()}"); 
           }
           else 
           {
                // to memory from reg 
                memory[address] = reg.lo;
                if (address == memory.Length - 1) 
                {
                    Console.WriteLine("oob address index: " + (address + 1)); 
                    address = 0;
                }
                memory[address + 1] = reg.hi; // could be oob!
                string result = GetHex(reg);
                Console.WriteLine($"mov {addDesc}, {reg.name} ; {GetIp()}"); 
           }
        }
        else 
        {
            if (d) 
            {
                // to a register
                reg.lo = memory[address];
                string result = GetHex(reg);
                Console.WriteLine($"mov {reg.name}, {addDesc} ; {reg.name}:{cached}->{result} {GetIp()}"); 
            }
            else
            {
                // to memory from reg
                memory[address] = reg.lo;
                Console.WriteLine("we moved the lo byte to memory, do we zero out the full register?? won't result here give us a weird value, since its 16 bit?");
                string result = GetHex(reg);
                Console.WriteLine($"mov {addDesc}, {reg.name} ; {GetIp()}"); 
            }
        }
    }
    
    // we could be accessing a register
    // a register + offset
    // or direct address
    public static void MovMemImm(bool w, int address, string addDesc, byte dataLo, byte dataHi)
    {
        string desc = w ? "word" : "byte";
        int data = GetInt(dataLo, dataHi);
        string line = $"mov {desc} {addDesc}, {data} ;{GetIp()}";   
        memory[address] = dataLo;
        if (w) memory[address + 1] = dataHi;
        Console.WriteLine(line);
    }

    public static void MovImm(M.Reg dest, byte lo, byte hi)
    {
        M.debug("executing MovImm");
        string cached = GetHex(dest);
        dest.hi = hi;
        dest.lo = lo;
        string destResult = GetInt(lo, hi).ToString();
        PrintResult("mov", cached, dest.name, destResult, GetHex(dest));
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
        PrintResult("mov", cached, dest.name, src.name, GetHex(dest));
    } 

    public static void AddRmRm(M.Reg dest, M.Reg src, bool w) 
    {
        Add(dest, src.name, src.lo, src.hi, w);
    }

    public static void Add(M.Reg dest, string srcLoc, byte lo, byte hi, bool w)
    {
        if (string.IsNullOrEmpty(srcLoc)) srcLoc = GetInt(lo, hi).ToString();
        string cached = GetHex(dest); 
        int dData = GetInt(dest);
        int sData = GetInt(lo, w ? hi : (byte)0);
        int result = dData + sData;
        byte[] bytes = GetBytes(result); 
        dest.lo = bytes[0];
        dest.hi = bytes[1];
        // do we discard the destination's high bytes? no
        M.debug("lo is: " + lo);
        M.debug("dest.lo is: " + dest.lo);
        PrintResult("add", cached, dest.name, srcLoc, GetHex(dest));
    }

    public static void CmpRmRm(M.Reg dest, M.Reg src, bool w) 
    {
        Cmp(dest, src.name, src.lo, src.hi, w);
    }

    public static void Cmp(M.Reg dest, string srcLoc, byte lo, byte hi, bool w)
    {
        if (string.IsNullOrEmpty(srcLoc)) srcLoc = GetInt(lo, hi).ToString();
        if (!w)
        {
            Console.WriteLine("comparing just lo bits happens?");    
            if (lo < dest.lo) M.SetFlag(M.RegFlag.Sign);
            else M.UnsetFlag(M.RegFlag.Sign);
        }
        else 
        {
            if (GetInt(lo, hi) < GetInt(dest)) M.SetFlag(M.RegFlag.Sign);
            else M.UnsetFlag(M.RegFlag.Sign);
        }
        PrintResult("cmp", "", dest.name, srcLoc, "", true);
    }

    public static void SubRmRm(M.Reg dest, M.Reg src, bool w)
    {
        M.debug("subtraction rm rm!");
        Sub(dest, src.name, src.lo, src.hi, w);
    }

    public static void Sub(M.Reg dest, string srcLoc, byte lo, byte hi, bool w)
    {
        if (string.IsNullOrEmpty(srcLoc)) srcLoc = GetInt(lo, hi).ToString();
        string cached = GetHex(dest);
        int result = 0;
        bool signed;
        if (w) 
        {
            int dData = M.ToInt16(dest.lo, dest.hi);
            int sData = M.ToInt16(lo, hi);
            result = dData - sData;
            byte[] bytes = GetBytes(result);
            dest.lo = bytes[0];
            dest.hi = bytes[1];
            signed = (dest.hi & (1 << 7)) != 0;
        }
        else 
        {
            if (dest.hi != 0) Console.WriteLine("WARNING: non zero high bytes being dropped?");
            dest.hi = 0;
            dest.lo = (byte)(dest.lo - lo);
            result = dest.lo;
            signed = (dest.lo & (1 << 7)) != 0;
        }
        if (result == 0) 
        {
            M.SetFlag(M.RegFlag.Zero);
            M.UnsetFlag(M.RegFlag.Sign);
        }
        else 
        {
            M.UnsetFlag(M.RegFlag.Zero);
            if (signed) M.SetFlag(M.RegFlag.Sign);
            else M.UnsetFlag(M.RegFlag.Sign);
        }
        PrintResult("sub", cached, dest.name, srcLoc, GetHex(dest), true);
    }

    public static void JumpIf(M.Op op, sbyte jump)
    {
        if (jump > 125) Console.WriteLine("jump was greater than 125, so sbyte's gonna wrap");
        jump += 2; 
        switch (op) 
        {
            case  M.Op.jne:
                if (!M.CheckFlag(M.RegFlag.Zero))
                {
                    M.index += jump - 2; // jumping 2 back also to compensate for the 2 bytes read for this operation
                    Console.WriteLine($"jne ${jump} ; ip:{M.ToHex(M.cachedIndex)}->{M.ToHex(M.index)}");
                }
                else Console.WriteLine($"jne ${jump} ; ip:{M.ToHex(M.cachedIndex)}->{M.ToHex(M.index)}");
                break;
            default:
                Console.WriteLine($"{op} {jump}");
                Console.WriteLine("ERROR: unhandled jump: " + op.ToString());
                break;
        }
    }
}

