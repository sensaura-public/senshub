using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IotWeb.Common.Http;
using SensHub.Core.Plugins;
using Splat;

namespace SensHub.Core.Http
{
	class RpcRequestHandler : IHttpRequestHandler, IWebSocketRequestHandler, IEnableLogger
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
							m_defaultArgs[param.Name] = param.DefaultValue;
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
				m_methods.Add(callInfo.FunctionName, new RpcCallInfo()
				{
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
//					Thread.SetData(Thread.GetNamedDataSlot("Session"), session);
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

		#region Handler methods for HTTP and WebSockets
		/// <summary>
		/// Handle a HTTP request containing a RPC call
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="request"></param>
		/// <param name="response"></param>
		/// <param name="context"></param>
		public void HandleRequest(string uri, HttpRequest request, HttpResponse response, HttpContext context)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Determine if we will accept a websocket protocol.
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public bool WillAcceptRequest(string uri, string protocol)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Called when a websocket connection is accepted.
		/// </summary>
		/// <param name="socket"></param>
		public void Connected(WebSocket socket)
		{
			throw new NotImplementedException();
		}
		#endregion

	}
}
