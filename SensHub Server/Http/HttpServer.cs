using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using SensHub.Plugins;
using SensHub.Server;
using Splat;

namespace SensHub.Server.Http
{
    /// <summary>
    /// Implements a simple single threaded HTTP server to provide the UI.
    /// </summary>
    public class HttpServer : IEnableLogger
    {
        // The directory containing the site
        private string m_sitePath;

        // The actual listener
        private HttpListener m_listener;

        // URL handler instances
        private Dictionary<string, HttpRequestHandler> m_handlers;

        public HttpServer(string sitePath)
        {
            m_sitePath = sitePath;
            m_handlers = new Dictionary<string, HttpRequestHandler>();
            AddHandler("/", new StaticFileHandler(m_sitePath));
        }

        /// <summary>
        /// Attach a request handler to a given prefix.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="handler"></param>
        public void AddHandler(string prefix, HttpRequestHandler handler)
        {
            m_handlers.Add(prefix, handler);
        }

        /// <summary>
        /// Convert a resource name (without the prefix) into a target
        /// path and file. Assumes that all files have a single suffix
        /// (eg .html, .css, etc) and all other dots in the name are
        /// replaced with a directory separator.
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        private string GetTargetFileName(string resourceName)
        {
            int lastDot = resourceName.LastIndexOf('.');
            return resourceName.Substring(0, lastDot).Replace('.', '/') + resourceName.Substring(lastDot);
        }

        /// <summary>
        /// Unpack the site into a static directory.
        /// 
        /// In a 'production' environment the server is fronted by an Nginx
        /// instance running as a caching proxy. To facilitate this we unpack
        /// the site from resources to a directory so it can access them.
        /// </summary>
        /// <param name="siteDir"></param>
        public void UnpackSite()
        {
            // Make sure we have an empty directory to start with
            Directory.Delete(m_sitePath, true);
            Directory.CreateDirectory(m_sitePath);
            // Walk through all the resources
            var assembly = Assembly.GetExecutingAssembly();
            string prefix = assembly.GetName().Name + ".Resources.Site.";
            foreach (var resourceName in assembly.GetManifestResourceNames())
            {
                if (!resourceName.StartsWith(prefix))
                    continue;
                string fileName = GetTargetFileName(resourceName.Substring(prefix.Length));
                // Make sure the directory exists
                string directory = Path.Combine(m_sitePath, Path.GetDirectoryName(fileName));
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                // Copy the resource in
                using (Stream source = assembly.GetManifestResourceStream(resourceName))
                {
                    Stream target = File.Create(Path.Combine(m_sitePath, fileName));
                    source.CopyTo(target);
                    target.Close();
                }
            }
        }

        public string ProcessRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            // Find a matching handler
            HttpRequestHandler handler = null;
            string fullURI = request.Url.AbsolutePath;
            int matchLength = 0;
            foreach (string candidate in m_handlers.Keys)
            {
                int size = candidate.Length;
                if (fullURI.StartsWith(candidate) && (size > matchLength))
                {
                    handler = m_handlers[candidate];
                    matchLength = size;
                }
            }
            // Invoke the handler if we have one
            string result = null;
            if (handler == null)
            {
                response.StatusCode = 404;
                response.StatusDescription = "Not found.";
                response.KeepAlive = false;
            }
            else
            {
                try
                {
                    result = handler.HandleRequest(fullURI.Substring(matchLength), request, response);
                }
                catch (Exception ex)
                {
                    this.Log().Error("Failed to process request - {0}", ex.ToString());
                    response.StatusCode = 500;
                    response.StatusDescription = ex.ToString();
                    response.KeepAlive = false;
                }
            }
            return result;
        }

        /// <summary>
        /// Start the HTTP server.
        /// 
        /// This method returns immediately, leaving the requests to be
        /// processed on the system threadpool.
        /// </summary>
        public void Start()
        {
            // Set up the listener
            m_listener = new HttpListener();
            Configuration serverConfig = Locator.Current.GetService<Configuration>();
            string prefix = "http://*:" + serverConfig["httpPort"].ToString() + "/";
            this.Log().Debug("Server listening on {0}", prefix);
            m_listener.Prefixes.Add(prefix);
            m_listener.Start();
            // Process incoming requests on a thread pool
            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    while (m_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                this.Log().Debug("{0}", ctx.Request.Url);
                                // Set default content type
                                ctx.Response.ContentType = "text/plain";
                                // Process the request
                                string response = ProcessRequest(ctx.Request, ctx.Response);
                                if (response != null)
                                {
                                    byte[] buf = Encoding.UTF8.GetBytes(response);
                                    ctx.Response.ContentLength64 = buf.Length;
                                    ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                                }
                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, m_listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        /// <summary>
        /// Stop the server.
        /// </summary>
        public void Stop()
        {
            if (m_listener != null)
                m_listener.Stop();
        }
    }
}
