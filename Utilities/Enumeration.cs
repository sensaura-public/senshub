using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensaura.Utilities
{
	public class Enumeration
	{
		public class EmptyEnumerator : IEnumerator
		{


			public EmptyEnumerator()
			{
			}

			public void Reset() { }

			public object Current
			{
				get
				{
					throw new InvalidOperationException();
				}
			}
			public bool MoveNext()
			{ return false; }
		}


		public class EmptyEnumerable : IEnumerable
		{

			public IEnumerator GetEnumerator()
			{
				return new EmptyEnumerator();
			}
		}
	}
}
