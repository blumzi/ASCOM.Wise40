using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AzimuthTest;

namespace ConsoleTestAzimuth
{
    class ConsoleTestAzimuth
    {
        static void Main(string[] args)
        {
            AzimuthTest.AzimuthTest test = new AzimuthTest.AzimuthTest();
            test.TestNormalized();
        }
    }
}
