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

        // computing haversine on generated pairs
        double average = 0;
        for (int i = 0; i < pairs.Length; i++)
        {
            Pair p = pairs[i];
            average += HaversineFormula.Calc(p.x0, p.y0, p.x1, p.y1, HaversineCalc.R_EARTH);
        }
        if (pairs.Length > 0) average /= pairs.Length;
        else Console.WriteLine("0 pairs"); 
        Console.WriteLine("generated average distance: " + average);
            
        string json = JsonSerializer.Serialize(pairs, new JsonSerializerOptions { WriteIndented = true });

        System.IO.File.WriteAllText("pairs.json", json);
    }

    public static void AssignVar(ref Pair pair, List<char> integerPart, List<char> fractionalPart, int varIndex) {
        string s_value = "";
        for (int i = 0; i < integerPart.Count; i++) {
            s_value += integerPart[i];
        }
        s_value += ".";
        for (int i = 0; i < fractionalPart.Count; i++) {
            s_value += fractionalPart[i];
        }
        Double.TryParse(s_value, out double value); 

        switch (varIndex) {
            case 0:
                pair.x0 = value;
                break;
            case 1:
                pair.y0 = value;
                break;
            case 2:
                pair.x1 = value;
                break;
            case 3:
                pair.y1 = value;
                break;
            default:
                Console.WriteLine("WARNING: unhandled pair var index: " + varIndex);
                break;
        }
    }

    public static Pair[] ParsePairs(string fileName) {

        if (!File.Exists(fileName))
        {
            Console.WriteLine("can't find json file at " + fileName);
            return new Pair[0];
        }
        
        string json = File.ReadAllText(fileName);

        Console.WriteLine("parsing...");

        List<Pair> pairs = new List<Pair>();
        Pair pair = new Pair();
        int pairValueIndex = 0;
        List<char> integerPart = new List<char>();
        List<char> fractionalPart = new List<char>();
        HashSet<char> digits = new HashSet<char>() {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
        bool passedDecimal = false;
        bool readingVarName = false;
        for (int i = 0; i < json.Length; i++) {
            if (!readingVarName && digits.Contains(json[i])) {
                if (passedDecimal) {
                    //Console.WriteLine("adding to fractional: " + json[i]);     
                    fractionalPart.Add(json[i]);
                }
                else {
                    //Console.WriteLine("adding to integer: " + json[i]);
                    integerPart.Add(json[i]); 
                }
            }
            else {
                switch (json[i]) {
                    case '"':
                        readingVarName = !readingVarName;
                        break;

                    case '}':
                        AssignVar(ref pair, integerPart, fractionalPart, pairValueIndex);
                        integerPart.Clear();
                        fractionalPart.Clear();
                        pairs.Add(pair);

                        //Console.WriteLine($"adding pair x0:{pair.x0}, y0:{pair.y0}, x1:{pair.x1}, y1:{pair.y1}");
                        pair = new Pair();
                        pairValueIndex = 0;
                        passedDecimal = false;
                        break;

                    case ',':
                        if (integerPart.Count > 0) {
                            AssignVar(ref pair, integerPart, fractionalPart, pairValueIndex);
                            integerPart.Clear();
                            fractionalPart.Clear();
                            pairValueIndex++;
                            passedDecimal = false;
                        }
                        break;

                    case '.':
                        passedDecimal = true;
                        break;
                }
            }
        }


        return pairs.ToArray();
    }

    public static Pair[] OldParsePairs(string fileName)
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
