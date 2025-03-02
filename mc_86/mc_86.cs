using System;
using System.IO;


public class mc_86 {

    public enum Op {
        mov_rm_rm,  // 0b100010dw, 0bmodregrm
        mov_imm_rm, // 0b1100011w, 0bmod000rm
        mov_imm_reg,// 0b1011wreg, data...
        mov_mem_acc,// 0b1010000w, addr-lo, addr-hi
        mov_acc_mem,// 0b1010001w, addr-lo, addr-hi
        // skipping mov_rm_segr, mov_segr_rm
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
	
    private static bool debugPrints = false;
	private static byte[] content = new byte[0];
	private static int index = 0;

	private static bool GetByte(out byte b, bool checkingForNewOp=false) {
		b = 255;
		if (index < content.Length) {
			b = content[index];
			index++;
			return true;
		}
		else if (checkingForNewOp) {
            debug("checking for new op and finding nothing");
            return false;
        }
	    else {
            Console.WriteLine("uh oh, no more data, but we expected more data!?");
            return false;
        }
    }

	private static bool GetInt16(out int i) {
		i = 0;
		if (index < content.Length - 1) {
			BitConverter.ToInt16(new byte[] {content[index], content[index + 1]});
			index += 2;
			return true;
		}
		else return false;
	}
	
	private static string ToBinary(int b) { return Convert.ToString(b, 2).PadLeft(8, '0'); }
	private static void debug(string s) { if (debugPrints) Console.WriteLine(s); }

	private static string GetReg(bool memMode, int regNum, bool w, int mod=0, string disp = "") {
		string regName = "unknown";
		switch (regNum) {
			case 0b000:
				if (memMode) regName = "bx + si";
				else if (w) regName = "ax";
				else regName = "al";
				break;
			case 0b001:
				if (memMode) regName = "bx + di";
				else if (w) regName ="cx";
				else regName ="cl";
				break;
			case 0b010:
				if (memMode) regName = "bp + si";
				else if (w) regName ="dx";
				else regName ="dl";
				break;
			case 0b011:
				if (memMode) regName = "bp + di";
				else if (w) regName ="bx";
				else regName ="bl";
				break;
			case 0b100:
				if (memMode) regName = "si";
				else if (w) regName ="sp";
				else regName ="ah";
				break;
			case 0b101:
				if (memMode) regName = "di";
				else if (w) regName ="bp";
				else regName ="ch";
				break;
			case 0b110:
				if (memMode) {
                    if (mod != 0) regName = "bp";
                    else {
                        regName = "";
                        if (disp.Length > 3) disp = disp.Substring(3);
                    }
                }
				else if (w) regName ="si";
				else regName ="dh";
				break;
			case 0b111:
				if (memMode) regName = "bx";
				else if (w) regName = "di";
                else regName ="bh";
				break;
			default:
				Console.WriteLine($"unhandled register number: {Convert.ToString(regNum, 2)}");
				break;
		}

		if (memMode) return $"[{regName}{disp}]";
		else return regName;
	}

    public static bool GetData(bool w, out int data) {
        bool foundData = false;
        if (w) {
            if (GetByte(out var data0) && GetByte(out var data1)) {
                data = BitConverter.ToInt16(new byte[] {data0, data1});
                foundData = true;
            } 
            else data = 0;
        } 
        else if (GetByte(out var data0)) {
            data = data0;
            foundData = true;
        }
        else data = 0;
        return foundData;
    }

    private static bool ProcessModRegRM(out Ids ids) { 
        ids = new Ids(); 
        if (GetByte(out byte b)) {
            ids.mod = b >> 6;
            ids.reg = (b >> 3) & 0b111;
            ids.rm = b & 0b111;
            debug("mod " + ToBinary(ids.mod));
            debug("reg " + ToBinary(ids.reg));
            debug("rm " + ToBinary(ids.rm));
            GetModeAndDisp(ref ids);
            return true;
        }
        else return false;
    }

    private static void GetModeAndDisp(ref Ids ids) {
        // 00: memory mode, no disp*, except when r/m is 110, then 16-bit disp
        // 01: memory mode, 8-bit disp
        // 10: memory mode, 16-bit disp
        // 11: register mode
        if (ids.mod == 0b00) {
            // check for r/m is 110
            debug("00 mode");
            ids.memMode = true;
            if (ids.rm == 0b110) {
                if (GetByte(out var byte3) && GetByte(out var byte4)) {
                    int value = BitConverter.ToInt16(new byte[] {byte3, byte4});
                    ids.disp = $" {(value > 0 ? "+" : "-")} {Math.Abs(value).ToString()}";
                }
            }
        }	
        else if (ids.mod == 0b01) {
            debug("01 mode");
            ids.memMode = true;
            if (GetByte(out var byte3)) {
                if (byte3 != 0) ids.disp = $" + {byte3}";
            }
        }
        else if (ids.mod == 0b10) {
            debug("10 mode");
            ids.memMode = true;
            if (GetByte(out var byte3) && GetByte(out var byte4)) {
                int value = BitConverter.ToInt16(new byte[] {byte3, byte4});
                ids.disp = $" {(value > 0 ? "+" : "-")} {Math.Abs(value).ToString()}";
            }
        }
        else if (ids.mod == 0b11) {
            debug("11 mode");
            ids.memMode = false;
        }
        else Console.WriteLine($"error? {ids.mod}");
        debug("memoryMode: " + ids.memMode);
    }

    public class Ids {
        public bool memMode = false;
        public int mod = 0;
        public int reg = 0;
        public int rm = 0;
        public string disp = "";
    }

    private static bool Process() {
		if (!GetByte(out var byte1, true)) return false; // end of ops
        debug("");
        bool success = true;
        int b7 = byte1 >> 1;
        int b6 = byte1 >> 2;
        int b4 = byte1 >> 4;
        OpInfo info;
        if (op_codes_8b.TryGetValue(byte1, out info)) {
            // conditional jumps, all have 8 bits following
            if (GetByte(out var pointer)) {
                sbyte spointer = (sbyte)pointer;
                Console.WriteLine($"{info.transfer} {spointer}");
            }
            else success = false; 
        }
        // these are imm_reg stuff
        else if (op_codes_7b.TryGetValue(b7, out info)) {
            debug("found op: " + info.code + ", " + info.transfer);       
            bool w = (byte1 & 1) != 0;
            debug("w: " + w);

            string immReg = w ? "ax" : "al"; // looks like 0b11 is ax imm, and 0b10 is al imm, nothing else in book
            if (info.code == Op.mov_mem_acc) {
                if (GetData(w, out int data)) Console.WriteLine($"{info.transfer} {immReg}, [{data}]");
            }
            else if (info.code == Op.mov_acc_mem) {
                if (GetData(w, out int data)) Console.WriteLine($"{info.transfer} [{data}], {immReg}" );
            }
            else if (info.code == Op.add_imm_ac || info.code == Op.sub_imm_ac || info.code == Op.cmp_imm_ac) {
                debug("?_imm_ac");
                if (GetData(w, out int data)) Console.WriteLine($"{info.transfer} {immReg}, {data}");
            }
            else if (ProcessModRegRM(out Ids ids)) {
                string description = w ? "word" : "byte";
                if (GetData(w, out int data)) Console.WriteLine($"{info.transfer} {GetReg(ids.memMode, ids.rm, w, ids.mod, ids.disp)}, {description} {data}");
            }
        }
        else if (op_codes_6b.TryGetValue(b6, out info)) {
            debug("found op: " + info.code.ToString() + ", " + info.transfer);
            bool d = (byte1 & (1 << 1)) != 0;
            bool w = (byte1 & 1) != 0;
            debug("w: " + w);
            if (ProcessModRegRM(out Ids ids)) {
                int reg = ids.reg;
                if (info.code == Op.add_imm_rm) {
                    // this could be 1 of 3 actual ops
                    // and d is now s (1 is sign extend 8-bit to 16 if w is also 1)
                    debug("s: " + d);
                    bool s = d;
                    if (s || !w) debug("(s or !w), might be problematic");
                    if (reg == 0b000) info.transfer = "add";
                    else if (reg == 0b101) info.transfer = "sub";
                    else if (reg == 0b111) info.transfer = "cmp";
                    else {
                        Console.WriteLine($"unhandled 7bit code({info.code})with reg({reg})");
                        success = false;
                    }
                    int data = 0;
                    if (success) {
                        // from table 4-13 just listing out the possibilities...
                        // 00 reg8, imm8
                        // 01 reg16, imm16
                        // 10 reg8, imm8
                        // 11 reg16, imm8
                        // so if w is 0, both are small
                        // manual had  "if s: w=01"
                        // manual had  "if s: w=1" for CMP, which is confusing
                        if (GetData(!s && w, out data)) Console.WriteLine($"{info.transfer} {GetReg(ids.memMode, ids.rm, w, ids.mod, ids.disp)}, {data}");
                        else success = false;
                    }
                }
                else {
                    debug("d: " + d);
                    if (d) Console.WriteLine($"{info.transfer} {GetReg(false, ids.reg, w)}, {GetReg(ids.memMode, ids.rm, w, ids.mod, ids.disp)}");
                    else Console.WriteLine($"{info.transfer} {GetReg(ids.memMode, ids.rm, w, ids.mod, ids.disp)}, {GetReg(false, ids.reg, w)}");
                }
            }
            else success = false;
        }       
        else if (op_codes_4b.TryGetValue(b4, out info)) {
            debug("found op: " + info.code + ", " + info.transfer);
            bool w = (byte1 & 0b00001000) != 0;
            int reg = byte1 & 0b00000111;
            if (GetData(w, out var data)) Console.WriteLine($"{info.transfer} {GetReg(false, reg, w)}, {data}");
        }
        else {
            Console.WriteLine("couldn't extract op code from " + ToBinary(byte1));
            success = false;
        }
        return success;
	}

	public static void Main(string[] args) {
        string filePath = "";
        for (int i = 0; i < args.Length; i++) {
            string arg = args[i];
            if (arg.StartsWith('-')) {
               if (arg == "-d") debugPrints = true; 
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
        while (index < content.Length) {
            if (!Process()) break;
        }
    }   	
}
