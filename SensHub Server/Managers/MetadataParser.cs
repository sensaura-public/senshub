using System;
using System.IO;
using System.Xml;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
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
        // Base image URL
        private const string BaseImageUrl = "img/plugins/";

		// Tag names
		private const string MetadataTag = "metadata";
		private const string ClassTag = "class";
		private const string ConfigurationTag = "configuration";
		private const string ValueTag = "value";
		private const string DescriptionTag = "description";
		private const string SelectionTag = "selection";
		private const string TextTag = "text";
		private const string DisplayNameTag = "displayname";
		private const string ShortDescriptionTag = "shortdescription";
		private const string LongDescriptionTag = "longdescription";
		private const string IconTag = "icon";

		// Attribute names
		private const string DefaultLangAttribute = "defaultLang";
		private const string NameAttribute = "name";
		private const string LanguageAttribute = "lang";
		private const string TypeAttribute = "type";
        private const string SubtypeAttribute = "subtype";
		private const string DefaultAttribute = "default";
        private const string ImageAttribute = "image";

		/// <summary>
		/// Simple implementation of IObjectDescription
		/// </summary>
		private class ObjectDescription : IObjectDescription
		{
			public string Icon { get; internal set; }

			public string DisplayName { get; internal set; }

			public string Description { get; internal set; }

			public string DetailedDescription { get; internal set; }

			public IDictionary<string, object> Pack()
			{
				Dictionary<string, object> result = new Dictionary<string, object>();
				result["Icon"] = Icon;
				result["DisplayName"] = DisplayName;
				result["Description"] = Description;
				result["DetailedDescription"] = DetailedDescription;
				return result;
			}

		}

		/// <summary>
		/// Implementation of IConfigurationValue
		/// </summary>
		private class ConfigurationValue : IConfigurationValue
		{
			#region Implementation of IConfigurationValue
			public string Icon { get; set; }

			public string DisplayName { get; private set; }

			public string Description { get; private set; }

			public string DetailedDescription { get; set; }

			public ConfigurationValueType Type { get; private set; }

			public object DefaultValue { get; private set; }

			public List<IObjectDescription> Options { get; set; }

			public UserObjectType Subtype { get; set; }
			#endregion

			public ConfigurationValue(string name, ConfigurationValueType type, object defaultValue, IObjectDescription description)
			{
				DisplayName = name;
				Type = type;
				Description = description.Description;
				DetailedDescription = description.DetailedDescription;
				DefaultValue = defaultValue;
			}

			/// <summary>
			/// Validate a value for this field.
			/// </summary>
			/// <param name="value">The value to validate</param>
			/// <param name="adjusted">The adjust value to store in the confguration</param>
			/// <returns></returns>
			public bool Validate(object value, out object adjusted)
			{
				// TODO: More complete implementation for different types
				adjusted = value;
				return true;
			}

			/// <summary>
			/// Pack the configuration for transport
			/// </summary>
			/// <returns></returns>
			public IDictionary<string, object> Pack()
			{
				Dictionary<string, object> results = new Dictionary<string, object>();
				results["DisplayName"] = DisplayName;
				results["Icon"] = Icon;
				results["Description"] = Description;
				results["DetailedDescription"] = DetailedDescription;
				results["ValueType"] = Type.ToString();
				results["SubType"] = Subtype.ToString();
				if (Options != null)
				{
					List<IDictionary<string, object>> options = new List<IDictionary<string, object>>();
					foreach (IObjectDescription description in Options)
						options.Add(description.Pack());
					results["Options"] = options;
				}
				results["DefaultValue"] = DefaultValue;
				return results;
			}
		}

		/// <summary>
		/// Implementation of IConfigurationDescription
		/// </summary>
		private class ConfigurationDescription : IConfigurationDescription
		{
			// Instance variables
			private List<IConfigurationValue> m_configuration;
			private Dictionary<string, IConfigurationValue> m_mapping;

			/// <summary>
			/// Constructor from a list of configuration values
			/// </summary>
			/// <param name="description"></param>
			public ConfigurationDescription(List<IConfigurationValue> description)
			{
				m_configuration = description;
				BuildMapping();
			}

			/// <summary>
			/// Constructor from an array of configuration values
			/// </summary>
			/// <param name="description"></param>
			public ConfigurationDescription(IConfigurationValue[] description)
			{
				m_configuration = new List<IConfigurationValue>(description);
				BuildMapping();
			}

			/// <summary>
			/// Build the mapping of names to values for faster lookup.
			/// </summary>
			private void BuildMapping()
			{
				m_mapping = new Dictionary<string, IConfigurationValue>();
				foreach (IConfigurationValue value in m_configuration)
					m_mapping[value.DisplayName] = value;
			}

			/// <summary>
			/// Verify a set of data for the configuration
			/// </summary>
			/// <param name="values"></param>
			/// <param name="failed"></param>
			/// <returns></returns>
			public IDictionary<string, object> Verify(IDictionary<string, object> values, IList<string> failed = null)
			{
				bool success = true;
				Dictionary<string, object> result = new Dictionary<string, object>();
				foreach (IConfigurationValue value in m_configuration)
				{
					object source;
					if (values.ContainsKey(value.DisplayName))
						source = values[value.DisplayName];
					else
						source = value.DefaultValue;
					// Validate the value according to type

					try
					{
						object adjusted;
						source = (value.Validate(source, out adjusted)) ? adjusted : null;
					}
					catch (Exception) {
						source = null;
					}
					if (source == null) 
					{
						success = true;
						if (failed != null)
							failed.Add(value.DisplayName);
					}
					else
						result[value.DisplayName] = source;
				}
				return success?result:null;
			}

			/// <summary>
			/// Get the value applied to the named property.
			/// 
			/// This will return the configured value or the default if it not set.
			/// </summary>
			/// <param name="values"></param>
			/// <param name="name"></param>
			/// <returns></returns>
			public object GetAppliedValue(IDictionary<string, object> values, string name)
			{
				// Is it a valid name
				if (!m_mapping.ContainsKey(name))
					return null;
				// Do we have an explicit value ?
				if ((values != null) && (values.ContainsKey(name)))
					return values[name];
				// Return the default
				return m_mapping[name].DefaultValue;
			}

			#region Implementation of IReadOnlyList
			public IConfigurationValue this[int index]
			{
				get { return ((IReadOnlyList<IConfigurationValue>)m_configuration)[index]; }
			}

			public int Count
			{
				get { return ((IReadOnlyList<IConfigurationValue>)m_configuration).Count; }
			}

			public IEnumerator<IConfigurationValue> GetEnumerator()
			{
				return ((IReadOnlyList<IConfigurationValue>)m_configuration).GetEnumerator();
			}

			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return ((IEnumerable<IConfigurationValue>)m_configuration).GetEnumerator();
			}
			#endregion
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
			foreach (XmlNode node in document.DocumentElement.SelectNodes(String.Format("{0}[@{1}]", ClassTag, NameAttribute)))
			{
				attr = node.Attributes.GetNamedItem(NameAttribute);
				string className = baseName + "." + attr.Value;
				LogHost.Default.Debug("Loading class definition for '{0}'", className);
				ProcessClass(baseName, baseName + "." + attr.Value, defaultLanguage, (XmlElement)node);
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
            XmlNodeList nodes = element.SelectNodes(String.Format("{0}[@{1}='{2}']", textTag, LanguageAttribute, CultureInfo.CurrentCulture.Name));
            if (nodes.Count == 0) // Look for default language instead
                nodes = element.SelectNodes(String.Format("{0}[@{1}='{2}']", textTag, LanguageAttribute, defaultLang));
			StringBuilder sb = new StringBuilder("");
			foreach (XmlElement text in nodes)
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
		private static IConfigurationDescription ProcessConfiguration(string defaultLang, XmlElement parent)
		{
            List<IConfigurationValue> values = new List<IConfigurationValue>();
            XmlNodeList nodes = parent.SelectNodes(String.Format("{0}[@{1} and @{2} and @{3}]", ValueTag, NameAttribute, TypeAttribute, DefaultAttribute));
            foreach (XmlNode node in nodes)
            {
                // Get required attributes (name, type and default)
                Dictionary<string, string> attributes = new Dictionary<string, string>();
                attributes[NameAttribute] = node.Attributes.GetNamedItem(NameAttribute).Value;
                attributes[TypeAttribute] = node.Attributes.GetNamedItem(TypeAttribute).Value;
                attributes[DefaultAttribute] = node.Attributes.GetNamedItem(DefaultAttribute).Value;
                LogHost.Default.Debug("Adding configuration entry '{0}' ({1})", attributes[NameAttribute], attributes[TypeAttribute]);
                // Verify the type
                ConfigurationValueType valueType;
                if (!Enum.TryParse(attributes[TypeAttribute], false, out valueType))
                {
                    LogHost.Default.Warn("Unrecognised configuration type '{0}'", attributes[TypeAttribute]);
                    continue;
                }
                // Process the description of the entry
                ObjectDescription description = ProcessDescription(defaultLang, (XmlElement)node);
                ConfigurationValue configValue = new ConfigurationValue(
                    attributes[NameAttribute],
                    valueType,
                    attributes[DefaultAttribute],
                    description
                    );
                values.Add(configValue);
                // Do type specific configuration
                if (valueType == ConfigurationValueType.OptionList)
                {
                    // Build a list of individual options
                    XmlNodeList optionNodes = node.SelectNodes(string.Format("{0}[@{1}]", SelectionTag, NameAttribute));
                    if (optionNodes.Count == 0)
                    {
                        LogHost.Default.Error("Configuration values of type 'OptionList' require options.");
                        return null;
                    }
                    List<IObjectDescription> options = new List<IObjectDescription>();
                    foreach (XmlNode item in optionNodes)
                        options.Add(ProcessDescription(defaultLang, (XmlElement)item));
                    configValue.Options = options;
                }
                if ((valueType == ConfigurationValueType.ObjectList) || (valueType == ConfigurationValueType.ObjectValue))
                {
                    UserObjectType objectType;
                    if (!Enum.TryParse(node.Attributes.GetNamedItem(SubtypeAttribute).Value, true, out objectType))
                    {
                        LogHost.Default.Error("Fields of type {0} require a valid '{1}' attribute.", valueType, SubtypeAttribute);
                        return null;
                    }
                    configValue.Subtype = objectType;
                }
			}
			return new ConfigurationDescription(values);
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
            // Handle the icon differently
            XmlNodeList nodes = parent.SelectNodes(string.Format("{0}[@{1}]", IconTag, ImageAttribute));
            if (nodes.Count > 0)
            {
                XmlAttribute attr = (XmlAttribute)nodes[0].Attributes.GetNamedItem(ImageAttribute);
                description.Icon = attr.Value.Trim();
            }
            // Done
            return description;
		}

		/// <summary>
		/// Extract information for a class.
		/// </summary>
        /// <param name="assembly"></param>
		/// <param name="className"></param>
		/// <param name="defaultLang"></param>
		/// <param name="element"></param>
		private static void ProcessClass(string assembly, string className, string defaultLang, XmlElement element)
		{
			MasterObjectTable mot = Locator.Current.GetService<MasterObjectTable>();
            // Process the description
            XmlNodeList nodes = element.SelectNodes(DescriptionTag);
            if (nodes.Count > 0)
            {
                // Process the description
                LogHost.Default.Debug("Loading description definition for class '{0}'", className);
                ObjectDescription description = ProcessDescription(defaultLang, (XmlElement)nodes[0]);
                if ((description.Icon != null) && (description.Icon.Length > 0))
                    description.Icon = BaseImageUrl + assembly + "." + description.Icon;
                mot.AddDescription(className, description);
            }
            nodes = element.SelectNodes(ConfigurationTag);
            if (nodes.Count > 0)
            {
                // Process the configuration
                LogHost.Default.Debug("Loading configuration definition for class '{0}'", className);
                IConfigurationDescription config = ProcessConfiguration(defaultLang, (XmlElement)nodes[0]);
                if (config != null)
                    mot.AddConfigurationDescription(className, config);
            }
		}
		#endregion

	}
}
