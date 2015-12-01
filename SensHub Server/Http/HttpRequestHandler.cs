using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Server.Http
{
    public abstract class HttpRequestHandler
    {
        /// <summary>
        /// Handle an incoming request.
        /// 
        /// This method is called when an incoming request matches the
        /// prefix this HttpRequestHandler is bound to.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns>
        /// Return a string to use as the reponse or null if the response
        /// object has been filled out already.
        /// </returns>
        public abstract string HandleRequest(string url, HttpListenerRequest request, HttpListenerResponse response);
    }
}
