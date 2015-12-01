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
        /// <param name="session"></param>
        /// <param name="url"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns>
        /// Return a string to use as the reponse or null if the response
        /// object has been filled out already.
        /// </returns>
        public abstract string HandleRequest(HttpSession session, string url, HttpListenerRequest request, HttpListenerResponse response);

        /// <summary>
        /// Helper method to generate HTTP 404 Not Found responses
        /// </summary>
        /// <param name="response"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public string NotFound(HttpListenerResponse response, string message = null)
        {
            response.StatusCode = 404;
            response.StatusDescription = "Item not found.";
            return message;
        }

        /// <summary>
        /// Helper method to generate HTTP 500 Server Error responses
        /// </summary>
        /// <param name="response"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public string ServerError(HttpListenerResponse response, string message = null)
        {
            response.StatusCode = 500;
            response.StatusDescription = "Internal server error.";
            return message;
        }

        /// <summary>
        /// Helper method to generate HTTP 400 Bad Request responses
        /// </summary>
        /// <param name="response"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public string BadRequest(HttpListenerResponse response, string message = null)
        {
            response.StatusCode = 400;
            response.StatusDescription = "Bad request.";
            return message;
        }

        /// <summary>
        /// Helper method to generate HTTP 405 Method Not Supported responses
        /// </summary>
        /// <param name="response"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public string MethodNotSupported(HttpListenerResponse response, string message = null)
        {
            response.StatusCode = 405;
            response.StatusDescription = "Method not supported.";
            return message;
        }

        /// <summary>
        /// Helper method to generate HTTP 403 Permission Denied responses
        /// </summary>
        /// <param name="response"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public string PermissionDenied(HttpListenerResponse response, string message = null)
        {
            response.StatusCode = 403;
            response.StatusDescription = "Permission Denied.";
            return message;
        }

    }
}
