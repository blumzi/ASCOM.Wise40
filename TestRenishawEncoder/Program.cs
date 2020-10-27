using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRenishawEncoder
{
    public static class Program
    {
        public static void Main(string[] argv)
        {
            Hardware.RenishawEncoder encoder = null;
            int encNumber = 1;

            if (argv.Length != 0)
                encNumber = Convert.ToInt32(argv[0]);

            try
            {
                encoder = new Hardware.RenishawEncoder(encNumber);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create RenishawEncoder({encNumber}): caught {ex.Message}");
                Environment.Exit(1);
            }

            for (; ; )
            {
                try
                {
                    Console.WriteLine($"{encoder.Position}");
                    System.Threading.Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to get encoder position: Exception: {ex.Message}");
                    Environment.Exit(2);
                }
            }
        }
    }
}
