using System;

// we want
// to generate
// a json
// with x paris of (x,y) aka (lon,lat)
// a method to cluster them
// anything else?
public class HaversineCalc
{

    public const double R_EARTH = 6372;

    public static void Main(string[] args)
    {
        Generator.GeneratePairs(true, 100);    
        Pair[] pairs = Generator.ParsePairs("pairs.json");
        
        double average = 0;
        for (int i = 0; i < pairs.Length; i++)
        {
            Pair p = pairs[i];
            average += HaversineFormula.Calc(p.x0, p.y0, p.x1, p.y1, R_EARTH);
        }
        if (pairs.Length > 0) average /= pairs.Length;
        else Console.WriteLine("0 pairs"); 

        Console.WriteLine("average distance: " + average);
    }
}
