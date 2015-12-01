using System;
using System.IO;
using System.Dynamic;
using System.Collections.Generic;
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
        /// Name of the key used to store the object type
        /// </summary>
        private const string TypeKey = "_type";

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
            IReadOnlyDictionary<string, object> values = packable.Pack();
            // Annoyingly we need to convert to a IDictionary
            Dictionary<string, object> mutable = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> pair in values)
                mutable.Add(pair.Key, pair.Value);
            if (storeTypeInformation)
                mutable.Add(TypeKey, packable.GetType().AssemblyQualifiedName);
            return JsonParser.ToJson(mutable);
		}

        /// <summary>
        /// Unpack a JSON string into a 'raw' dictionary format.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Dictionary<string, object> UnpackRaw(string json)
        {
            IDictionary<string, object> result;
            try
            {
                return (Dictionary<string, object>)JsonParser.FromJson(json);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Unpack a JSON file into a 'raw' dictionary format.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static Dictionary<string, object> UnpackRaw(Stream jsonFile)
        {
            StreamReader reader = new StreamReader(jsonFile);
            Dictionary<string, object> result = UnpackRaw(reader.ReadToEnd());
            jsonFile.Close();
            return result;
        }

		/// <summary>
		/// Unpack an object from JSON format.
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static IPackable Unpack(string json)
		{
            IReadOnlyDictionary<string, object> values = UnpackRaw(json);
            if (values == null)
                return null;
            // Do we have a type key ?
            Type t = null;
            if (values.ContainsKey(TypeKey))
            {
                try
                {
                    t = Type.GetType(values[TypeKey].ToString());
                }
                catch (Exception ex)
                {

                }
            }
            if (t== null)
            {
                // Create a generic object instance
                t = typeof(GenericPackedObject);
            }
            // Get the unpacker for this type
            Type unpackerType = typeof(IUnpacker<>).MakeGenericType(new Type[] { t });
            IUnpacker<IPackable> unpacker = (IUnpacker<IPackable>)Locator.Current.GetService(unpackerType);
            if (unpacker == null)
            {
                return null;
            }
            try
            {
                return unpacker.Unpack(values);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// Unpack an object from JSON format.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static IPackable Unpack(Stream jsonFile)
        {
            StreamReader reader = new StreamReader(jsonFile);
            IPackable result = Unpack(reader.ReadToEnd());
            jsonFile.Close();
            return result;
        }

        /// <summary>
        /// Unpack a specific type of object from JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Unpack<T>(string json) where T : IPackable
		{
            IReadOnlyDictionary<string, object> values = UnpackRaw(json);
            if (values == null)
                return default(T);
            IUnpacker<T> unpacker = Locator.Current.GetService<IUnpacker<T>>();
            if (unpacker == null)
            {
                return default(T);
            }
            try
            {
                return unpacker.Unpack(values);
            }
            catch (Exception ex)
            {
                return default(T);
            }
        }

        /// <summary>
        /// Unpack a specific type of object from JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Unpack<T>(Stream jsonFile) where T : IPackable
        {
            StreamReader reader = new StreamReader(jsonFile);
            T result = Unpack<T>(reader.ReadToEnd());
            jsonFile.Close();
            return result;
        }
    }
}
