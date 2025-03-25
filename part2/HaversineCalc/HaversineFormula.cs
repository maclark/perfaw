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
        return a * Math.PI * 2L / 360L;
    }

    public static double Calc(double x0, double y0, double x1, double y1, double rEarth)
    {
        // i mean...do i have to do this?
        // won't all of the trig functions wrap
        if (x0 < 0) x0 += 360;
        else if (x0 > 180) x0 -= 360;
        if (x1 < 0) x1 += 360;
        else if (x1 > 180) x1 -= 360;
        if (y0 < 0) y1 += 180;
        else if (y0 > 90) y0 -= 180;
        if (y1 < 0) y1 += 180;
        else if (y1 > 180) y1 -= 180;

        double lon1 = x0;
        double lat1 = y0;
        double lon2 = x1;
        double lat2 = y1;

        double dLat = Deg2Rad(lat2 - lat1);
        double dLon = Deg2Rad(lon2 - lon1);
        lat1 = Deg2Rad(lat1);
        lat2 = Deg2Rad(lat2);

        double a = Square(Sin(dLat/2L)) + Cos(lat1) * Cos(lat2) * Square(Sin(dLon/2L));
        double c = 2L * Asin(Sqrt(a));

        double result = rEarth * c;
        return result; 
    }


}
