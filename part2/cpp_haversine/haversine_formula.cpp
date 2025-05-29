/* This is all just copied from casey muratori's shit
 * i guess we'll have to define the trig functions somewhere
 *  and the f64
 */


static f64 Square(f64 A) 
{
    f64 Result = A*A;
    return Result;
}

static f64 Deg2Rad(f64 Degrees)
{
    f64 Result = 0.01745329251994329577 * Degrees;
    return Result;
}

// casey says the earth's radius is 6472.8
static f64 ReferenceHaversine(f64 X0, f64 Y0, f64 X1, f64 Y1, f64 R_Earth)
{
    f64 lat1 = Y0;
    f64 lat2 = Y1;
    f64 lon1 = X0;
    f64 lon2 = X1;

    f64 dLat = Deg2Rad(lat2 - lat1);
    f64 dLon = Deg2Rad(lon2 - lon1);
    lat1 = Deg2Rad(lat1);
    lat2 = Deg2Rad(lat2);

    f64 a = Square(sin(dLat/2.0)) + cos(lat1)*cos(lat2)*Square(sin(dLon)/2);
    f64 c = 2.0*asin(sqrt(a));

    f64 Result = R_Earth * c;

    return Result;
}

