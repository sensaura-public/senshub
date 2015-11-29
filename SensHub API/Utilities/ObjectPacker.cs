using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace SensHub.Plugins.Utilities
{
	/// <summary>
	/// This interface must be implemented by classes that can be packed
	/// and unpacked from persistant storage.
	/// </summary>
	public interface IPackable
	{
		IReadOnlyDictionary<string, object> Pack();
	}

	/// <summary>
	/// This interface defines an unpacker to create object instances from
	/// packed dictionaries.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IUnpacker<T> where T : IPackable
	{
		T Unpack(IReadOnlyDictionary<string, object> packed);
	}

	/// <summary>
	/// This class provides static helper methods to pack and unpack objects.
	/// </summary>
	public static class ObjectPacker
	{
		private static IPackable Unpack(Type t, IReadOnlyDictionary<string, object> packed)
		{
			Type unpackerType = typeof(IUnpacker<>).MakeGenericType(t);
			IUnpacker<IPackable> unpacker = Locator.Current.GetService(unpackerType) as IUnpacker<IPackable>;
			if (unpacker == null)
				throw new NotImplementedException(String.Format("No Unpacker available for type {0}", t.FullName));
			return unpacker.Unpack(packed);
		}

		/// <summary>
		/// Unpack an object from dictionary representation.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="packed"></param>
		/// <returns></returns>
		public static T Unpack<T>(IReadOnlyDictionary<string, object> packed) where T : IPackable
		{
			return (T)Unpack(typeof(T), packed);
		}

		/// <summary>
		/// Unpack from a JSON representation when the type is known
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="json"></param>
		/// <returns></returns>
		public static T Unpack<T>(string json) where T : IPackable
		{
			// Convert the JSON string into a Dictionary (JObject)
			Dictionary<string, object> packed = new Dictionary<string, object>();
			return (T)Unpack(typeof(T), packed);
		}

		/// <summary>
		/// Unpack from a json representation where the type is encoded in the format.
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static IPackable Unpack(string json)
		{
			// Convert the JSON string into a Dictionary (JObject)
			Dictionary<string, object> packed = new Dictionary<string, object>();
			if (!packed.ContainsKey("type") || !packed.ContainsKey("data"))
				return null;
			// Convert the type name to a Type instance
			Type t = Type.GetType(packed["type"].ToString());
			if (t == null)
				throw new NotSupportedException(String.Format("Unrecognised type '{0}'", packed["type"]));
			// Get the data block
			Dictionary<string, object> data = packed["data"] as Dictionary<string, object>;
			if (data == null)
				return null;
			// Now unpack it
			return Unpack(t, data);
		}

		/// <summary>
		/// Unpack from a stream where the object type is known.
		/// </summary>
		/// <param name="typeID"></param>
		/// <param name="json"></param>
		/// <returns></returns>
		public static T Unpack<T>(Stream json) where T : IPackable
		{
			// Read the sensor description from the JSON file
			TextReader reader = new StreamReader(json);
			string text = reader.ReadToEnd();
			// Convert the JSON string into a Dictionary (JObject)
			Dictionary<string, object> packed = new Dictionary<string, object>();
			return (T)Unpack(typeof(T), packed);
		}

		/// <summary>
		/// Unpack from a stream where the type is not known
		/// </summary>
		/// <param name="json"></param>
		/// <returns></returns>
		public static IPackable Unpack(Stream json)
		{
			// Read the sensor description from the JSON file
			TextReader reader = new StreamReader(json);
			string text = reader.ReadToEnd();
			return Unpack(text);
		}
	}
}
