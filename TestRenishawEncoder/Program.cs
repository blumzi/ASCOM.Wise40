using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ASCOM.Wise40.Hardware;

namespace TestRenishawEncoder
{
    public static class Program
    {
        public static void Main()
        {
            RenishawEncoder DecEncoder = null, HaEncoder = null;

            try
            {
                HaEncoder = new RenishawEncoder(RenishawEncoder.Module.Ha);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create RenishawEncoder({RenishawEncoder.Module.Ha}): caught {ex.Message}");
                Environment.Exit(1);
            }


            try
            {
                DecEncoder = new RenishawEncoder(RenishawEncoder.Module.Dec);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create RenishawEncoder({RenishawEncoder.Module.Dec}): caught {ex.Message}");
                Environment.Exit(1);
            }

            for (;;)
            {
                try
                {
                    Console.WriteLine($"Ha: {HaEncoder.Position}, Dec: {DecEncoder.Position}");
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
