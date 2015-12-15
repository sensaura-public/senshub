using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SensHub.Plugins;
using SensHub.Server.Managers;
using Splat;

namespace SensHub.Server.Http
{
    /// <summary>
    /// Implementation of HttpRequestHandler to process RPC calls
    /// </summary>
    internal class RpcRequestHandler : WebSocketRequestHandler, IEnableLogger
    {
        private class RpcCallInfo
        {
            // How to build a parameter list
            private List<string> m_parameterOrder;
            private Dictionary<string, object> m_defaultArgs;

            /// <summary>
            /// The object instance to invoke the method on
            /// </summary>
            public object Instance { get; set; }

            /// <summary>
            /// The method descriptor
            /// </summary>
            public MethodInfo Method { get; set; }

            /// <summary>
            /// Is authentication required to invoke the method?
            /// </summary>
            public bool AuthenticationRequired { get; set; }

            /// <summary>
            /// Invoke the method with the given parameters
            /// </summary>
            /// <param name="parameters"></param>
            /// <returns>The result of the method call.</returns>
            public object InvokeMethod(IDictionary<string, object> parameters)
            {
                if (m_parameterOrder == null)
                {
                    // Determine the parameter order as well as any default values
                    m_parameterOrder = new List<string>();
                    m_defaultArgs = new Dictionary<string, object>();
                    foreach (ParameterInfo param in Method.GetParameters())
                    {
                        m_parameterOrder.Add(param.Name);
                        if (param.HasDefaultValue)
                            m_defaultArgs[param.Name] = param.RawDefaultValue;
                    }
                }
                // Do we have too many parameters ?
                if ((parameters != null) && (parameters.Count > m_parameterOrder.Count))
                    throw new RpcException("Too many parameters in RPC call.");
                // Map named parameters to an array for invokation
                object[] callParams = new object[m_parameterOrder.Count];
                int index = 0;
                foreach (string paramName in m_parameterOrder)
                {
                    if ((parameters != null) && (parameters.ContainsKey(paramName)))
                        callParams[index] = parameters[paramName];
                    else if (m_defaultArgs.ContainsKey(paramName))
                        callParams[index] = m_defaultArgs[paramName];
                    else // Invalid or missing parameters
                        throw new RpcException("One or more required parameters is missing.");
                    index++;
                }
                // Finally, call the method (the caller should handle exceptions)
                return Method.Invoke(Instance, callParams);
            }

        }

        // Maximum size of incoming websocket packets
        private const int MaxPacketSize = 4096;

        // Cache our method information
        private Dictionary<string, RpcCallInfo> m_methods;

        /// <summary>
        /// Default constructor
        /// </summary>
        public RpcRequestHandler()
        {
            m_methods = new Dictionary<string, RpcCallInfo>();
            // Add our own methods first
            RegisterMethods(this);
			// Add methods from the MasterObjectTable
			RegisterMethods(Locator.Current.GetService<MasterObjectTable>());
        }

        /// <summary>
        /// Test if a method description is marked as an RpcCall
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        private RpcCall IsMethodRpcCall(MethodInfo method)
        {
            foreach (object attribute in method.GetCustomAttributes(true))
            {
                if (attribute is RpcCall)
                {
                    return (RpcCall)attribute;
                }
            }
            return null;
        }

        /// <summary>
        /// Register all methods on the given class.
        /// </summary>
        /// <param name="implementation"></param>
        public void RegisterMethods(object implementation)
        {
            // Find all methods on the instance decorated with the RpcCall attribute
            foreach (MethodInfo method in implementation.GetType().GetRuntimeMethods())
            {
                RpcCall callInfo = IsMethodRpcCall(method);
                if (callInfo == null)
                    continue;
                // Add it to the cache
                if (m_methods.ContainsKey(callInfo.FunctionName))
                {
                    this.Log().Warn("An RPC method called '{0}' is already registered.", callInfo.FunctionName);
                    continue;
                }
                this.Log().Debug("Adding RPC method '{0}' bound to {1}.{2}::{3}",
                    callInfo.FunctionName,
                    implementation.GetType().Namespace,
                    implementation.GetType().Name,
                    method.Name
                    );
                m_methods.Add(callInfo.FunctionName, new RpcCallInfo() {
                    Instance = implementation,
                    Method = method,
                    AuthenticationRequired = callInfo.AuthenticationRequired
                    });
            }
        }

		/// <summary>
		/// Dispatch a call given the JSON description of the call.
		/// </summary>
		/// <param name="rpcCall"></param>
		/// <returns></returns>
		private IDictionary<string, object> DispatchCall(HttpSession session, IDictionary<string, object> rpcCall)
		{
			// Set up the response
			Dictionary<string, object> callResult = new Dictionary<string, object>();
			callResult["success"] = true;
			callResult["type"] = "response";
			if (rpcCall.ContainsKey("sequence"))
				callResult["sequence"] = rpcCall["sequence"];
			// We require the 'method' parameter in the request
			if (!rpcCall.ContainsKey("method"))
			{
				callResult["success"] = false;
				callResult["result"] = "Method name not specified in call.";
				return callResult;
			}
			string methodName = rpcCall["method"].ToString();
			// Arguments are optional but must be a dictionary if provided
			IDictionary<string, object> parameters = null;
			if (rpcCall.ContainsKey("arguments"))
			{
				parameters = rpcCall["arguments"] as IDictionary<string, object>;
				if (parameters == null)
				{
					callResult["success"] = false;
					callResult["result"] = "Method parameters must be provided as a dictionary.";
					return callResult;
				}
			}
			// Make sure we support the call
			RpcCallInfo callInfo;
			if (!m_methods.TryGetValue(methodName, out callInfo))
				callInfo = null;
			if (callInfo == null)
			{
				callResult["success"] = false;
				callResult["result"] = "Method not implemented.";
			}
			else if (callInfo.AuthenticationRequired && (!session.Authenticated))
			{
				callResult["success"] = false;
				callResult["result"] = "Authentication required.";
			}
			else
			{
				// Now try the call
				try
				{
					Thread.SetData(Thread.GetNamedDataSlot("Session"), session);
					callResult["result"] = callInfo.InvokeMethod(parameters);
				}
				catch (Exception ex)
				{
					callResult["success"] = false;
					callResult["result"] = ex.Message;
				}
			}
			return callResult;
		}

		private void DispatchMessage(HttpSession session, IDictionary<string, object> callInfo)
		{

		}

        /// <summary>
        /// Handle the request
        /// </summary>
        /// <param name="session"></param>
        /// <param name="url"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public override string HandleRequest(string url, HttpListenerRequest request, HttpListenerResponse response)
        {
			// Make sure it is a POST request and contains a JSON payload
			if (request.HttpMethod != "POST")
				return MethodNotSupported(response);
			if ((!request.HasEntityBody) || (request.ContentType != "application/json"))
				return BadRequest(response);
			// Get the session associated with the request
			HttpSession session = GetSession(request, response);
            // Try and interpret the data as JSON
			Dictionary<string, object> results = new Dictionary<string, object>();
			IDictionary<string, object> item = ObjectPacker.UnpackRaw(request.InputStream);
			if (item.ContainsKey("type"))
			{
				string type = item["type"].ToString();
				if (type == "request")
				{
					results["response"] = DispatchCall(session, item);
				}
				else if (type == "message")
				{
					DispatchMessage(session, item);
				}
				else
				{
					this.Log().Info("Unsupported type in RPC stream - {0}", type);
				}
			}
			// Add any pending messages for the session
			results["messages"] = session.Messages;
			// Finally we can send back the response
			return JsonParser.ToJson(results);
        }

        #region Core RPC
        /// <summary>
        /// Authenticate the user and mark the session as valid.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        [RpcCall("Authenticate", AuthenticationRequired = false)]
        public bool Authenticate(string password)
        {
			HttpSession session = Thread.GetData(Thread.GetNamedDataSlot("Session")) as HttpSession;
			// TODO: Reimplement this correctly
            //Configuration serverConfig = Locator.Current.GetService<Configuration>();
            //string systemPassword = serverConfig["password"].ToString();
            //if (password == systemPassword)
            //{
            //    session.Authenticated = true;
            //    return true;
            //}
			session.Authenticated = true;
            return true;
        }

        /// <summary>
        /// Poll for any pending messages.
        /// </summary>
        /// <returns></returns>
        [RpcCall("Poll", AuthenticationRequired = false)]
        public bool Poll()
        {
            return true;
        }

		[RpcCall("Subscribe", AuthenticationRequired = true)]
		public bool Subscribe(string topic)
		{
			HttpSession session = Thread.GetData(Thread.GetNamedDataSlot("Session")) as HttpSession;
			IMessageBus messageBus = Locator.Current.GetService<IMessageBus>();
			ITopic t = messageBus.Create(topic);
			messageBus.Subscribe(t, session);
			return true;
		}

		[RpcCall("Unsubscribe", AuthenticationRequired = true)]
		public bool Unsubscribe(string topic)
		{
			HttpSession session = Thread.GetData(Thread.GetNamedDataSlot("Session")) as HttpSession;
			IMessageBus messageBus = Locator.Current.GetService<IMessageBus>();
			ITopic t = messageBus.Create(topic);
			messageBus.Unsubscribe(t, session);
			return true;
		}

		[RpcCall("Publish", AuthenticationRequired = true)]
		public bool Publish(string topic, IDictionary<string, object> message)
		{
			HttpSession session = Thread.GetData(Thread.GetNamedDataSlot("Session")) as HttpSession;
			IMessageBus messageBus = Locator.Current.GetService<IMessageBus>();
			ITopic t = messageBus.Create(topic);
			MessageBuilder builder = new MessageBuilder();
			foreach (string key in message.Keys)
				builder.Add(key, message[key]);
			messageBus.Publish(t, builder.CreateMessage(), session);
			return true;
		}
        #endregion

		#region WebSocket Implementation
		public override string Protocol
		{
			get
			{
				return "senshub";
			}
		}

		public override bool WillAcceptWebSocket(string uri)
		{
			// No child URLs allowed
			return uri.Length == 0;
		}

		public override async void AttachWebSocket(string uri, WebSocket socket)
		{
            // Create a session specificly for this WebSocket
            HttpSession session = HttpSession.CreateSession();
            session.Socket = socket;
            byte[] receiveBuffer = new byte[MaxPacketSize];
            while (socket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = null;
                try
                {
                    result = await socket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                }
                catch (Exception ex)
                {
                    this.Log().Debug("Error receiving data from websocket - {0}", ex.Message);
                    continue;
                }
                // Trigger activity on the session so it doesn't expire
                HttpSession.GetSession(session.ID);
                // Process the message
                if ((result.MessageType == WebSocketMessageType.Close) || (session == null))
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    continue;
                }
                // Assume the message is JSON and convert it
                string json = null;
                try
                {
                    json = Encoding.UTF8.GetString(receiveBuffer, 0, result.Count);
                }
                catch (Exception ex)
                {
                    this.Log().Debug("Unable to convert incoming message to text - {0}", ex.Message);
                    continue;
                }
                // Unpack the JSON into a message
                IDictionary<string, object> item = null;
                try
                {
                    item = ObjectPacker.UnpackRaw(json);
                }
                catch (Exception ex)
                {
                    this.Log().Debug("Could not unpack JSON into a message or RPC call - {0}", ex.Message);
                    continue;
                }
                if (item.ContainsKey("type"))
                {
                    string type = item["type"].ToString();
                    if (type == "request")
                    {
                        try
                        {
                            IDictionary<string, object> callResult = DispatchCall(session, item);
                            await socket.SendAsync(
                                new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonParser.ToJson(callResult))),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None
                                );
                        }
                        catch (Exception ex)
                        {
                            this.Log().Debug("Unable to dispatch response to RPC method - {0}", ex.Message);
                            continue;
                        }
                    }
                    else if (type == "message")
                    {
                        DispatchMessage(session, item);
                    }
                    else
                    {
                        this.Log().Info("Unsupported type in RPC stream - {0}", type);
                    }
                }
            }
        }
        #endregion
    }
}
