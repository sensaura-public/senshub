using System;
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

		public abstract IJsonSerialisable Deserialise(string json);

		public static IJsonSerialisable Deserialise(string typeID, string json)
		{
			lock (m_typemap)
			{
				if (!m_typemap.ContainsKey(typeID))
					return null;
				return m_typemap[typeID].Deserialise(json);
			}
		}
	}
}
