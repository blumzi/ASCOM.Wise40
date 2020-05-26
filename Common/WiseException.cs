using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ASCOM.Wise40.Common
{
    [Serializable]
    public class WiseException : Exception
    {
        public WiseException(string message) : base(message)
        {
            Exceptor.Throw<DriverException>("WiseException", message);
        }

        public WiseException() : base()
        {
        }

        public WiseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected WiseException(SerializationInfo serializationInfo, StreamingContext streamingContext)
        {
            Exceptor.Throw<System.NotImplementedException>("WiseException", "Not implemented");
        }
    }
}
