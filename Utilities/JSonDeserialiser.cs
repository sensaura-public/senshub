using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Sensaura.Utilities
{
	public abstract class JSonDeserialiser
	{
		private static Dictionary<string, JSonDeserialiser> m_typemap = new Dictionary<string,JSonDeserialiser>();

		public static void RegisterSerialisedType(string typeID, JSonDeserialiser deserialiser)
		{
			if ((typeID == null) || (deserialiser == null))
				throw new ArgumentException("Serialisation type ID and deserialiser instance must not be null.");
			// Add the deserialiser (will overwrite any previous entry)
			lock (m_typemap)
			{
				m_typemap.Add(typeID, deserialiser);
			}
		}

		public abstract IJsonSerialisable Deserialise(IReadOnlyDictionary<string, object> packed);

		public static IJsonSerialisable Deserialise(string typeID, IReadOnlyDictionary<string, object> packed)
		{
			lock (m_typemap)
			{
				if (!m_typemap.ContainsKey(typeID))
					return null;
				return m_typemap[typeID].Deserialise(packed);
			}
		}

		public static IJsonSerialisable Deserialise(string typeID, string json)
		{
			// Convert the JSON string into a Dictionary (JObject)
			Dictionary<string, object> packed = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
			return Deserialise(typeID, packed);
		}

		public static IJsonSerialisable Deserialise(string json)
		{
			// Convert the JSON string into a Dictionary (JObject)
			Dictionary<string, object> packed = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
			if (!packed.ContainsKey("type") || !packed.ContainsKey("data"))
				return null;
			Dictionary<string, object> data = packed["data"] as Dictionary<string, object>;
			if (data == null)
				return null;
			return Deserialise(packed["type"].ToString(), data);
		}

		public static IJsonSerialisable Deserialise(string typeID, Stream json)
		{
			// Read the sensor description from the JSON file
			TextReader reader = new StreamReader(json);
			string text = reader.ReadToEnd();
			return Deserialise(typeID, text);
		}

		public static IJsonSerialisable Deserialise(Stream json)
		{
			// Read the sensor description from the JSON file
			TextReader reader = new StreamReader(json);
			string text = reader.ReadToEnd();
			return Deserialise(text);
		}
	}
}
