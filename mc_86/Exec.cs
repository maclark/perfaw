using System;
using M = mc_86;

public static class Exec {

    public static byte[] memory = new byte[1024 * 1024];
    public static Opera operation;
    
    public enum Opera
    {
        NONE,
        MOV,
        ADD,
        SUB,
        CMP,
    }

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

    public static void MovRegMem(bool d, bool w, M.Reg reg, int address, string addDesc)
    {
        // moving a word from register to memory
        // moving a word from memory to register
        // moving a byte from register to memory
        // moving a byte from memory to register
        if (address < 0)
        {
            Console.WriteLine("ERROR: address < 0, " + address);
            address = 0;
        }
        else if (address >= memory.Length) 
        {
            Console.WriteLine("ERROR: address oob " + address);
            address = 0;
        }
        string o = "mov "; //w ? "mov word " : "mov byte ";
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
                Console.WriteLine($"{o}{reg.name}, {addDesc} ; {reg.name}:{cached}->{result} {GetIp()}"); 
           }
           else 
           {
                // to memory from reg 
                memory[address] = reg.lo;
                if (address + 1 >= memory.Length) 
                {
                    Console.WriteLine("oob address index: " + (address + 1)); 
                    address = 0;
                }
                memory[address + 1] = reg.hi; // could be oob!
                string result = GetHex(reg);
                M.debug("data now at address: " + address + " " + result);
                Console.WriteLine($"{o}{addDesc}, {reg.name} ; {GetIp()}"); 
           }
        }
        else 
        {
            if (d) 
            {
                // to a register
                // note: hi byte unchanged
                reg.lo = memory[address];
                string result = GetHex(reg);
                Console.WriteLine($"{o}{reg.name}, {addDesc} ; {reg.name}:{cached}->{result} {GetIp()}"); 
            }
            else
            {
                // to memory from reg
                // note: hi byte unchanged
                memory[address] = reg.lo;
                string result = GetHex(reg);
                M.debug("data now at address: " + address + " " + result);
                Console.WriteLine($"{o}{addDesc}, {reg.name} ; {GetIp()}"); 
            }
        }
    }
    
    // we could be accessing a register
    // a register + offset
    // or direct address
    public static void MovImmMem(bool w, int address, string addDesc, byte dataLo, byte dataHi)
    {
        string size = w ? "word" : "byte";
        int data = GetInt(dataLo, dataHi);
        string line = $"mov {addDesc}, {size} {data} ;{GetIp()}";   
        memory[address] = dataLo;
        if (w) memory[address + 1] = dataHi;
        Console.WriteLine(line);
    }

    public static void MovImmReg(boool w, M.RegPt rp, byte lo, byte hi)
    {
        debug("MovImmReg");
        string cached = GetHex(rp);
        int result = lo;
        if (rp.size == M.RegSize.Full)
        {
            rp.reg.lo = lo;
            if (w) 
            {
                rp.reg.hi = hi;
                result = GetInt(lo, hi);
            }
        }
        else 
        {
            System.Diagnostics.Assert(!w);
            if (rp.size == M.Reg.Size.High) rp.reg.hi = lo;
            else rp.reg.lo = lo;
        }
        string printableResult = GetInt(lo, hi).ToString();
        if (w) printableResult = "word " + result.ToString();
        else printableResult = "byte " + result.ToString();
        PrintResult("mov", cached, rp.name, printableResult, GetHex(result));
    }

    public static void MovImmReg(bool w, M.Reg dest, byte lo, byte hi)
    {
        M.debug("executing MovImmReg");
        string cached = GetHex(dest);
        dest.hi = hi;
        dest.lo = lo;
        string destResult = GetInt(lo, hi).ToString();
        if (w) destResult = "word " + destResult;
        else destResult = "byte " + destResult;
        PrintResult("mov", cached, dest.name, destResult, GetHex(dest));
    }

    public static void MovRegReg(bool w, M.Reg dest, M.Reg src) {
        M.debug("executing MovRmRm");
        string cached = GetHex(dest); 
        dest.lo = src.lo;
        if (w) dest.hi = src.hi;
        PrintResult("mov", cached, dest.name, src.name, GetHex(dest));
    } 

    public static void AddRegReg(bool w, M.Reg dest, M.Reg src) 
    {
        AddToReg(w, dest, src.name, src.lo, src.hi);
    }

    public static void AddRegMem(bool d, bool w, M.Reg reg, int address, string addDesc)
    {
        if (d) AddToReg(w, reg, addDesc, memory[address], w ? memory[address + 1] : (byte)0);
        else AddToMem(w, address, addDesc, reg.name, reg.lo, reg.hi);
    }

    public static void AddImmReg(bool w, byte lo, byte hi, M.Reg dest)
    {
        // may need to format string better
        AddToReg(w, dest, GetInt(lo, hi).ToString(), lo, hi);
    }

    public static void AddImmMem(bool w, byte lo, byte hi, int address, string addDesc)
    {
        AddToMem(w, address, addDesc, GetInt(lo, hi).ToString(), lo, hi);
    }

    // source could be register, memory, or immediate
    public static void AddToReg(bool w, M.Reg dest, string srcDesc, byte lo, byte hi)
    {
        if (string.IsNullOrEmpty(srcDesc)) srcDesc = GetInt(lo, hi).ToString();
        string cached = GetHex(dest); 
        int dData = GetInt(dest);
        int sData = GetInt(lo, w ? hi : (byte)0);
        int result = dData + sData;
        byte[] bytes = GetBytes(result); 
        dest.lo = bytes[0];
        dest.hi = bytes[1];

        bool signed;
        if (w) signed = (dest.hi & (1 << 7)) != 0;
        else signed = (dest.lo & (1 << 7)) != 0;
        if (signed) M.SetFlag(M.RegFlag.Sign);
        else M.UnsetFlag(M.RegFlag.Sign);
        if (result == 0) M.SetFlag(M.RegFlag.Zero);
        else M.UnsetFlag(M.RegFlag.Zero);

        // do we discard the destination's high bytes? no
        M.debug("lo is: " + lo);
        M.debug("dest.lo is: " + dest.lo);
        PrintResult("add", cached, dest.name, srcDesc, GetHex(dest));
    }

    // source could be register, memory, or immediate
    public static void AddToMem(bool w, int address, string addDesc, string srcDesc, byte lo, byte hi)
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

    public static void SubRegReg(bool w, M.Reg dest, M.Reg src) 
    {
        SubFromReg(w, dest, src.name, src.lo, src.hi);
    }

    public static void SubRegMem(bool d, bool w, M.Reg reg, int address, string addDesc)
    {
        if (d) SubFromReg(w, reg, addDesc, memory[address], w ? memory[address + 1] : (byte)0);
        else SubFromMem(w, address, addDesc, reg.name, reg.lo, reg.hi);
    }

    public static void SubImmReg(bool w, byte lo, byte hi, M.Reg dest)
    {
        // may need to format string better
        SubFromReg(w, dest, GetInt(lo, hi).ToString(), lo, hi);
    }

    public static void SubImmMem(bool w, byte lo, byte hi, int address, string addDesc)
    {
        SubFromMem(w, address, addDesc, GetInt(lo, hi).ToString(), lo, hi);
    }

    // source could be register, memory, or immediate
    public static void SubFromReg(bool w, M.Reg dest, string srcDesc, byte lo, byte hi)
    {
        int cached = GetInt(dest);
        int result;
        bool signed;
        if (w)
        {
            result = cached - GetInt(lo, hi);
            byte[] bytes = GetBytes(result);
            dest.lo = bytes[0];
            dest.hi = bytes[1];
            signed = (dest.hi & (1 << 7)) != 0;
        }
        else
        {
            dest.lo = (byte)(dest.lo - lo);
            dest.hi = 0; // dropping hi bytes?
            result = dest.lo;
            signed = (lo & (1 << 7)) != 0;
        }

        if (signed) M.SetFlag(M.RegFlag.Sign);
        else M.UnsetFlag(M.RegFlag.Sign);
        if (result == 0) M.SetFlag(M.RegFlag.Zero);
        else M.UnsetFlag(M.RegFlag.Zero);
        PrintResult("sub", M.ToHex(cached), dest.name, srcDesc, GetHex(dest));  
    }

    // source could be register, memory, or immediate
    public static void SubFromMem(bool w, int address, string addDesc, string srcDesc, byte lo, byte hi)
    {
        int cached = GetMem(address, w);
        int result;
        bool signed;
        if (w)
        {
            result = cached - GetInt(lo, hi);
            byte[] bytes = GetBytes(result);
            memory[address] = bytes[0];
            memory[address + 1] = bytes[1];
            signed = (bytes[1] & (1 << 7)) != 0;
        }
        else
        {
            memory[address] -= lo;
            result = memory[address];
            signed = (lo & (1 << 7)) != 0;
        }

        if (signed) M.SetFlag(M.RegFlag.Sign);
        else M.UnsetFlag(M.RegFlag.Sign);
        if (result == 0) M.SetFlag(M.RegFlag.Zero);
        else M.UnsetFlag(M.RegFlag.Zero);
        PrintResult("sub", M.ToHex(cached), addDesc, srcDesc, M.ToHex(result));  
    }

    // ofc, WORD or BYTE
    // and d sets 
    // cmp reg reg x
    // cmp reg imm x
    // cmp reg mem
    // cmp mem imm
    public static void OperateRegReg(bool w, M.Reg dest, M.Reg src)
    {
        if (w) Operate(GetInt(dest), GetInt(src), dest.name, src.name);
        else Operate(dest.lo, src.lo, dest.name, src.name);
    }

    public static void OperateRegMem(bool d, bool w, M.Reg reg, int address, string addDesc)
    {
        if (d)
        {
            if (w) Operate(GetInt(reg), GetMem(address, w), reg.name, addDesc);
            else Operate(reg.lo, GetMem(address, w), reg.name, addDesc);
        }
        else
        {
            if (w) Operate(GetMem(address, w), GetInt(reg), addDesc, reg.name);
            else Operate(GetMem(address, w), reg.lo, addDesc, reg.name);
        }
    }

    public static void OperateImmReg(bool w, byte lo, byte hi, M.Reg reg)
    {
        if (w) Operate(GetInt(reg), GetInt(lo, hi), reg.name, GetInt(lo, hi).ToString());
        else Operate(reg.lo, lo, reg.name, lo.ToString());
    }

    public static void OperateImmMem(bool w, byte lo, byte hi, int address, string addDesc)
    {
        if (w) Operate(GetInt(lo, hi), GetMem(address, w), GetInt(lo, hi).ToString(), addDesc);  
        else Operate(lo, GetMem(address, w), lo.ToString().ToString(), addDesc);
    }

    public static void Operate(int l, int r, string lDesc, string rDesc)
    {
        // mov, add, sub, cmp
        switch (operation)
        {
            case Opera.MOV:
                //Mov(l, r, lDesc, rDesc);
                break;
            case Opera.ADD:
                //Add(l, r, lDesc, rDesc);
                break;
            case Opera.SUB:
                //Sub(l, r, lDesc, rDesc);
                break;
            case Opera.CMP:
                Cmp(l, r, lDesc, rDesc);
                break;
            default:
                Console.WriteLine("ERROR: unhandled operation " + operation);
                break;
        }
        operation = Opera.NONE;
    }

    public static void Cmp(int left, int right, string lDesc, string rDesc)
    {
        int diff = left - right;
        if (diff < 0) 
        {
            M.SetFlag(M.RegFlag.Sign);
            M.UnsetFlag(M.RegFlag.Zero);
        }
        else if (diff > 0) 
        {
            M.UnsetFlag(M.RegFlag.Sign);
            M.UnsetFlag(M.RegFlag.Zero);
        }
        else
        {
            M.UnsetFlag(M.RegFlag.Sign);
            M.SetFlag(M.RegFlag.Zero);
        }
        PrintResult("cmp", "", lDesc, rDesc, "", true);
    }

    public static void SubRmRm(bool w, M.Reg dest, M.Reg src)
    {
        M.debug("subtraction rm rm!");
        Sub(w, dest, src.name, src.lo, src.hi);
    }

    public static void Sub(bool w, M.Reg dest, string srcLoc, byte lo, byte hi)
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

