using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Core.Http
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RpcCall : Attribute
    {
        /// <summary>
        /// The public name of function
        /// </summary>
        public readonly string FunctionName;

        /// <summary>
        /// If authentication is required to invoke the method.
        /// </summary>
        public bool AuthenticationRequired { get; set; }

        /// <summary>
        /// Constructor, does nothing - use named parameters
        /// </summary>
        public RpcCall(string functionName)
        {
            FunctionName = functionName;
            AuthenticationRequired = false;
        }
    }
}
