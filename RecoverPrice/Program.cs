using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RecoverPrice
{
    class Program
    {
        static Dictionary<int, int> priceDict = new Dictionary<int, int>();
        static void Main(string[] args)
        {
            priceDict.Add(0, 1);
            priceDict.Add(5, 2);
            priceDict.Add(2, 3);
            priceDict.Add(3, 4);
            priceDict.Add(6, 5);
            priceDict.Add(4, 6);
            priceDict.Add(9, 7);
            priceDict.Add(8, 8);
            priceDict.Add(7, 9);
            priceDict.Add(1, 0);

            

            StreamReader sr = new StreamReader("data.txt");
            while (sr.EndOfStream)
            {
                var line = sr.ReadLine();
                var match = Regex.Match(line, @"￥ (\d+)");
                if (match != null && match.Length > 0)
                {
                    if (match.Groups.Count > 1)
                    {
                        var price = Convert.ToInt32(match.Groups[1].Value);

                    }
                }
            }
        }
    }
}
