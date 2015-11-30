using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Splat;

namespace SensHub.Plugins
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
}
