using System;
using System.IO;


public class mc_86 {

	public enum Op {
		mov_regmem_to_regmem,
		mov_im_regmem,
		mov_im_reg,
		mov_mem_acc,
		mov_acc_mem,
	}

	private static bool debugPrints = true;
	private static byte[] content = new byte[0];
	private static int index = 0;

	private static bool GetByte(out byte b) {
		b = 255;
		if (index < content.Length) {
			b = content[index];
			index++;
			return true;
		}
		else return false;
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
	private static void print(string s) => Console.WriteLine(s);
	private static void debug(string s) { if (debugPrints) Console.WriteLine(s); }

	private static void ProcessOps() {

		if (!GetByte(out var byte1)) {
			return;
		}


		debug("");
		debug(ToBinary(byte1));
		// check for the 6 bit op...
		int b7 = byte1 >> 1;
		int b6 = byte1 >> 2;
		int b4 = byte1 >> 4;

		bool failed = false;	
		if (b6 == 0b100010) {


			Op op = Op.mov_regmem_to_regmem;

			bool d = (byte1 & 0b00000010) != 0;
			d = (byte1 & 1 << 1) != 0;
			bool w = (byte1 & 0b00000001) != 0;
			w = (byte1 & 1) != 0;

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
				string displacement = "";
				if (mod == 0b00) {
					// check for r/m is 110
					debug("00 mode");
					memoryMode = true;
					if (rm == 0b110) {
						if (GetByte(out var byte3) && GetByte(out var byte4)) {
							// maybe it should be {byte4, byte3} bc 2nd byte is "most significant"?
							displacement = $" + {BitConverter.ToInt16(new byte[] {byte4, byte3})}";
						}
						else {
							Console.WriteLine($"mod(e) was 11, but there wasn't another 2 bytes available!");
							failed = true;
						}
					}
				}	
				else if (mod == 0b01) {
					debug("01 mode");
					memoryMode = true;

					if (GetByte(out var byte3)) {
						if (byte3 != 0) displacement = $" + {byte3}";
					}
					else {
						Console.WriteLine($"mod(e) was 01, but there wasn't another 1 byte available!");
						failed = true;
					}
				}
				else if (mod == 0b10) {
					debug("10 mode");
					memoryMode = true;
					if (GetByte(out var byte3) && GetByte(out var byte4)) displacement = $" + {BitConverter.ToInt16(new byte[] {byte3, byte4})}";
					else {
						Console.WriteLine($"mod(e) was 10, but there wasn't another 2 bytes available!");
						failed = true;
					}
				}
				else if (mod == 0b11) {
					debug("11 mode");
					memoryMode = false;
				}
				else Console.WriteLine($"error? {mod}");
				debug("rm: " + ToBinary(rm));
				debug("d: " + d);
				debug("w: " + w);
				if (d) Console.WriteLine($"mov {GetReg(false, reg, w)}, {GetReg(memoryMode, rm, w, displacement)}");
				else Console.WriteLine($"mov {GetReg(memoryMode, rm, w, displacement)}, {GetReg(false, reg, w)}");
			}
			else {
				failed = true;
				Console.WriteLine($"couldn't finish processing {op}, not enough bytes");
			}
		}
		else if (b7 == 0b1100011) {
			Console.WriteLine("how did we end up here? " + ToBinary(b7));
			if (GetByte(out var byte2)) {
				//ProcessByte2(byte2, out bool memMode, out int reg, out int rm, out string disp);
				// note rm is 000
				//
				//
				/*
				int data;
				if (w) GetInt16(out data);
				else GetByte(out data);
				string disp = GetDisplacement(mod, rm);
				
				if (memMode) 
				Console.WriteLine("mov " + dest + ", " + src);
				//WriteASM("mov", d ? , w, memMode, disp);
				*/

			}
		}	
		else if (b4 == 0b1011) {
			Op op = Op.mov_im_reg;
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
    	}

	private static string GetReg(bool memMode, int regNum, bool w, string disp = "") {
		string regName = "unknown";
		Console.WriteLine("memMode: " + memMode);
		Console.WriteLine("regNum: " + ToBinary(regNum));	
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
				if (memMode) regName = "bp";
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
 	
	public static void Main(string[] args) {
		string filePath = "C:\\MaxClark\\perfaw\\listing_0038_many_register_mov";
		if (args.Length > 0) filePath = args[0];
		if (args.Length > 1) Console.WriteLine("ignoring multiple arguments...");
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
		ProcessOps();
    	}   	
}
