using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensaura.Utilities
{
	public interface IJsonSerialisable
	{
		string SerialisationTypeID { get; }

		IReadOnlyDictionary<string, object> Pack();
	}
}
