using System;
using System.Text.Json;

public static class Generator
{
    public static void GeneratePairs(bool cluster, int count)
    {
        Random rand = new Random(0);
        // i guess x goes from -180 to +180 (lon)
        // while y goes from -90 to +90 (lat)
        int clusterCount = (int)Math.Ceiling(count * .1);
        Tuple<double, double>[] clusters = new Tuple<double, double>[clusterCount];
        for (int i = 0; i < clusterCount; i++)
        {
            double x = rand.NextDouble() - .5;
            double y = rand.NextDouble() - .5;
            clusters[i] = new Tuple<double, double>(x * 180, x * 90);
        }

        double xSpread = 15;
        double ySpread = 15;
        Pair[] pairs = new Pair[count];
        for (int i = 0; i < count; i++)
        {
            Tuple<double, double> c = clusters[rand.Next(0, clusters.Length)];
            double x0 = c.Item1 + xSpread * (rand.NextDouble() - .5);
            double y0 = c.Item2 + ySpread * (rand.NextDouble() - .5);
            double x1 = c.Item1 + xSpread * (rand.NextDouble() - .5);
            double y1 = c.Item2 + ySpread * (rand.NextDouble() - .5);
            pairs[i] = new Pair(x0, y0, x1, y1);
        }
            
        string json = JsonSerializer.Serialize(pairs, new JsonSerializerOptions { WriteIndented = true });

        System.IO.File.WriteAllText("pairs.json", json);
    }

    public static Pair[] ParsePairs(string fileName)
    {

        if (!File.Exists(fileName))
        {
            Console.WriteLine("can't find json file at " + fileName);
            return new Pair[0];
        }
        
        string json = File.ReadAllText(fileName);
        Pair[]? pairs = JsonSerializer.Deserialize<Pair[]>(json, new JsonSerializerOptions { IncludeFields = true });

        if (pairs == null)
        {
            Console.WriteLine("json deserialization failed!");
            return new Pair[0];
        }

        return pairs;
    }
}
