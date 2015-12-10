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

namespace SensHub.Server.Managers
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
	static class MetadataParser
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

			public IDictionary<string, object> Pack()
			{
				Dictionary<string, object> result = new Dictionary<string, object>();
				result["Name"] = Name;
				result["DisplayName"] = DisplayName;
				result["Description"] = Description;
				result["DetailedDescription"] = DetailedDescription;
				return result;
			}

		}

		/// <summary>
		/// Load metadata from a stream.
		/// </summary>
		/// <param name="baseName"></param>
		/// <param name="input"></param>
		public static void LoadFromStream(string baseName, Stream input)
		{
			LogHost.Default.Debug("Loading metadata for assembly '{0}'", baseName);
			XmlDocument document = new XmlDocument();
			try
			{
				document.Load(input);
				if (document.DocumentElement.Name != MetadataTag)
				{
					LogHost.Default.Warn("Unexpected document tag in metadata file for {0} - expected <{1}>, got <{2}>.", baseName, MetadataTag, document.DocumentElement.Name);
					return;
				}
			}
			catch (Exception ex)
			{
				LogHost.Default.Error("Unable to parse metadata file for {0} - {1}", baseName, ex.ToString());
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
						LogHost.Default.Warn("Unexpected tag found in metadata file for {0} - expected <{1}>, found <{2}>.", baseName, ClassTag, node.Name);
					else
					{
						attr = node.Attributes.GetNamedItem(NameAttribute);
						if (attr == null)
							LogHost.Default.Warn("Missing name attribute in class definition metadata for {0}.", baseName);
						else
						{
							string className = baseName + "." + attr.Value;
							LogHost.Default.Debug("Loading class definition for '{0}'", className);
							ProcessClass(baseName + "." + attr.Value, defaultLanguage, (XmlElement)node);
						}
					}						
				}
			}

		}

		#region XML Parser Helpers
		/// <summary>
		/// Get the localised text for a parent node.
		/// </summary>
		/// <param name="defaultLang"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		private static string GetLocalisedText(string defaultLang, XmlElement element, string textTag = TextTag)
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
				LogHost.Default.Warn("No text elements defined for this node.");
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
		private static ObjectConfiguration ProcessConfiguration(string defaultLang, XmlElement parent)
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
						LogHost.Default.Warn("Value definition is missing required attribute '{0}'", attrName);
					else
						attributes[attrName] = attr.Value;
				}
				if (attributes.Count != 3)
					continue; // Warning already logged
				// Verify the type
				ConfigurationValue.ValueType valueType;
				if (!Enum.TryParse<ConfigurationValue.ValueType>(attributes[TypeAttribute], out valueType))
				{
					LogHost.Default.Warn("Unsupported value type '{0}' for configuration attribute {1}", attributes[TypeAttribute], attributes[NameAttribute]);
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
							LogHost.Default.Warn("Option element for value '{0}' does not specify a name.", attributes[NameAttribute]);
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
		private static ObjectDescription ProcessDescription(string defaultLang, XmlElement parent)
		{
			ObjectDescription description = new ObjectDescription();
			// Get the text first
			description.DisplayName = GetLocalisedText(defaultLang, parent, DisplayNameTag);
			description.Description = GetLocalisedText(defaultLang, parent, ShortDescriptionTag);
			description.DetailedDescription = GetLocalisedText(defaultLang, parent, LongDescriptionTag);
			// TODO: Handle the icon differently
			return description;
		}

		/// <summary>
		/// Extract information for a class.
		/// </summary>
		/// <param name="className"></param>
		/// <param name="defaultLang"></param>
		/// <param name="element"></param>
		private static void ProcessClass(string className, string defaultLang, XmlElement element)
		{
			MasterObjectTable mot = Locator.Current.GetService<MasterObjectTable>();
			// Process configuration first
			var items = from node in element.ChildNodes.Cast<XmlNode>()
						where (node.Name == ConfigurationTag) && (node.NodeType == XmlNodeType.Element)
						select node;
			int count = items.Count();
			if (count > 1)
				LogHost.Default.Warn("Multiple configuration entries for class '{0}'", className);
			else if (count == 1)
			{
				LogHost.Default.Debug("Loading configuration definition for class '{0}'", className);
				mot.AddConfigurationDescription(className, ProcessConfiguration(defaultLang, (XmlElement)items.First()));
			}
			// Process description
			items = from node in element.ChildNodes.Cast<XmlNode>()
					where (node.Name == DescriptionTag) && (node.NodeType == XmlNodeType.Element)
					select node;
			count = items.Count();
			if (count > 1)
				LogHost.Default.Warn("Multiple description entries for class '{0}'", className);
			else if (count == 1)
			{
				LogHost.Default.Debug("Loading description definition for class '{0}'", className);
				mot.AddDescription(className, ProcessDescription(defaultLang, (XmlElement)items.First()));
			}
		}
		#endregion

	}
}
