using System.Text.Json.Serialization;

public class Pair
{
    [JsonInclude] public double x0;
    [JsonInclude] public double y0;
    [JsonInclude] public double x1;
    [JsonInclude] public double y1;
    
    public Pair() {
    }

    public Pair(double x0, double y0, double x1, double y1)
    {
        this.x0 = x0;
        this.y0 = y0;
        this.x1 = x1;
        this.y1 = y1;
    }
}
