using System;
using static System.Math;

public static class HaversineFormula
{

    public static double Square(double a)
    {
        return a * a;
    }

    public static double Deg2Rad(double a)
    {
        Console.WriteLine(a * Math.PI * 2L / 360L);
        return a * Math.PI * 2L / 360L;
    }

    public static double Calc(double x0, double x1, double y0, double y1, double rEarth)
    {

        double lat1 = y0;
        double lat2 = y1;
        double lon1 = x0;
        double lon2 = x1;


        double dLat = Deg2Rad(lat2 - lat1);
        double dLon = Deg2Rad(lon2 - lon1);
        Console.WriteLine("dlat: " + dLat);
        Console.WriteLine("dlon: " + dLon);


        double a = Square(Sin(dLat/2L)) + Cos(lat1) * Cos(lat2) * Square(Sin(dLon/2L));
        Console.WriteLine("a " + a);
        double c = 2L * Asin(Sqrt(a));

        Console.WriteLine("c " + c);
        double result = rEarth * c;
        return result; 
    }


}
