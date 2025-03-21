using System;
using System.IO;

public class mc_86 {

    public enum RegSize
    {
        Full,
        High,
        Low,
    }

    public class Reg 
    {
        public string name;
        public byte hi;
        public byte lo;

        public Reg(string name) 
        {
            this.name = name;
            hi = 0;
            lo = 0;
        }
    }
    
    public class RegPt
    {
        public Reg reg;
        public string name;
        public RegSize size;

        public RegPt(Reg reg)
        {
            this.reg = reg;
            this.name = reg.name;
            this.size = RegSize.Full;
        }

        public RegPt(string name, Reg reg, RegSize size)
        {
            this.reg = reg;
            this.name = name;
            this.size = size;
        }
    }

    public static List<Reg> registers = new List<Reg>() {
        new Reg("ax"),
        new Reg("bx"),
        new Reg("cx"),
        new Reg("dx"),
        new Reg("sp"),
        new Reg("bp"),
        new Reg("si"),
        new Reg("di"),
    };

    public enum Op {
        mov_rm_rm,  // 0b100010dw, 0bmodregrm
        mov_imm_rm, // 0b1100011w, 0bmod000rm
        mov_imm_reg,// 0b1011wreg, data...
        mov_mem_acc,// 0b1010000w, addr-lo, addr-hi
        mov_acc_mem,// 0b1010001w, addr-lo, addr-hi
        add_rm_rm,  // 0b000000dw, 0bmodregrm
        add_imm_rm, // 0b100000sw, 0bmod000rm (first byte matches 2 others)               
        add_imm_ac, // 0b0000010w, data...                         
        sub_rm_rm,  // 0b001010dw, 0bmodregrm
        sub_imm_rm, // 0b100000sw, 0bmod101rm (first byte same as add_imm_rm)
        sub_imm_ac, // 0b0010110w, data...
        cmp_rm_rm,  // 0b001110dw, 0bmodregrm
        cmp_imm_rm, // 0b100000sw, 0bmod111rm (first byte same as add and sub imm_rm)
        cmp_imm_ac, // 0b0011110w, data...
        je,
        jl,
        jle,
        jb,
        jbe,
        jp,
        jo,
        js,
        jne,
        jnl,
        jnle,
        jnb,
        jnbe,
        jnp,
        jno,
        jns,
        loop,
        loopz,
        loopnz,
        jcxz,
    } 
    
    public struct OpInfo {
        public string transfer;
        public Op code;
        public OpInfo(Op code, string transfer) {
            this.transfer = transfer;
            this.code = code;
        }
    }

    public static Dictionary<int, OpInfo> op_codes_8b = new Dictionary<int, OpInfo>() {
      {0b01110100, new OpInfo(Op.je, "je")},
      {0b01111100, new OpInfo(Op.jl, "jl")},
      {0b01111110, new OpInfo(Op.jle, "jle")},
      {0b01110010, new OpInfo(Op.jb, "jb")},
      {0b01110110, new OpInfo(Op.jbe, "jbe")},
      {0b01111010, new OpInfo(Op.jp, "jp")},
      {0b01110000, new OpInfo(Op.jo, "jo")},
      {0b01111000, new OpInfo(Op.js, "js")},
      {0b01110101, new OpInfo(Op.jne, "jne")},
      {0b01111101, new OpInfo(Op.jnl, "jnl")},
      {0b01111111, new OpInfo(Op.jnle, "jnle")},
      {0b01110011, new OpInfo(Op.jnb, "jnb")},
      {0b01110111, new OpInfo(Op.jnbe, "jnbe")},
      {0b01111011, new OpInfo(Op.jnp, "jnp")},
      {0b01110001, new OpInfo(Op.jno, "jno")},
      {0b01111001, new OpInfo(Op.jns, "jns")},
      {0b11100010, new OpInfo(Op.loop, "loop")},
      {0b11100001, new OpInfo(Op.loopz, "loopz")},
      {0b11100000, new OpInfo(Op.loopnz, "loopnz")},
      {0b11100011, new OpInfo(Op.jcxz, "jcxz")},
    };

    public static Dictionary<int, OpInfo> op_codes_7b = new Dictionary<int, OpInfo>() {
        {0b1100011, new OpInfo(Op.mov_imm_rm, "mov")},
        {0b1010000, new OpInfo(Op.mov_mem_acc, "mov")},
        {0b1010001, new OpInfo(Op.mov_acc_mem, "mov")},
        {0b0000010, new OpInfo(Op.add_imm_ac, "add")},
        {0b0010110, new OpInfo(Op.sub_imm_ac, "sub")},
        {0b0011110, new OpInfo(Op.cmp_imm_ac, "cmp")},
    };

    public static Dictionary<int, OpInfo> op_codes_6b = new Dictionary<int, OpInfo>() {
        {0b100010, new OpInfo(Op.mov_rm_rm, "mov")},
        {0b000000, new OpInfo(Op.add_rm_rm, "add")},
        {0b100000, new OpInfo(Op.add_imm_rm, "add")}, // this matches sub/cmp imm_rm beware
        {0b001010, new OpInfo(Op.sub_rm_rm, "sub")},
        {0b001110, new OpInfo(Op.cmp_rm_rm, "cmp")},
    };

    public static Dictionary<int, OpInfo> op_codes_4b = new Dictionary<int, OpInfo>() {
        {0b1011, new OpInfo(Op.mov_imm_reg, "mov")},
    };

	public static int index = 0;
	public static int cachedIndex = 0;
    
    public static bool in_bounds = false;
    public static RegFlag flags = RegFlag.None;
    
    private static bool debugPrints = false;
	private static byte[] content = new byte[0];
    
    public static int ToInt16(byte lo, byte hi) 
    {
        ushort result;
        if (BitConverter.IsLittleEndian) result = BitConverter.ToUInt16(new byte[] { lo, hi });
        else result = BitConverter.ToUInt16(new byte[] { hi, lo});
        return result;
    }

    private static byte NextByte() 
    {
        if (index >= content.Length) 
        {
            index = 0;
            in_bounds = false;
            Console.WriteLine("WARNING: we've gone out of bounds!");
        }
        byte b = content[index];
        index++;
        return b;
    }

    public static string ToHex(int h, bool pad=false) { 
        uint uh = (uint)h;
        if (pad) return "0x" + h.ToString("X4");
        else return "0x" + h.ToString("X");
    }

	public static string ToBinary(int b) { return Convert.ToString(b, 2).PadLeft(8, '0'); }
	public static void debug(string s) { if (debugPrints) Console.WriteLine(s); }

    public static Reg FindReg(string name) 
    {
        Reg? reg = registers.Find(r => r.name == name);
        if (reg != null) return reg;
        else return new Reg("blank");
    }

	public static void GetMemory(Ids ids, bool w, out int address, out string addDesc)
    {
        address = 0;
		addDesc = "unknown";
        bool direct = false;
		switch (ids.rm) {
			case 0b000:
                address = Exec.GetInt(FindReg("bx")) + Exec.GetInt(FindReg("si"));
				addDesc = "bx+si";
                break;
			case 0b001:
                address = Exec.GetInt(FindReg("bx")) + Exec.GetInt(FindReg("di"));
				addDesc = "bx+di";
				break;
			case 0b010:
                address = Exec.GetInt(FindReg("bp")) + Exec.GetInt(FindReg("si"));
				addDesc = "bp+si";
				break;
			case 0b011:
                address = Exec.GetInt(FindReg("bp")) + Exec.GetInt(FindReg("di"));
				addDesc = "bp+di";
				break;
			case 0b100:
                address = Exec.GetInt(FindReg("si"));
                addDesc = "si";
				break;
			case 0b101:
                address = Exec.GetInt(FindReg("di"));
                addDesc = "di";
				break;
			case 0b110:
                if (ids.mod != 0) {
                    address = Exec.GetInt(FindReg("bp"));
                    addDesc = "bp";
                }
                else {
                    direct = true;
                    addDesc = "";
                }
				break;
			case 0b111:
                address = Exec.GetInt(FindReg("bx"));
				addDesc = "bx";
				break;
			default:
				Console.WriteLine($"unhandled register number: {Convert.ToString(ids.rm, 2)}");
				break;
		}
        
        address += ids.data;
        //if (string.IsNullOrEmpty(addDesc)) ids.disp = ids.disp.TrimStart('+');
        if (direct)
        {
            if (w) addDesc = $"word [{addDesc}{ids.disp}]";
            else addDesc = $"byte [{addDesc}{ids.disp}]";
        }
        else addDesc = $"[{addDesc}{ids.disp}]";
	}

	public static RegPt GetRP(int regNum, bool w) {
		switch (regNum) {
			case 0b000:
				if (w) return new RegPt(registers[0]);
                else return new RegPt("al", registers[0], RegSize.Low);
			case 0b001:
				if (w) return new RegPt(registers[2]);
                else return new RegPt("cl", registers[2], RegSize.Low);
			case 0b010:
				if (w) return new RegPt(registers[3]);
                else return new RegPt("dl", registers[3], RegSize.Low);
			case 0b011:
				if (w) return new RegPt(registers[1]);
                else return new RegPt("bl", registers[1], RegSize.Low);
			case 0b100:
				if (w) return new RegPt(registers[4]);
                else return new RegPt("ah", registers[0], RegSize.High);
			case 0b101:
				if (w) return new RegPt(registers[5]);
				else return new RegPt("ch", registers[2], RegSize.High);
			case 0b110:
				if (w) return new RegPt(registers[6]);
                else return new RegPt("dh", registers[3], RegSize.High);
			case 0b111:
				if (w) return new RegPt(registers[7]);
                else return new RegPt("bh", registers[1], RegSize.High);
			default:
				Console.WriteLine($"unhandled regNum: {Convert.ToString(regNum, 2)}");
                return new RegPt(new Reg("empty"));
		}
	}

    private static void ParseModRegRM(out Ids ids) { 
        ids = new Ids();
        byte b = NextByte();
        ids.mod = b >> 6;
        ids.reg = (b >> 3) & 0b111;
        ids.rm = b & 0b111;
        GetModeAndDisp(ref ids);
        debug($"mod {ToBinary(ids.mod)}, reg {ToBinary(ids.reg)}, rm {ToBinary(ids.rm)}, memMod {ids.memMode}, disp {ids.disp}");
    }

    private static void GetModeAndDisp(ref Ids ids) {
        // 00: memory mode, no disp*, except when r/m is 110, then 16-bit disp
        // 01: memory mode, 8-bit disp
        // 10: memory mode, 16-bit disp
        // 11: register mode
        if (ids.mod == 0b00) {
            // check for r/m is 110
            ids.memMode = true;
            if (ids.rm == 0b110) {
                ids.b0 = NextByte();
                ids.b1 = NextByte();
                ids.data = BitConverter.ToInt16(new byte[] { ids.b0, ids.b1 });
                ids.disp = $"{(ids.data > 0 ? "+" : "-")}{Math.Abs(ids.data).ToString()}";
            }
        }	
        else if (ids.mod == 0b01) {
            ids.memMode = true;
            ids.b0 = NextByte();
            ids.data = ids.b0;
            if (ids.b0 != 0) ids.disp = $"+{ids.b0}";
        }
        else if (ids.mod == 0b10) {
            ids.memMode = true;
            ids.b0 = NextByte();
            ids.b1 = NextByte();
            ids.data = BitConverter.ToInt16(new byte[] { ids.b0, ids.b1 });
            ids.disp = $"{(ids.data > 0 ? "+" : "-")}{Math.Abs(ids.data).ToString()}";
        }
        else if (ids.mod == 0b11) {
            ids.memMode = false;
        }
        else Console.WriteLine($"error {ids.mod}");
    }

    public class Ids {
        public bool memMode = false;
        public int mod = 0;
        public int reg = 0;
        public int rm = 0;
        public byte b0;
        public byte b1;
        public int data;
        public string disp = "";
    }

    private static void Process() {
        // probably not necessary, i think we're going to print out the "WARNING" every time
        if (index >= content.Length) return;

        cachedIndex = index; 
        byte byte1 = NextByte();
        int b7 = byte1 >> 1;
        int b6 = byte1 >> 2;
        int b4 = byte1 >> 4;
        OpInfo info;
        if (op_codes_8b.TryGetValue(byte1, out info)) {
            // (8bit) conditional jumps
            Exec.JumpIf(info.code, (sbyte)NextByte());
        }
        // these are imm_reg stuff
        else if (op_codes_7b.TryGetValue(b7, out info)) {
            bool w = (byte1 & 1) != 0;
            debug($"(7bit)found op: {info.code}, w: {w}");       
            string acReg = w ? "ax" : "al"; // looks like 0b11 is ax imm, and 0b10 is al imm, nothing else in book
            if (info.code == Op.mov_mem_acc) {
                byte byte2 = NextByte();
                if (w) 
                {
                    byte byte3 = NextByte();    
                    int data = BitConverter.ToInt16(new byte[] { byte2, byte3 });
                    Console.WriteLine($"{info.transfer} {acReg}, [{data}] (accumulator)");
                }
                else Console.WriteLine($"{info.transfer} {acReg}, [{byte2}] (accumulator)");
            }
            else if (info.code == Op.mov_acc_mem) {
                byte byte2 = NextByte();
                if (w) 
                {
                    byte byte3 = NextByte();    
                    int data = BitConverter.ToInt16(new byte[] { byte2, byte3 });
                    Console.WriteLine($"{info.transfer} [{data}], {acReg} (accumulator)");
                }
                else Console.WriteLine($"{info.transfer} [{byte2}], {acReg} (accumulator)");
            }
            else if (info.code == Op.add_imm_ac || info.code == Op.sub_imm_ac || info.code == Op.cmp_imm_ac) {
                debug("?_imm_ac");
                byte byte2 = NextByte();
                if (w) 
                {
                    byte byte3 = NextByte();    
                    int data = BitConverter.ToInt16(new byte[] { byte2, byte3 });
                    Console.WriteLine($"{info.transfer} {acReg}, [{data}] (accumulator)");
                }
                else Console.WriteLine($"{info.transfer} {acReg}, [{byte2}] (accumulator)");
            }
            else 
            {
                // not using accumulator i think
                debug("op.mov_imm_rm");
                System.Diagnostics.Debug.Assert(info.code == Op.mov_imm_rm);
                ParseModRegRM(out Ids ids);
                byte lo = NextByte();
                byte hi = w ? NextByte() : (byte)0;
                if (ids.memMode) 
                {
                    GetMemory(ids, w, out int address, out string addDesc); 
                    addDesc = (w ? "word " : "byte ") + addDesc;
                    Exec.MovImmMem(w, address, addDesc, lo, hi);
                }
                else Exec.MovImmReg(w, GetRP(ids.rm, w), lo, hi);
            }
        }
        else if (op_codes_6b.TryGetValue(b6, out info)) {
            debug("(6bit)found op: " + info.code.ToString() + ", " + info.transfer);
            bool d = (byte1 & (1 << 1)) != 0;
            bool w = (byte1 & 1) != 0;
            debug("w: " + w);
            debug("d: " + d);
            ParseModRegRM(out Ids ids);
            
            int reg = ids.reg;
            if (info.code == Op.add_imm_rm) {
                // this could be 1 of 3 actual ops
                // and d is now s (1 is sign extend 8-bit to 16 if w is also 1)
                // from table 4-13 just listing out the possibilities...
                // 00 reg8, imm8
                // 01 reg16, imm16
                // 10 reg8, imm8
                // 11 reg16, imm8
                // so if w is 0, both are small
                // manual had  "if s: w=01"
                // manual had  "if s: w=1" for CMP, which is confusing
                // d is 's' in these cases with an immediate value
                bool wReal = !d && w;
                byte b0 = NextByte();
                byte b1 = wReal ? NextByte() : (byte)0;
                if (ids.memMode)
                {
                    GetMemory(ids, w, out int address, out string addDesc);
                    addDesc = (w ? "word " : "byte ") + addDesc;
                    if (reg == 0b000) Exec.AddImmMem(w, b0, b1, address, addDesc);
                    else if (reg == 0b101) Exec.SubImmMem(w, b0, b1, address, addDesc);
                    else if (reg == 0b111) Exec.CmpImmMem(w, b0, b1, address, addDesc);
                    else Console.WriteLine($"ERROR: unhandled code({info.code})with reg field({reg})");
                }
                else 
                {
                    RegPt rp = GetRP(ids.rm, w);
                    if (reg == 0b000) Exec.AddImmReg(w, b0, b1, rp);
                    else if (reg == 0b101) Exec.SubImmReg(w, b0, b1, rp);
                    else if (reg == 0b111) Exec.CmpImmReg(w, b0, b1, rp);
                    else Console.WriteLine($"ERROR: unhandled code({info.code})with reg field({reg})");
                }
            }
            else { 
                // this is where 6b codes that aren't imm_x goes!
                // i assume it's all xx_rm_rm
                // we have parsed mod reg rm
                int address = 0;
                string addDesc = "undescribed";
                RegPt? dest, src, used;
                if (d) 
                {
                    dest = GetRP(ids.reg, w);
                    if (ids.memMode) 
                    {
                        src = new RegPt(new Reg("empty"));
                        GetMemory(ids, w, out address, out addDesc);
                    }
                    else src = GetRP(ids.rm, w);
                    used = dest;
                }
                else 
                {
                    src = GetRP(ids.reg, w);
                    if (ids.memMode)
                    {
                        dest = new RegPt(new Reg("empty"));
                        GetMemory(ids, w, out address, out addDesc);
                        // we know the source is operand[0] and it is mem, bc d is false
                        // this is just from looking at casey's github
                        // text.cpp says to add these prefixes when operand[0] is not a register i guess
                        addDesc = (w ? "word " : "byte ") + addDesc;
                    }
                    else dest = GetRP(ids.rm, w);
                    used = src;
                }
               
                switch (info.transfer) 
                {
                    case "mov":
                        if (ids.memMode) Exec.MovRegMem(d, w, used, address, addDesc);
                        else Exec.MovRegReg(w, dest, src);
                        break;
                    case "add":
                        if (ids.memMode) Exec.AddRegMem(d, w, used, address, addDesc); 
                        else Exec.AddRegReg(w, dest, src);   
                        break;
                    case "sub":
                        if (ids.memMode) Exec.SubRegMem(d, w, used, address, addDesc);
                        else Exec.SubRegReg(w, dest, src);   
                        break;
                    case "cmp":
                        if (ids.memMode) Exec.CmpRegMem(d, w, used, address, addDesc);
                        else Exec.CmpRegReg(w, dest, src);   
                        break;
                    default:
                        Console.WriteLine($"ERROR: unhandled transfer, {info.transfer}");
                        break;
               }
            }
        }       
        // i believe the only 4b op code we have is mov_imm_reg
        else if (op_codes_4b.TryGetValue(b4, out info)) {
            debug("(4bit)found op: " + info.code + ", " + info.transfer);
            if (info.code != Op.mov_imm_reg) Console.WriteLine("WARNING: unhandled op");
            bool w = (byte1 & 0b00001000) != 0;
            int regNum = byte1 & 0b00000111;
            debug("regNum " + ToBinary(regNum));
            debug("w: " + w);
            Exec.MovImmReg(w, GetRP(regNum, w), NextByte(), w ? NextByte() : (byte)0); 
        }
        else Console.WriteLine("couldn't extract op code from " + ToBinary(byte1));
	}

    [Flags]
    public enum RegFlag : byte
    {
        None = 0,
        Zero = 1 << 0, // 0b1,
        Sign = 1 << 1, //0b10,
    }

    public static void SetFlag(RegFlag f) 
    {
        flags |= f;
    }

    public static void UnsetFlag(RegFlag f) 
    {
        flags &= ~f;
    }

    public static string GetFlags() 
    {
        string flagPrint = "";
        if ((mc_86.flags & RegFlag.Sign) != 0) flagPrint += "S";
        if ((mc_86.flags & RegFlag.Zero) != 0) flagPrint += "Z";
        return flagPrint;
    }

    public static bool CheckFlag(RegFlag f)
    {
        return (flags & f) != 0;
    }

	public static void Main(string[] args) {
        string filePath = "";
        bool dumpMemory = false;
        for (int i = 0; i < args.Length; i++) {
            string arg = args[i];
            if (arg.StartsWith('-')) {
               if (arg == "-d") debugPrints = true; 
               if (arg == "-dump") dumpMemory = true;
            }   
            else {
                filePath = arg;
                if (i < args.Length - 1) Console.WriteLine("ignoring extra arguments");
                break; // maybe it's now a file name
            }     
        }

		if (File.Exists(filePath)) {
			content = File.ReadAllBytes(filePath);
		}
		else {
			Console.WriteLine("couldn't find file " + filePath);
			return;
		}

		if (content.Length < 2) {
        	Console.WriteLine("how many bytes are there? " + content.Length);
            return;
		}
		
		Console.WriteLine($"processing file {filePath}");
        Console.WriteLine("");
        in_bounds = true;
        index = 0;
        while (index < content.Length && in_bounds) {
            debug("");
            debug($"{ToBinary(content[index])}-{ToBinary(content[index + 1])}");
            Process();
            if (debugPrints)
            {
                Console.WriteLine("press enter (or maybe any key) to continue...");
                Console.ReadLine();
            }
        }

        Console.WriteLine("\nFinal registers:");
        for (int i = 0; i < registers.Count; i++) {
            Reg r = registers[i];
            int data = ToInt16(r.lo, r.hi); 
            if (data != 0) Console.WriteLine("{0,8}: {1} {2}", r.name, ToHex(data, true), $"({data})");
        }
        Console.WriteLine("{0,8}: {1} {2}", "ip", ToHex(index, true), $"({index})");
        Console.WriteLine("{0,8}: {1}", "flags", GetFlags());

        if (dumpMemory)
        {
            Console.WriteLine("dumping memory");
            Imaginer.ConvertToPNG(Exec.memory);
        }
    }   	
}
