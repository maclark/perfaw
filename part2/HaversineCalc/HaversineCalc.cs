using System;

public class HaversineCalc
{

    public const double R_EARTH = 6372;

    public static void Main(string[] args)
    {

        Console.WriteLine("hello, sphere.");
        Console.WriteLine("Calc: " + HaversineFormula.Calc(0, 60, 0, 60, R_EARTH));
        
    }
}
