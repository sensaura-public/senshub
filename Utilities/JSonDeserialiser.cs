using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			throw new NotImplementedException();
		}

		public static IJsonSerialisable Deserialise(string json)
		{
			throw new NotImplementedException();
		}

		public static IJsonSerialisable Deserialise(string typeID, Stream json)
		{
			throw new NotImplementedException();
		}

		public static IJsonSerialisable Deserialise(Stream json)
		{
			throw new NotImplementedException();
		}
	}
}
