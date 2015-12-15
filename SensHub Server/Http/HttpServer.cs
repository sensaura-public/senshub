using System;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using SensHub.Plugins;
using SensHub.Server;
using SensHub.Server.Managers;
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

		// The port to listen on
		private int m_port;

        // The actual listener
        private HttpListener m_listener;

        // URL handler instances
        private Dictionary<string, HttpRequestHandler> m_handlers;

        // Active sessions
        private Dictionary<Guid, HttpSession> m_sessions;

        // RPC call manager
        private RpcRequestHandler m_rpcHandler;

        public HttpServer(string sitePath, int port)
        {
			m_port = port;
            m_sitePath = sitePath;
            m_sessions = new Dictionary<Guid, HttpSession>();
            m_handlers = new Dictionary<string, HttpRequestHandler>();
            AddHandler("/", new StaticFileHandler(m_sitePath));
            m_rpcHandler = new RpcRequestHandler();
            AddHandler("/api/", m_rpcHandler);
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

        /// <summary>
        /// Unpack images provided by the various plugins to the site directory
        /// </summary>
        public void UnpackImages()
        {
            // Make sure we have an empty directory to start with
            string imagePath = Path.Combine(m_sitePath, "img/plugins");
            if (Directory.Exists(imagePath))
                Directory.Delete(imagePath, true);
            Directory.CreateDirectory(imagePath);
            // Walk through the assemblies unpacking the Images
            MasterObjectTable mot = Locator.Current.GetService<MasterObjectTable>();
            foreach (Assembly assembly in mot.Assemblies)
            {
                string prefix = assembly.GetName().Name + ".Resources.Images.";
                foreach (var resourceName in assembly.GetManifestResourceNames())
                {
                    if (!resourceName.StartsWith(prefix))
                        continue;
                    // TODO: Should limit this to image files only
                    // Copy the resource in
                    using (Stream source = assembly.GetManifestResourceStream(resourceName))
                    {
                        Stream target = File.Create(Path.Combine(imagePath, assembly.GetName().Name + "." + resourceName.Substring(prefix.Length)));
                        source.CopyTo(target);
                        target.Close();
                    }
                }
            }
        }

        private void Return404(HttpListenerContext context, string status = "Not Found")
        {
            context.Response.StatusCode = 404;
            context.Response.StatusDescription = status;
            context.Response.KeepAlive = false;
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            // Find a matching handler
            HttpRequestHandler handler = null;
            string fullURI = context.Request.Url.AbsolutePath;
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
            string childURI = fullURI.Substring(matchLength);
            // Invoke the handler if we have one
            if (handler == null)
            {
                Return404(context);
                return;
            }
            // Is this a websocket request ?
            if (context.Request.IsWebSocketRequest)
            {
                this.Log().Debug("Websocket request on URI {0}", fullURI);
                WebSocketRequestHandler wsHandler = handler as WebSocketRequestHandler;
                if ((wsHandler == null) || (!wsHandler.WillAcceptWebSocket(fullURI.Substring(matchLength))))
                {
                    Return404(context);
                    return;
                }
                Task.Factory.StartNew(async () =>
                {
                    HttpListenerWebSocketContext wsContext = null;
                    try
                    {
                        wsContext = await context.AcceptWebSocketAsync(wsHandler.Protocol);
                        wsHandler.AttachWebSocket(fullURI.Substring(matchLength), wsContext.WebSocket);
                    }
                    catch (Exception ex)
                    {
                        this.Log().Warn("Failed to accept WebSocket connection - {0}", ex.Message);
                        if (wsContext != null)
                            wsContext.WebSocket.Abort();
                    }
                });
                return;
            }
            // Normal request
            try
            {
                // Set default content type
                context.Response.ContentType = "text/plain";
                // Get the handler to service the request
                string response = handler.HandleRequest(fullURI.Substring(matchLength), context.Request, context.Response);
                if (response != null)
                {
                    byte[] buf = Encoding.UTF8.GetBytes(response);
                    context.Response.ContentLength64 = buf.Length;
                    context.Response.OutputStream.Write(buf, 0, buf.Length);
                }
            }
            catch (Exception ex)
            {
                this.Log().Error("Failed to process request - {0}", ex.ToString());
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = ex.ToString();
                context.Response.KeepAlive = false;
            }
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
			// Get the configured port
			string prefix = string.Format("http://*:{0}/", m_port);
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
                                ProcessRequest(ctx);
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
