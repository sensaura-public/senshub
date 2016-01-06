using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SensHub.Plugins;

namespace SensHub.Core.Messages
{
	/// <summary>
	/// Topics are used to group messages. A topic may only be created as a
	/// child of another topic, the MessageBus itself represents the root
	/// topic for the entire system.
	/// </summary>
	public class Topic : ITopic
	{
		// Regular expression to match topics
		private static readonly Regex TOPIC_REGEX = new Regex(@"^[a-zA-Z0-9\-_]+$");
 
		// Separator characters for topic paths
		private static readonly char[] TOPIC_SEPARATOR = { '/' };

		//--- Instance variables
		private string m_name;     // Name of this topic
		private string m_fqname;   // Fully qualified name
		private Dictionary<string, Topic> m_children; // Child nodes

		/// <summary>
		/// Default constructor. Declared private so it cannot be invoked.
		/// </summary>
		private Topic()
		{

		}

		/// <summary>
		/// Constructor. Used internally only.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="name"></param>
		internal Topic(Topic parent, string name)
		{
			m_children = new Dictionary<string, Topic>();
			Parent = parent;
			m_name = name;
		}

		#region Public API
		/// <summary>
		/// Provide read only access to the parent topic.
		/// </summary>
		public ITopic Parent { get; private set; }

		/// <summary>
		/// Create (or acquire) a child topic.
		/// </summary>
		/// <param name="child">The name of the child topic to create.</param>
		/// <returns>The new topic</returns>
		public ITopic Create(string name)
		{
			// Check arguments
			if ((name == null) || (name.Length == 0))
				throw new ArgumentException("Topic name is null or empty.");
			// Split the name into parts (may be multiple children)
			string[] parts = name.Split(TOPIC_SEPARATOR, StringSplitOptions.None);
			// Verify all the parts
			foreach (string part in parts)
			{
				if (part.Length == 0)
					throw new ArgumentException("Topic contains an empty child name.");
				if (!TOPIC_REGEX.IsMatch(part))
					throw new ArgumentException("Topic contains a child name with illegal characters.");
			}
			// Create all the children
			Topic child = this;
			foreach (string part in parts)
				child = child.CreateDirectChild(part);
			// Return the final leaf
			return child;
		}

		/// <summary>
		/// Generate a string representation of the topic
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			// Generate the name if we need to
			if (m_fqname == null)
			{
				// Start with the parent name if we have one
				if (Parent != null)
					m_fqname = String.Format("{0}/{1}", Parent.ToString(), m_name);
				else
					m_fqname = m_name;
			}
			return m_fqname;
		}
		#endregion

		#region Internal Helpers
		/// <summary>
		/// Create (or retrieve) a direct child of this node by name
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected Topic CreateDirectChild(string name)
		{
			Topic child;
			lock (m_children)
			{
				if (!m_children.TryGetValue(name, out child))
				{
					// Create the new child
					child = new Topic(this, name);
					m_children.Add(name, child);
				}
			}
			return child;
		}
		#endregion
	}
}
