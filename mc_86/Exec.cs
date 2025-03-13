using System;
//using mc_86 = mc;

public static class Exec {

    public static void MoveRmRm(mc_86.Reg dest, mc_86.Reg src, bool w) {
        mc_86.debug("here we are now");
        string cached = mc_86.ToHex(mc_86.ToInt16(dest.lo, dest.hi));
        if (w) // i'm assuming this is how we know to use 1 or 2 bytes 
        {
            dest.hi = src.hi;
            dest.lo = src.lo;
            // idk if we're supposed to empty out src?
        }
        else 
        {
            if (dest.hi != 0) Console.WriteLine("are we supposed to not empty out the high bits? idk");
            dest.hi = 0;
            dest.lo = src.lo;
        }
        mc_86.debug("w: " + w);
        mc_86.debug(src.name + " src had " + src.lo);
        mc_86.debug(dest.name + " now has " + dest.lo);

        string movedData = mc_86.ToHex(mc_86.ToInt16(dest.lo, dest.hi)); 
        Console.WriteLine($"mov {dest.name}, {src.name} ; {dest.name}:{cached}->{movedData}");
    } 
}
