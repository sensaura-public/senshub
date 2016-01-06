using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Core.Http
{
    public class RpcException : Exception
    {
        /// <summary>
        /// Constructor with a message
        /// </summary>
        /// <param name="message"></param>
        public RpcException(string message) : base(message) { }
    }
}
