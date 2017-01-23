using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            string filename = args[0];
            if (!File.Exists(filename))
            {
                Console.WriteLine($"File not found: '{filename}'");
                return 1;
            }

            double limit;
            if (!double.TryParse(args[1], out limit))
            {
                Console.WriteLine("Couldn't parse limit: '" + args[1] + "'");
                return 1;
            }

            string[] rows = File.ReadAllLines(filename);

            var values = rows
                .Skip(1)
                .Where(r => r.Contains('\t'))
                .Select(r => new { date = DateTime.Parse(r.Split('\t')[0]), value = limit - double.Parse(r.Split('\t')[1]) });

            double y = GetLinearRegressionYIntercept(
                values.Select(v => v.value).ToArray(),
                values.Select(v => (double)(v.date.Ticks)).ToArray());

            DateTime d = new DateTime((long)y);
            Console.WriteLine("Y intercept: " + d);

            return 0;
        }

        static double GetLinearRegressionYIntercept(double[] xvalues, double[] yvalues)
        {
            var values = xvalues.Zip(yvalues, (x, y) => new { x = x, y = y }).ToArray();

            double sumx = values.Sum(v => v.x);
            double sumy = values.Sum(v => v.y);
            double sumxsq = values.Sum(v => v.x * v.x);
            double sumysq = values.Sum(v => v.y * v.y);
            double sumcodev = values.Sum(v => v.x * v.y);

            double count = values.Length;

            double ssx = sumxsq - sumx * sumx / count;
            double ssy = sumysq - sumy * sumy / count;
            double sco = sumcodev - sumx * sumy / count;

            double yintercept = sumy / count - sco / ssx * sumx / count;

            return yintercept;
        }
    }
}
