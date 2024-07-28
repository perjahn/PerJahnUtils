using System;
using System.IO;
using System.Linq;

namespace LimitReacher
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine(
@"LimitReacher 1.0 - Calculates Y Intercept using Linear Regression.

Usage: LimitReacher <filename> <limit>

File should contain rows with two tab separated columns: date value
First row of file is excluded (can contain column names or whatever).
Date should be parsable to DateTime.
Value should be parsable to double.");
                return 1;
            }

            var filename = args[0];
            if (!File.Exists(filename))
            {
                Console.WriteLine($"File not found: '{filename}'");
                return 1;
            }

            if (!double.TryParse(args[1], out double limit))
            {
                Console.WriteLine($"Couldn't parse limit: '{args[1]}'");
                return 1;
            }

            var rows = File.ReadAllLines(filename);

            (DateTime date, double value)[] values = [.. rows
                .Skip(1)
                .Where(r => r.Contains('\t'))
                .Select(r => (date: DateTime.Parse(r.Split('\t')[0]), value: limit - double.Parse(r.Split('\t')[1])))];

            var y = GetLinearRegressionYIntercept([.. values.Select(v => v.value)], [.. values.Select(v => (double)v.date.Ticks)]);

            DateTime d = new((long)y);
            Console.WriteLine($"Y intercept: {d}");

            return 0;
        }

        static double GetLinearRegressionYIntercept(double[] xvalues, double[] yvalues)
        {
            (double x, double y)[] values = [.. xvalues.Zip(yvalues, (x, y) => (x, y))];

            var sumx = values.Sum(v => v.x);
            var sumy = values.Sum(v => v.y);
            var sumxsq = values.Sum(v => v.x * v.x);
            var sumysq = values.Sum(v => v.y * v.y);
            var sumcodev = values.Sum(v => v.x * v.y);

            double count = values.Length;

            var ssx = sumxsq - sumx * sumx / count;
            var ssy = sumysq - sumy * sumy / count;
            var sco = sumcodev - sumx * sumy / count;

            var yintercept = sumy / count - sco / ssx * sumx / count;

            return yintercept;
        }
    }
}
