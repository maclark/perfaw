using System;
using System.IO;


public class mc_86 {

    public enum Op {
        mov_rm_rm,  // 0b100010dw, 0bmodregrm
        mov_imm_rm, // 0b1100011w, 0bmod000rm
        mov_imm_reg,// 0b1011wreg, data...
        // skipping mem_acc, acc_mem, rm_segr, segr_rm
        add_rm_rm,  // 0b000000dw, 0bmodregrm
        add_imm_rm, // 0b100000sw, 0bmod000rm (first byte matches 2 others)               
        add_imm_ac, // 0b0000010w, data...                         
        sub_rm_rm,  // 0b001010dw, 0bmodregrm
        sub_imm_rm, // 0b100000sw, 0bmod101rm (first byte same as add_imm_rm)
        sub_imm_ac, // 0b0010110w, data...
        cmp_rm_rm,  // 0b001110dw, 0bmodregrm
        cmp_imm_rm, // 0b100000sw, 0bmod111rm (first byte same as add and sub imm_rm)
        cmp_imm_ac, // 0b0011110w, data...
        jz, // je?
        jnz, // jne?
    } 

    public struct OpInfo {
        public string transfer;
        public Op code;
        public OpInfo(Op code, string transfer) {
            this.transfer = transfer;
            this.code = code;
        }
    }

    public static Dictionary<int, OpInfo> op_codes_7b = new Dictionary<int, OpInfo>() {
        {0b1100011, new OpInfo(Op.mov_imm_rm, "mov")},
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

    private static void Process() {
		if (!GetByte(out var byte1, true)) return; // end of ops
        debug("");
        bool failed = false;
        int b7 = byte1 >> 1;
        int b6 = byte1 >> 2;
        int b4 = byte1 >> 4;
        OpInfo info;
        // these are imm_reg stuff
        if (op_codes_7b.TryGetValue(b7, out info)) {
            debug("found op: " + info.code + ", " + info.transfer);       
            bool w = (byte1 & 1) != 0;
            debug("w: " + w);
            if (ProcessModRegRM(out Ids ids)) {
                string description = w ? "word" : "byte";
                if (GetData(w, out int data)) Console.WriteLine($"{info.transfer} {GetReg(ids.memMode, ids.rm, w, ids.mod, ids.disp)}, {description} {data}");
            }
        }
        else if (op_codes_6b.TryGetValue(b6, out info)) {
            debug("found op: " + info.code.ToString() + ", " + info.transfer);
            bool d = (byte1 & (1 << 1)) != 0;
            bool w = (byte1 & 1) != 0;
            debug("d: " + d);
            debug("w: " + w);
            if (info.code == Op.add_imm_rm) {
                // this could be 1 of 3 actual ops
                Console.WriteLine("unimplemented...3 ops to choose from now");
                failed = true;
            }
            else {
                if (ProcessModRegRM(out Ids ids)) {
                    if (d) Console.WriteLine($"{info.transfer} {GetReg(false, ids.reg, w)}, {GetReg(ids.memMode, ids.rm, w, ids.mod, ids.disp)}");
                    else Console.WriteLine($"{info.transfer} {GetReg(ids.memMode, ids.rm, w, ids.mod, ids.disp)}, {GetReg(false, ids.reg, w)}");
                }
                else failed = true;
            }
        }       
        else if (op_codes_4b.TryGetValue(b4, out info)) {
            debug("found op: " + info.code + ", " + info.transfer);
            bool w = (byte1 & 0b00001000) != 0;
            int reg = byte1 & 0b00000111;
            if (GetData(w, out var data)) Console.WriteLine($"{info.transfer} {GetReg(false, reg, w)}, {data}");
        }
        else {
            Console.WriteLine("couldn't extract op code from " + ToBinary(byte1));
            failed = true;
        }
        if (!failed) Process();
	}



/*private static void ProcessOps() {

		if (!GetByte(out var byte1)) return; // end of ops

		debug(ToBinary(byte1));
		// check for the 6 bit op...
		int b7 = byte1 >> 1;
		int b6 = byte1 >> 2;
		int b4 = byte1 >> 4;

		bool failed = false;	
		if (b6 == 0b100010) {



			bool d = (byte1 & 1 << 1) != 0;
			bool w = (byte1 & 1) != 0;

			if (GetByte(out var byte2)) {
				
				debug(ToBinary(byte2));
				
				int mod = byte2 >> 6;
				int reg = (byte2 >> 3) & 0b00000111;
				int rm = byte2 & 0b00000111;

				// 00: memory mode, no disp*, except when r/m is 110, then 16-bit disp
				// 01: memory mode, 8-bit disp
				// 10: memory mode, 16-bit disp
				// 11: register mode
				bool memoryMode = false;
				string disp = "";
				if (mod == 0b00) {
					// check for r/m is 110
					debug("00 mode");
					mode = true;
					if (rm == 0b110) {
						if (GetByte(out var byte3) && GetByte(out var byte4)) {
							// maybe it should be {byte4, byte3} bc 2nd byte is "most significant"?
							disp = $" + {BitConverter.ToInt16(new byte[] {byte4, byte3})}";
						}
						else {
							Console.WriteLine($"mod(e) was 11, but there wasn't another 2 bytes available!");
							failed = true;
						}
					}
				}	
				else if (mod == 0b01) {
					debug("01 mode");
					mode = true;

					if (GetByte(out var byte3)) {
						if (byte3 != 0) disp = $" + {byte3}";
					}
					else {
						Console.WriteLine($"mod(e) was 01, but there wasn't another 1 byte available!");
						failed = true;
					}
				}
				else if (mod == 0b10) {
					debug("10 mode");
					mode = true;
					if (GetByte(out var byte3) && GetByte(out var byte4)) disp = $" + {BitConverter.ToInt16(new byte[] {byte3, byte4})}";
					else {
						Console.WriteLine($"mod(e) was 10, but there wasn't another 2 bytes available!");
						failed = true;
					}
				}
				else if (mod == 0b11) {
					debug("11 mode");
					mode = false;
				}
				else Console.WriteLine($"error? {mod}");
				debug("rm: " + ToBinary(rm));
				debug("d: " + d);
				debug("w: " + w);
				if (d) Consol.WriteLine($"mov {GetReg(false, reg, w)}, {GetReg(mode, rm, w, disp)}");
				else Console.WriteLine($"mov {GetReg(mode, rm, w, disp)}, {GetReg(false, reg, w)}");
			}
			else {
				failed = true;
				//Console.WriteLine($"couldn't finish processing {op}, not enough bytes");
			}
		}
		else if (b7 == 0b1100011) {
			Console.WriteLine("how did we end up here? " + ToBinary(b7));
			if (GetByte(out var byte2)) {
				//ProcessByte2(byte2, out bool memMode, out int reg, out int rm, out string disp);
				// note rm is 000
				//
				//
			//	int data;
			//	if (w) GetInt16(out data);
			//	else GetByte(out data);
			//	string disp = GetDisplacement(mod, rm);
				
			//	if (memMode) 
			//	Console.WriteLine("mov " + dest + ", " + src);
				//WriteASM("mov", d ? , w, memMode, disp);
			}
		}	
		else if (b4 == 0b1011) {
			//Op op = Op.mov_im_reg;
			debug(op.ToString());
			bool w = (byte1 & 0b00001000) != 0;
			int reg = byte1 & 0b00000111;
			debug("w: " + w);	
			if (GetByte(out var byte2)) {
				debug(ToBinary(byte2));
				if (w) {
					if (GetByte(out var byte3)) {
						int data = BitConverter.ToInt16(new byte[] {byte2, byte3}, 0);
						Console.WriteLine($"mov {GetReg(false, reg, w)}, {data}");
					}
					else {
						failed = true;
						Console.WriteLine($"couldn't finish processing {op}, not enough bytes");
					}
				}
				else {
					Console.WriteLine($"mov {GetReg(false, reg, w)}, {byte2}");
				}

			}
			else {
				failed = true;
				Console.WriteLine($"couldn't finish processing {op}, not enough bytes");
			}


		}
		else if (b7 == 0b1010000) {
			Console.WriteLine("how did we end up here? " + ToBinary(b7));
		}
		else if (b7 == 0b1010001) {
			Console.WriteLine("how did we end up here? " + ToBinary(b7));
		}
		else {
			failed = true;
			Console.WriteLine($"couldn't extract op code from byte: {Convert.ToString(byte1, 2).PadLeft(8, '0')}");
		}
		
		// we don't know how many bytes to advance, if we didn't successfully process op code!
		if (!failed) ProcessOps();
    }*/

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
		Process();
    }   	
}
