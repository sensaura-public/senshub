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
		// Name of the cookie to use for sessions
		public const string SessionCookie = "SensHubSessionID";

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

		/// <summary>
		/// Get (or create) the session associated with the request.
		/// 
		/// Session information is maintained with cookies, this method will update the
		/// response with the correct cookie to set the session ID as well.
		/// </summary>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <returns></returns>
		public HttpSession GetSession(HttpListenerRequest request, HttpListenerResponse response)
		{
			HttpSession session = null;
			Cookie cookie = request.Cookies[SessionCookie];
            if (cookie != null)
				session = HttpSession.GetSession(cookie.Value);
			if (session == null)
				session = HttpSession.CreateSession();
			// Make sure we send it back
			cookie = new Cookie(SessionCookie, session.ID);
			cookie.Expires = DateTime.UtcNow.AddMinutes(HttpSession.SessionLifetime * 2);
			response.Cookies.Add(cookie);
			return session;
		}

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
