using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	public interface ITopic
	{
		/// <summary>
		/// Get the parent of this topic.
		/// 
		/// This value will be null for the root topic.
		/// </summary>
		ITopic Parent { get; }

		/// <summary>
		/// Create (or acquire) a child topic.
		/// </summary>
		/// <param name="child">The name of the child topic to create.</param>
		/// <returns>The new topic</returns>
		ITopic Create(string child);
	}
}
