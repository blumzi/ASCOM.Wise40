using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Wise40.Log;

namespace LogTest
{
    class LogTest
    {
        static void Main(string[] args)
        {
            WiseLogger logger = new WiseLogger("test");
            logger.Log("hello {0} {1}", "world", "HIHO");
        }
    }
}
