using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using Splat;

namespace SensHub.Server
{
	/// <summary>
	/// Provides static helper methods to pack/unpack objects in JSON
	/// format.
	/// </summary>
	internal class ObjectPacker
	{
		/// <summary>
		/// Uses DynamicObject to create a generic packable object from data.
		/// </summary>
		private class GenericPackedObject : DynamicObject, IPackable
		{
			// Values of the object
			private Dictionary<string, object> m_values;

			/// <summary>
			/// Constructor with packed values
			/// </summary>
			/// <param name="packed"></param>
			public GenericPackedObject(IReadOnlyDictionary<string, object> packed)
			{
				m_values = new Dictionary<string, object>();
				foreach(KeyValuePair<string, object> value in packed)
					m_values.Add(value.Key, value.Value);
			}

			/// <summary>
			/// Get the value of a property that is not explicitly defined.
			/// </summary>
			/// <param name="binder"></param>
			/// <param name="result"></param>
			/// <returns></returns>
			public override bool TryGetMember(GetMemberBinder binder, out object result)
			{
				// Converting the property name to lowercase
				// so that property names become case-insensitive.
				string name = binder.Name.ToLower();

				// If the property name is found in a dictionary,
				// set the result parameter to the property value and return true.
				// Otherwise, return false.
				return m_values.TryGetValue(name, out result);
			}

			/// <summary>
			/// Setting a name that is not defined by the class.
			/// </summary>
			/// <param name="binder"></param>
			/// <param name="value"></param>
			/// <returns></returns>
			public override bool TrySetMember(SetMemberBinder binder, object value)
			{
				string key = binder.Name.ToLower();
				if (!m_values.ContainsKey(key))
					return false;
				// Save it in the dictionary
				m_values[key] = value;
				return true;
			}

			public IReadOnlyDictionary<string, object> Pack()
			{
				return (IReadOnlyDictionary<string, object>)m_values;
			}
		}

		/// <summary>
		/// Unpacker for generic objects
		/// </summary>
		private class GenericPackedObjectPacker : IUnpacker<GenericPackedObject>
		{
			/// <summary>
			/// Unpack a generic object from a dictionary.
			/// </summary>
			/// <param name="packed"></param>
			/// <returns></returns>
			public GenericPackedObject Unpack(IReadOnlyDictionary<string, object> packed)
			{
				return new GenericPackedObject(packed);
			}
		}

		/// <summary>
		/// Static initialisation
		/// </summary>
		static ObjectPacker()
		{
			Locator.CurrentMutable.RegisterConstant(new GenericPackedObjectPacker(), typeof(IUnpacker<GenericPackedObject>));
		}

		/// <summary>
		/// Convert a packable object to a JSON string.
		/// </summary>
		/// <param name="packable">The object to pack</param>
		/// <returns></returns>
		public static string Pack(IPackable packable, bool storeTypeInformation = false)
		{
			return null;
		}

		/// <summary>
		/// Unpack an object from JSON format.
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static IPackable Unpack(string json)
		{
			return null;
		}

		/// <summary>
		/// Unpack a specific tyep of object from JSON
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json"></param>
		/// <returns></returns>
		public static T Unpack<T>(string json) where T : IPackable
		{
			return default(T);
		}
	}
}
