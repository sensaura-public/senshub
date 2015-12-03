using System;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using Splat;

namespace SensHub.Server
{
	/// <summary>
	/// This class is responsible for loading the 'metadata' for plugins and
	/// supporting classes. The metadata consists of the descriptive text
	/// for items (DisplayName, Description, etc) as well as configuration
	/// definitions.
	/// 
	/// This data is stored in a 'metadata.xml' file in the resource fork
	/// of each assembly.
	/// </summary>
	public class MetadataManager : IEnableLogger
	{
		// Tag names
		private const string MetadataTag = "metadata";
		private const string ClassTag = "class";
		private const string ConfigurationTag = "configuration";
		private const string ValueTag = "value";
		private const string DescriptionTag = "description";
		private const string SelectionTag = "selection";
		private const string TextTag = "text";
		private const string OptionTag = "option";
		private const string DisplayNameTag = "displayname";
		private const string ShortDescriptionTag = "shortdescription";
		private const string LongDescriptionTag = "longdescription";
		private const string IconTag = "icon";

		// Attribute names
		private const string DefaultLangAttribute = "defaultLang";
		private const string NameAttribute = "name";
		private const string LanguageAttribute = "lang";
		private const string TypeAttribute = "type";
		private const string DefaultAttribute = "default";

		/// <summary>
		/// Simple implementation of IObjectDescription
		/// </summary>
		private class ObjectDescription : IObjectDescription
		{

			public string Name { get; internal set; }

			public IBitmap Icon { get; internal set; }

			public string DisplayName { get; internal set; }

			public string Description { get; internal set; }

			public string DetailedDescription { get; internal set; }

		}

		// Instance variables
		private Dictionary<string, IObjectDescription> m_descriptions = new Dictionary<string, IObjectDescription>();
		private Dictionary<string, ObjectConfiguration> m_configurations = new Dictionary<string, ObjectConfiguration>();

		public MetadataManager()
		{

		}

		/// <summary>
		/// Load metadata from a stream.
		/// </summary>
		/// <param name="baseName"></param>
		/// <param name="input"></param>
		public void LoadFromStream(string baseName, Stream input)
		{
			this.Log().Debug("Loading metadata for assembly '{0}'", baseName);
			XmlDocument document = new XmlDocument();
			try
			{
				document.Load(input);
				if (document.DocumentElement.Name != MetadataTag)
				{
					this.Log().Warn("Unexpected document tag in metadata file for {0} - expected <{1}>, got <{2}>.", baseName, MetadataTag, document.DocumentElement.Name);
					return;
				}
			}
			catch (Exception ex)
			{
				this.Log().Error("Unable to parse metadata file for {0} - {1}", baseName, ex.ToString());
				return;
			}
			// Get the default language
			string defaultLanguage = "en";
			XmlNode attr = document.DocumentElement.Attributes.GetNamedItem(DefaultLangAttribute);
			if (attr != null)
				defaultLanguage = attr.Value;
			// Find all the classes
			foreach (XmlNode node in document.DocumentElement.ChildNodes)
			{
				if (node.NodeType == XmlNodeType.Element)
				{
					if (node.Name != ClassTag)
						this.Log().Warn("Unexpected tag found in metadata file for {0} - expected <{1}>, found <{2}>.", baseName, ClassTag, node.Name);
					else
					{
						attr = node.Attributes.GetNamedItem(NameAttribute);
						if (attr == null)
							this.Log().Warn("Missing name attribute in class definition metadata for {0}.", baseName);
						else
						{
							string className = baseName + "." + attr.Value;
							this.Log().Debug("Loading class definition for '{0}'", className);
							ProcessClass(baseName + "." + attr.Value, defaultLanguage, (XmlElement)node);
						}
					}						
				}
			}

		}

		/// <summary>
		/// Load metadata from an embedded resource in an assembly
		/// </summary>
		/// <param name="assembly"></param>
		public void LoadFromAssembly(Assembly assembly)
		{
			Stream source = assembly.GetManifestResourceStream(assembly.GetName().Name + ".Resources.metadata.xml");
			if (source == null)
			{
				this.Log().Warn("Could not find metadata resource for assembly {0}.", assembly.GetName().Name);
				return;
			}
			LoadFromStream(assembly.GetName().Name, source);
		}

		/// <summary>
		/// Get a description by fully qualified class name
		/// </summary>
		/// <param name="classname"></param>
		/// <returns></returns>
		public IObjectDescription GetDescription(string className)
		{
			IObjectDescription description;
			if (!m_descriptions.TryGetValue(className, out description))
				return null;
			return description;
		}

		/// <summary>
		/// Get the description for objects of a particular type
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public IObjectDescription GetDescription(Type t)
		{
			string className = String.Format("{0}.{1}",
				t.Namespace,
				t.Name);
			return GetDescription(className);
		}

		/// <summary>
		/// Get the description for objects of a particular type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public IObjectDescription GetDescription<T>()
		{
			return GetDescription(typeof(T));
		}

		/// <summary>
		/// Get a named configuration
		/// </summary>
		/// <param name="className"></param>
		/// <returns></returns>
		public ObjectConfiguration GetConfiguration(string className)
		{
			ObjectConfiguration config;
			if (!m_configurations.TryGetValue(className, out config))
				return null;
			return config;
		}

		/// <summary>
		/// Get configuration description for a given type.
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public ObjectConfiguration GetConfiguration(Type t)
		{
			string className = String.Format("{0}.{1}",
				t.Namespace,
				t.Name);
			return GetConfiguration(className);
		}

		/// <summary>
		/// Get the configuration for a specific type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ObjectConfiguration GetConfiguration<T>()
		{
			return GetConfiguration(typeof(T));
		}

		#region XML Parser Helpers
		/// <summary>
		/// Get the localised text for a parent node.
		/// </summary>
		/// <param name="defaultLang"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		private string GetLocalisedText(string defaultLang, XmlElement element, string textTag = TextTag)
		{
			var items = from node in element.ChildNodes.Cast<XmlElement>()
						where (node.Name == textTag) && (node.Attributes.GetNamedItem(LanguageAttribute).Value == CultureInfo.CurrentCulture.Name)
						select node;
			int count = items.Count();
			if (count == 0)
			{
				// Look for the default language instead
				items = from node in element.ChildNodes.Cast<XmlElement>()
						where (node.Name == textTag) && (node.Attributes.GetNamedItem(LanguageAttribute).Value == defaultLang)
						select node;
				count = items.Count();
			}
			StringBuilder sb = new StringBuilder();
			if (count == 0) 
			{
				this.Log().Warn("No text elements defined for this node.");
				sb.Append("(unspecified)");
			}
			foreach (XmlElement text in items)
			{
				sb.Append(text.InnerText);
			}
			return sb.ToString().Trim();
		}

		/// <summary>
		/// Process a configuration block
		/// </summary>
		/// <param name="defaultLang"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		private ObjectConfiguration ProcessConfiguration(string defaultLang, XmlElement parent)
		{
			List<ConfigurationValue> values = new List<ConfigurationValue>();
			var items = from node in parent.ChildNodes.Cast<XmlElement>()
						where node.Name == ValueTag
						select node;
			foreach (XmlElement element in items)
			{
				// Get required attributes (name, type and default)
				Dictionary<string, string> attributes = new Dictionary<string,string>();
				XmlAttribute attr;
				foreach (string attrName in new string[] { NameAttribute, TypeAttribute, DefaultAttribute })
				{
					attr = element.Attributes.GetNamedItem(attrName) as XmlAttribute;
					if (attr == null)
						this.Log().Warn("Value definition is missing required attribute '{0}'", attrName);
					else
						attributes[attrName] = attr.Value;
				}
				if (attributes.Count != 3)
					continue; // Warning already logged
				// Verify the type
				ConfigurationValue.ValueType valueType;
				if (!Enum.TryParse<ConfigurationValue.ValueType>(attributes[TypeAttribute], out valueType))
				{
					this.Log().Warn("Unsupported value type '{0}' for configuration attribute {1}", attributes[TypeAttribute], attributes[NameAttribute]);
					continue;
				}
				// We can build the configuration entry now
				string description = GetLocalisedText(defaultLang, element);
				ConfigurationValue configValue = new ConfigurationValue(
					attributes[NameAttribute],
					valueType,
					attributes[DefaultAttribute],
					description
					);
				if (valueType == ConfigurationValue.ValueType.OptionList)
				{
					// Process any options available
					List<IObjectDescription> optionInfo = new List<IObjectDescription>();
					var options = from node in element.ChildNodes.Cast<XmlElement>()
								  where node.Name == OptionTag
								  select node;
					foreach (XmlElement optionElement in options)
					{
						attr = element.Attributes.GetNamedItem(NameAttribute) as XmlAttribute;
						if (attr == null)
						{
							this.Log().Warn("Option element for value '{0}' does not specify a name.", attributes[NameAttribute]);
							continue;
						}
						ObjectDescription optionDescription = ProcessDescription(defaultLang, optionElement);
						if (optionDescription != null) 
						{
							optionDescription.Name = attr.Value;
							optionInfo.Add(optionDescription);
						}
					}
					configValue.Options = optionInfo;
				}
				values.Add(configValue);
			}
			return new ObjectConfiguration(values);
		}

		/// <summary>
		/// Extract a description from the given element.
		/// 
		/// This is used for the object description as well as individual item descriptions.
		/// </summary>
		/// <param name="defaultLang"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		private ObjectDescription ProcessDescription(string defaultLang, XmlElement parent)
		{
			ObjectDescription description = new ObjectDescription();
			// Get the text first
			description.DisplayName = GetLocalisedText(defaultLang, parent, DisplayNameTag);
			description.Description = GetLocalisedText(defaultLang, parent, ShortDescriptionTag);
			description.DetailedDescription = GetLocalisedText(defaultLang, parent, LongDescriptionTag);
			// TODO: Handle the icon differently
			return description;
		}

		private void ProcessClass(string className, string defaultLang, XmlElement element)
		{
			// Process configuration first
			var items = from node in element.ChildNodes.Cast<XmlElement>()
						where node.Name == ConfigurationTag
						select node;
			int count = items.Count();
			if (count > 1)
				this.Log().Warn("Multiple configuration entries for class '{0}'", className);
			else if (count == 1)
			{
				this.Log().Debug("Loading configuration definition for class '{0}'", className);
				m_configurations.Add(className, ProcessConfiguration(defaultLang, items.First()));
			}
			// Process description
			items = from node in element.ChildNodes.Cast<XmlElement>()
					where node.Name == DescriptionTag
					select node;
			count = items.Count();
			if (count > 1)
				this.Log().Warn("Multiple description entries for class '{0}'", className);
			else if (count == 1)
			{
				this.Log().Debug("Loading description definition for class '{0}'", className);
				m_descriptions.Add(className, ProcessDescription(defaultLang, items.First()));
			}
		}
		#endregion

	}
}
