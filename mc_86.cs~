using System;
using System.IO;


public class mc_86 {

	public enum op {
		mov_regmem_to_regmem,
		mov_im_regmem,
		mov_reg,
		mov_mem_acc,
		mov_acc_mem,
	}




	public static void Main(string[] args) {


		string filePath = "C:\\Users\\Max\\Downloads\\listing_0037_single_register_mov";
		if (args.Length > 0) filePath = args[0];
		if (args.Length > 1) Console.WriteLine("ignoring multiple arguments...");



		byte[] content = new byte[0];
		if (File.Exists(filePath)) {
			Console.WriteLine("found file " + filePath);
			content = File.ReadAllBytes(filePath);
		}
		else {
			Console.WriteLine("couldn't find file " + filePath);
			return;
		}


		for (;;) {
			
			
		}


		byte a = content[0];
		byte b = content[1];
        	Console.WriteLine($"byte a: {Convert.ToString(a, 2)}");
        	Console.WriteLine("byte b " + b);

		byte c = (byte)(a >> 2);
		bool matches = (c == 0b100010);
		if (matches) {
			Console.WriteLine("we have mov!");


		}
		else {
			Console.WriteLine($"didn't match! {c} with 100010");
			return;
		}



    }
}
