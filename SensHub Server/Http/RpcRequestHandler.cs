using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using Splat;

namespace SensHub.Server.Http
{
    /// <summary>
    /// Implementation of HttpRequestHandler to process RPC calls
    /// </summary>
    internal class RpcRequestHandler : HttpRequestHandler, IEnableLogger
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

        // Cache our method information
        private Dictionary<string, RpcCallInfo> m_methods;

        /// <summary>
        /// Make the session available for local methods.
        /// </summary>
        protected HttpSession Session { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public RpcRequestHandler()
        {
            m_methods = new Dictionary<string, RpcCallInfo>();
            // Add our own methods first
            RegisterMethods(this);
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
        /// Handle the request
        /// </summary>
        /// <param name="session"></param>
        /// <param name="url"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public override string HandleRequest(HttpSession session, string url, HttpListenerRequest request, HttpListenerResponse response)
        {
            // Save the session for our methods to use
            Session = session;
            // Make sure it is a POST request and contains a JSON payload
            if (request.HttpMethod != "POST")
                return MethodNotSupported(response);
            if ((!request.HasEntityBody) || (request.ContentType != "application/json"))
                return BadRequest(response);
            // Try and interpret the data as JSON
            Dictionary<string, object> rpcCall = ObjectPacker.UnpackRaw(request.InputStream);
            // We require the 'methodName' parameter in the request
            if (!rpcCall.ContainsKey("methodName"))
                return BadRequest(response);
            string methodName = rpcCall["methodName"].ToString();
            // Arguments are optional but must be a dictionary if provided
            IDictionary<string, object> parameters = null;
            if (rpcCall.ContainsKey("parameters"))
            {
                parameters = rpcCall["parameters"] as IDictionary<string, object>;
                if (parameters == null)
                    return BadRequest(response);
            }
            // Set up the response
            Dictionary<string, object> callResult = new Dictionary<string, object>();
            callResult["failed"] = false;
            // Make sure we support the call
            RpcCallInfo callInfo;
            if (!m_methods.TryGetValue(methodName, out callInfo))
                callInfo = null;
            if (callInfo == null)
            {
                callResult["failed"] = true;
                callResult["failureMessage"] = "Method not implemented.";
            }
            else if (callInfo.AuthenticationRequired && (!Session.Authenticated))
            {
                callResult["failed"] = true;
                callResult["failureMessage"] = "Authentication required.";
            }
            else
            {
                // Now try the call
                try
                {
                    callResult["result"] = callInfo.InvokeMethod(parameters);
                }
                catch (Exception ex)
                {
                    callResult["failed"] = true;
                    if ((ex.Message == null) || (ex.Message.Length == 0))
                        callResult["failureMessage"] = ex.ToString();
                    else
                        callResult["failureMessage"] = ex.Message;
                }
            }
            // Add any pending messages for the session
			callResult["messages"] = session.Messages;
			// Finally we can send back the response
            return JsonParser.ToJson(callResult);
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
            Configuration serverConfig = Locator.Current.GetService<Configuration>();
            string systemPassword = serverConfig["password"].ToString();
            if (password == systemPassword)
            {
                Session.Authenticated = true;
                return true;
            }
            return false;
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
			IMessageBus messageBus = Locator.Current.GetService<IMessageBus>();
			ITopic t = messageBus.Create(topic);
			messageBus.Subscribe(t, Session);
			return true;
		}

		[RpcCall("Unsubscribe", AuthenticationRequired = true)]
		public bool Unsubscribe(string topic)
		{
			IMessageBus messageBus = Locator.Current.GetService<IMessageBus>();
			ITopic t = messageBus.Create(topic);
			messageBus.Unsubscribe(t, Session);
			return true;
		}

		[RpcCall("Publish", AuthenticationRequired = true)]
		public bool Publish(string topic, IDictionary<string, object> message)
		{
			IMessageBus messageBus = Locator.Current.GetService<IMessageBus>();
			ITopic t = messageBus.Create(topic);
			MessageBuilder builder = new MessageBuilder();
			foreach (string key in message.Keys)
				builder.Add(key, message[key]);
			messageBus.Publish(t, builder.CreateMessage(), Session);
			return true;
		}
        #endregion
    }
}
