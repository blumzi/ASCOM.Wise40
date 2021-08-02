using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASCOM.Wise40.Common
{
    public class Exceptor
    {
        private static readonly Debugger debugger = Debugger.Instance;
        private static Debugger.DebugLevel debugLevel;

        public Exceptor(Debugger.DebugLevel d)
        {
            debugLevel = d;
        }

        public void Throw<TException>(string op, string message, bool accessorIsSet = false) where TException : Exception, new()
        {
            if (typeof(Exception).IsAssignableFrom(typeof(TException)))
            {
                #region debug
                debugger.WriteLine(debugLevel, $"Throw: operation: {op}, cancelled with {typeof(TException)}({message})");
                #endregion
                if (typeof(TException) == typeof(PropertyNotImplementedException))
                    throw new PropertyNotImplementedException(message, accessorIsSet);
                else
                    throw Activator.CreateInstance(typeof(TException), message) as TException;
            }
            else
            {
                #region debug
                debugger.WriteLine(debugLevel,
                    $"Throw: Operation {op}, invalid exception type {typeof(TException)} for reason: {message}");
                #endregion
            }
        }
    }
}
