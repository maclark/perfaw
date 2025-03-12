using System;
//using mc_86 = mc;

public static class Exec {

    public static void MoveRmImm(mc_86.Reg dest, bool w, int data) 
    //public static void MoveRmImm(mc_86.Reg dest, bool w, byte hi, byte low) 
    {
        // i assume if w is false, we leave the high bits undisturbed?
        if (w) dest.hi = hi;
        dest.lo = lo;
    }
    
    public static void MoveRmRm(mc_86.Reg dest, mc_86.Reg src, bool w) {
        mc_86.debug("here we are now");
        if (w) // i'm assuming this is how we know to use 1 or 2 bytes 
        {
           dest.hi = src.hi;
           dest.lo = src.lo;
           // idk if we're supposed to empty out src?
           
        }
        else 
        {
            dest.hi = 0;
            dest.lo = src.lo;
        }
        Console.WriteLine("w: " + w);
        //mc_86.debug(dest.name + " now has " + dest.lo);
        Console.WriteLine(src.name + " src had " + src.lo);
        Console.WriteLine(dest.name + " now has " + dest.lo);
    } 


}
