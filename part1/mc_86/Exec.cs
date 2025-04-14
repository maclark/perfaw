using System;
using M = mc_86;

public static class Exec {

    public static byte[] memory = new byte[1024 * 1024];
    public static int clocksTotal = 0;
    public static int opClocks = 0;
    public static int eaClocks = 0;
    public static int pClocks = 0;


    public static byte[] GetBytes(int i)
    {
        byte[] b = BitConverter.GetBytes(i);
        if (!BitConverter.IsLittleEndian) Array.Reverse(b);
        return b;
    }

    public static string GetHex(M.RegPt rp) {
        if (rp.size == M.RegSize.Full) return GetHex(rp.reg);
        else if (rp.size == M.RegSize.High) return M.ToHex(rp.reg.hi);
        else return M.ToHex(rp.reg.lo);
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

    public static int GetMem(int address, bool w)
    {
        if (address < 0) 
        {
            Console.WriteLine("ERROR: address < 0: " + address);
            address = 0;
        }
        if (w) 
        {
            if (address + 1 >= memory.Length)
            {
                Console.WriteLine("ERROR: mem out of bounds at index " + (address + 1));
                address = 0;
            }
            return GetInt(memory[address], memory[address + 1]);
        }
        else 
        {
            if (address >= memory.Length)
            {
                Console.WriteLine("ERROR: mem out of bounds at index " + address);
                address = 0;
            }
            return memory[address];
        }
    }

    public static void PrintResult(string op, string cached, string destLoc, string srcLoc, string destResult, bool printFlags=false)
    {
        string line = $"{op} {destLoc}, {srcLoc} ;{GetClocks()}";
        if (!string.IsNullOrEmpty(cached)) line += $" {destLoc}:{cached}->{destResult}";
        line += GetIp(); 
        if (printFlags) line += $" flags:->{M.GetFlags()}"; 
        Console.WriteLine(line);
    }

    public static string GetClocks()
    {
        string sumPrint = "";
        if (eaClocks > 0 || pClocks > 0) sumPrint += $" ({opClocks}";
        if (eaClocks > 0) sumPrint += $" + {eaClocks}ea";
        if (pClocks > 0) sumPrint += $" + {pClocks}p";
        if (!string.IsNullOrEmpty(sumPrint)) sumPrint += ")"; 
        int addend = opClocks + eaClocks + pClocks;
        clocksTotal += addend;
        return $" Clocks: +{addend} = {clocksTotal}{sumPrint} |";
    }

    public static string GetIp() 
    {
        return " ip:" + M.ToHex(M.cachedIndex) + "->" + M.ToHex(M.index);
    }

    public static void SafeAddress(ref int address, bool w)
    {
        if (address < 0)
        {
            Console.WriteLine("WARNING: address below zero " + address);
            address = 0;
        }
        else if (w) 
        {
            if (address + 1 >= memory.Length)
            {
                Console.WriteLine("WARNING: address oob " + address);
                address = 0;
            }
        }
        else if (address >= memory.Length)
        {
            Console.WriteLine("WARNING: address oob " + address);
            address = 0;
        }
    }

    public static void MovRegReg(bool w, M.RegPt destPt, M.RegPt srcPt)
    {
        opClocks = 2;
        MovToReg(w, destPt, srcPt.name, srcPt.reg.lo, srcPt.reg.hi);
    }

    public static void MovRegMem(bool d, bool w, M.RegPt rp, int address, string addDesc)
    {
        SafeAddress(ref address, w);
        if (d) 
        {
            opClocks = 8;
            MovToReg(w, rp, addDesc, memory[address], w ? memory[address + 1] : (byte)0);
        }
        else 
        {
            opClocks = 9;
            MovToMem(w, address, addDesc, rp.name, rp.reg.lo, rp.reg.hi);
        }
    }

    public static void MovImmMem(bool w, int addr, string addrDesc, byte lo, byte hi)
    {
        opClocks = 10;
        SafeAddress(ref addr, w);
        string srcDesc = GetInt(lo, hi).ToString();
        MovToMem(w, addr, addrDesc, srcDesc, lo, hi); 
    }

    public static void MovImmReg(bool w, M.RegPt rp, byte lo, byte hi)
    {
        opClocks = 4;
        string srcDesc = GetInt(lo, hi).ToString();
        MovToReg(w, rp, srcDesc, lo, hi);
    }

    public static void MovToReg(bool w, M.RegPt rp, string srcDesc, byte lo, byte hi)
    {
        string cached;
        string result;
        if (w) 
        {
            cached = GetHex(rp.reg);
            rp.reg.lo = lo;
            rp.reg.hi = hi;
            result = M.ToHex(GetInt(lo, hi));
        }
        else
        {
            cached = M.ToHex(rp.reg.lo); 
            if (rp.size == M.RegSize.High) rp.reg.hi = lo;
            else rp.reg.lo = lo;
            result = M.ToHex(lo);
        }
        Console.WriteLine($"mov {rp.name}, {srcDesc} ;{GetClocks()} {rp.name}:{cached}->{result}{GetIp()}"); 
    }

    private static void MovToMem(bool w, int addr, string addrDesc, string srcDesc, byte lo, byte hi)
    {
        string result;
        if (w)
        {
            memory[addr] = lo;
            memory[addr + 1] = hi;
            result = GetInt(lo, hi).ToString(); 
        }
        else
        {
            memory[addr] = lo;
            result = lo.ToString();
        }
        M.debug("data now at address: " + addr + " " + result);
        Console.WriteLine($"mov {addrDesc}, {srcDesc} ;{GetClocks()}{GetIp()}"); 
    }

    public static void AddRegReg(bool w, M.RegPt dest, M.RegPt src) 
    {
        opClocks = 3;
        AddToReg(w, dest, src.name, src.reg.lo, src.reg.hi);
    }

    public static void AddRegMem(bool d, bool w, M.RegPt rp, int address, string addDesc)
    {
        SafeAddress(ref address, w);
        if (d) 
        {
            opClocks = 9;
            AddToReg(w, rp, addDesc, memory[address], w ? memory[address + 1] : (byte)0);
        }
        else 
        {
            opClocks = 16;
            AddToMem(w, address, addDesc, rp.name, rp.reg.lo, rp.reg.hi);
        }
    }

    public static void AddImmReg(bool w, byte lo, byte hi, M.RegPt rp)
    {
        opClocks = 4;
        string srcDesc = GetInt(lo, hi).ToString();
        AddToReg(w, rp, srcDesc, lo, hi);
    }

    public static void AddImmMem(bool w, byte lo, byte hi, int address, string addDesc)
    {
        opClocks = 17;
        SafeAddress(ref address, w);
        string srcDesc = w ? GetInt(lo, hi).ToString() : lo.ToString();
        AddToMem(w, address, addDesc, srcDesc, lo, hi);
    }

    private static void AddToReg(bool w, M.RegPt rp, string srcDesc, byte lo, byte hi)
    {
        string cached;
        int sum;
        bool signed;
        if (w)
        {
            int addend = GetInt(rp.reg);
            cached = M.ToHex(addend); 
            sum = addend + GetInt(lo, hi);
            byte[] bytes = GetBytes(sum); 
            rp.reg.lo = bytes[0];
            rp.reg.hi = bytes[1];
            signed = (rp.reg.hi & (1 << 7)) != 0;
        }
        else
        {
            if (rp.size == M.RegSize.High)
            {
                cached = M.ToHex(rp.reg.hi);
                rp.reg.hi += hi;
                sum = rp.reg.hi;
                signed = (rp.reg.hi & (1 << 7)) != 0;
            }
            else 
            {
                cached = M.ToHex(rp.reg.lo);
                rp.reg.lo += lo;
                sum = rp.reg.lo;
                signed = (rp.reg.lo & (1 << 7)) != 0;
            }
        }
        
        if (signed) M.SetFlag(M.RegFlag.Sign);
        else M.UnsetFlag(M.RegFlag.Sign);
        if (sum == 0) M.SetFlag(M.RegFlag.Zero);
        else M.UnsetFlag(M.RegFlag.Zero);
        PrintResult("add", cached, rp.name, srcDesc, M.ToHex(sum));
    }

    // source could be register, memory, or immediate
    private static void AddToMem(bool w, int address, string addDesc, string srcDesc, byte lo, byte hi)
    {
        if (w)
        {
            if (address + 1 >= memory.Length) address = 0;
            int mem = GetInt(memory[address], memory[address + 1]);
            int result = mem + GetInt(lo, hi);
            byte[] bytes = GetBytes(result);
            memory[address] = bytes[0];
            memory[address + 1] = bytes[1];
            PrintResult("add", mem.ToString(), addDesc, srcDesc, M.ToHex(result));
        }
        else 
        {
            if (address >= memory.Length) address = 0;
            string cached = M.ToHex(memory[address]);
            memory[address] += lo;
            PrintResult("add", cached, addDesc, srcDesc, M.ToHex(memory[address]));
        }
    }

    public static void SubRegReg(bool w, M.RegPt dest, M.RegPt src) 
    {
        SubFromReg(w, dest, src.name, src.reg.lo, src.reg.hi);
    }

    public static void SubRegMem(bool d, bool w, M.RegPt rp, int address, string addDesc)
    {
        SafeAddress(ref address, w);
        if (d) SubFromReg(w, rp, addDesc, memory[address], w ? memory[address + 1] : (byte)0);
        else SubFromMem(w, address, addDesc, rp.name, rp.reg.lo, rp.reg.hi);
    }

    public static void SubImmReg(bool w, byte lo, byte hi, M.RegPt rp)
    {
        string srcDesc = GetInt(lo, hi).ToString();
        SubFromReg(w, rp, srcDesc, lo, hi);
    }

    public static void SubImmMem(bool w, byte lo, byte hi, int address, string addDesc)
    {
        SafeAddress(ref address, w);
        string srcDesc = w ? GetInt(lo, hi).ToString() : lo.ToString();
        SubFromMem(w, address, addDesc, srcDesc, lo, hi);
    }

    public static void SubFromReg(bool w, M.RegPt rp, string srcDesc, byte lo, byte hi)
    {
        string cached;  
        int difference;
        bool signed;
        if (w)
        {
            int minuend = GetInt(rp.reg);
            cached = M.ToHex(minuend);
            difference = minuend - GetInt(lo, hi);
            byte[] bytes = GetBytes(difference);
            rp.reg.lo = bytes[0];
            rp.reg.hi = bytes[1];
            signed = (rp.reg.hi & (1 << 7)) != 0;
        }
        else
        {
            if (rp.size == M.RegSize.High)
            {
                cached = M.ToHex(rp.reg.hi);
                rp.reg.hi -= hi;
                difference = rp.reg.hi;
                signed = (rp.reg.hi & (1 << 7)) != 0;
            }
            else 
            {
                cached = M.ToHex(rp.reg.lo);
                rp.reg.lo -= lo;
                difference = rp.reg.lo;
                signed = (rp.reg.lo & (1 << 7)) != 0;
            }
        }

        if (signed) M.SetFlag(M.RegFlag.Sign);
        else M.UnsetFlag(M.RegFlag.Sign);
        if (difference == 0) M.SetFlag(M.RegFlag.Zero);
        else M.UnsetFlag(M.RegFlag.Zero);
        PrintResult("sub", cached, rp.name, srcDesc, M.ToHex(difference));  
    }

    // source could be register, memory, or immediate
    public static void SubFromMem(bool w, int address, string addDesc, string srcDesc, byte lo, byte hi)
    {
        int cached = GetMem(address, w);
        int difference;
        bool signed;
        if (w)
        {
            difference = cached - GetInt(lo, hi);
            byte[] bytes = GetBytes(difference);
            memory[address] = bytes[0];
            memory[address + 1] = bytes[1];
            signed = (bytes[1] & (1 << 7)) != 0;
        }
        else
        {
            memory[address] -= lo;
            difference = memory[address];
            signed = (memory[address] & (1 << 7)) != 0;
        }

        if (signed) M.SetFlag(M.RegFlag.Sign);
        else M.UnsetFlag(M.RegFlag.Sign);
        if (difference == 0) M.SetFlag(M.RegFlag.Zero);
        else M.UnsetFlag(M.RegFlag.Zero);
        PrintResult("sub", M.ToHex(cached), addDesc, srcDesc, M.ToHex(difference));  
    }

    public static void CmpRegReg(bool w, M.RegPt dest, M.RegPt src) 
    {
        CmpToReg(w, dest, src.name, src.reg.lo, src.reg.hi);
    }

    public static void CmpRegMem(bool d, bool w, M.RegPt rp, int address, string addDesc)
    {
        SafeAddress(ref address, w);
        if (d) CmpToReg(w, rp, addDesc, memory[address], w ? memory[address + 1] : (byte)0);
        else CmpToMem(w, address, addDesc, rp.name, rp.reg.lo, rp.reg.hi);
    }

    public static void CmpImmReg(bool w, byte lo, byte hi, M.RegPt rp)
    {
        string srcDesc = GetInt(lo, hi).ToString();
        CmpToReg(w, rp, srcDesc, lo, hi);
    }

    public static void CmpImmMem(bool w, byte lo, byte hi, int address, string addDesc)
    {
        SafeAddress(ref address, w);
        string srcDesc = w ? GetInt(lo, hi).ToString() : lo.ToString();
        CmpToMem(w, address, addDesc, srcDesc, lo, hi);
    }

    public static void CmpToReg(bool w, M.RegPt rp, string srcDesc, byte lo, byte hi)
    {
        int difference;
        bool signed;
        if (w)
        {
            difference = GetInt(rp.reg) - GetInt(lo, hi);
            byte[] bytes = GetBytes(difference);
            signed = (bytes[1] & (1 << 7)) != 0;
        }
        else
        {
            if (rp.size == M.RegSize.High)
            {
                difference = rp.reg.hi - hi;
                signed = (rp.reg.hi & (1 << 7)) != 0;
            }
            else 
            {
                difference = rp.reg.lo - lo;
                signed = (rp.reg.lo & (1 << 7)) != 0;
            }
        }

        if (signed) M.SetFlag(M.RegFlag.Sign);
        else M.UnsetFlag(M.RegFlag.Sign);
        if (difference == 0) M.SetFlag(M.RegFlag.Zero);
        else M.UnsetFlag(M.RegFlag.Zero);
        PrintResult("cmp", "", rp.name, srcDesc, M.ToHex(difference));  
    }

    // source could be register, memory, or immediate
    public static void CmpToMem(bool w, int address, string addDesc, string srcDesc, byte lo, byte hi)
    {
        int cached = GetMem(address, w);
        int difference;
        bool signed;
        if (w)
        {
            difference = cached - GetInt(lo, hi);
            byte[] bytes = GetBytes(difference);
            signed = (bytes[1] & (1 << 7)) != 0;
        }
        else
        {
            difference = memory[address] - lo;
            signed = (lo & (1 << 7)) != 0;
        }

        if (signed) M.SetFlag(M.RegFlag.Sign);
        else M.UnsetFlag(M.RegFlag.Sign);
        if (difference == 0) M.SetFlag(M.RegFlag.Zero);
        else M.UnsetFlag(M.RegFlag.Zero);
        PrintResult("cmp", M.ToHex(cached), addDesc, srcDesc, M.ToHex(difference));  
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

            case M.Op.je:
                if (M.CheckFlag(M.RegFlag.Zero))
                {
                    M.index += jump - 2; // jumping 2 back also to compensate for the 2 bytes read for this operation
                    Console.WriteLine($"je ${jump} ; ip:{M.ToHex(M.cachedIndex)}->{M.ToHex(M.index)}");
                }
                else Console.WriteLine($"je ${jump} ; ip:{M.ToHex(M.cachedIndex)}->{M.ToHex(M.index)}");
                break;
                
            default:
                Console.WriteLine($"{op} {jump}");
                Console.WriteLine("ERROR: unhandled jump: " + op.ToString());
                break;
        }
    }
}

