using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using SensHub.Plugins;
using Splat;

namespace SensHub.Core.Plugins
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

			public List<IObjectDescription> Options { get; private set; }

			public UserObjectType Subtype { get; private set; }
			#endregion

			public ConfigurationValue(string name, ConfigurationValueType type, object defaultValue, IObjectDescription description, List<IObjectDescription> options = null, UserObjectType subType = UserObjectType.None)
			{
				DisplayName = name;
				Type = type;
				Description = description.Description;
				DetailedDescription = description.DetailedDescription;
				Options = options;
				Subtype = subType;
				object verifiedDefault;
				if (!Validate(defaultValue, out verifiedDefault))
					throw new ArgumentException("Default value is not acceptable for this type.");
				DefaultValue = verifiedDefault;
			}

			/// <summary>
			/// Validate a value for this field.
			/// </summary>
			/// <param name="value">The value to validate</param>
			/// <param name="adjusted">The adjust value to store in the confguration</param>
			/// <returns></returns>
			public bool Validate(object value, out object adjusted)
			{
				bool result = true;
				string strVal;
				int intVal;
				adjusted = null;
				switch (Type)
				{
					case ConfigurationValueType.ScriptValue:
					case ConfigurationValueType.StringValue:
					case ConfigurationValueType.TextValue:
						// These are just arbitrary strings
						adjusted = (value == null) ? "" : value.ToString();
						break;
					case ConfigurationValueType.PasswordValue:
						// Like a string but must be more either 0 or more than 8 characters
						strVal = (value == null) ? "" : value.ToString();
						if ((strVal.Length > 0) && (strVal.Length < 8))
							result = false;
						else
						{
							// TODO: Validate contents
							adjusted = strVal;
						}
						break;
					case ConfigurationValueType.BooleanValue:
						// Accept 'true' (with any case) as true, everything else is false
						strVal = (value == null) ? "" : value.ToString().ToLower();
						adjusted = (strVal == "true");
						break;
					case ConfigurationValueType.OptionList:
						// Must be one of the given options
						strVal = (value == null) ? "" : value.ToString();
						result = false;
						foreach (IObjectDescription option in Options)
						{
							if (option.DisplayName == strVal)
							{
								adjusted = strVal;
								result = true;;
							}
						}
						break;
					case ConfigurationValueType.NumericValue:
						// Only integers are supported
						strVal = (value == null) ? "" : value.ToString();
						if (!Int32.TryParse(strVal, out intVal))
							result = false;
						else
							adjusted = intVal;
						break;
					case ConfigurationValueType.DateValue:
					case ConfigurationValueType.ObjectList:
					case ConfigurationValueType.ObjectValue:
					case ConfigurationValueType.TimeValue:
					case ConfigurationValueType.TopicValue:
						// TODO: Not implemented yet
						break;
				}
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
			public IDictionary<string, object> Verify(IDictionary<string, object> values, IDictionary<string, string> failures = null)
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
					catch (Exception) 
					{
						source = null;
					}
					if (source == null) 
					{
						success = false;
						if (failures != null)
							failures.Add(value.DisplayName, "");
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
			XDocument document = null;
			try
			{
				document = XDocument.Load(input);
				if (document.Root.Name != MetadataTag)
				{
					LogHost.Default.Warn("Unexpected document tag in metadata file for {0} - expected <{1}>, got <{2}>.", baseName, MetadataTag, document.Root.Name);
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
			XAttribute attr = document.Root.Attribute(DefaultLangAttribute);
			if (attr != null)
				defaultLanguage = attr.Value;
			// Find all the classes
			IEnumerable<XElement> nodes =
				from node in document.Root.Elements(ClassTag)
				where node.Attribute(NameAttribute) != null
				select node;
			foreach (XElement node in nodes)
			{
				attr = node.Attribute(NameAttribute);
				string className = baseName + "." + attr.Value;
				LogHost.Default.Debug("Loading class definition for '{0}'", className);
				ProcessClass(baseName, baseName + "." + attr.Value, defaultLanguage, node);
			}
		}

		#region XML Parser Helpers
		/// <summary>
		/// Get the localised text for a parent node.
		/// </summary>
		/// <param name="defaultLang"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		private static string GetLocalisedText(string defaultLang, XElement element, string textTag = TextTag)
		{
			IEnumerable<XElement> nodes =
				from node in element.Descendants(textTag)
				where (string)node.Attribute(LanguageAttribute) == CultureInfo.CurrentCulture.Name
				select node;
			if (nodes.Count() == 0)
			{
				// Look for default language instead
				nodes =
					from node in element.Descendants(textTag)
					where (string)node.Attribute(LanguageAttribute) == defaultLang
					select node;
			}
			StringBuilder sb = new StringBuilder("");
			foreach (XElement text in nodes)
			{
				sb.Append(text.Value);
			}
			return sb.ToString().Trim();
		}

		/// <summary>
		/// Process a configuration block
		/// </summary>
		/// <param name="defaultLang"></param>
		/// <param name="element"></param>
		/// <returns></returns>
		private static IConfigurationDescription ProcessConfiguration(string defaultLang, XElement parent)
		{
            List<IConfigurationValue> values = new List<IConfigurationValue>();
			IEnumerable<XElement> nodes =
				from node in parent.Descendants(ValueTag)
				where (node.Attribute(NameAttribute) != null) && (node.Attribute(TypeAttribute) != null) && (node.Attribute(DefaultAttribute) != null)
				select node;
            foreach (XElement node in nodes)
            {
                // Get required attributes (name, type and default)
                Dictionary<string, string> attributes = new Dictionary<string, string>();
                attributes[NameAttribute] = node.Attribute(NameAttribute).Value;
                attributes[TypeAttribute] = node.Attribute(TypeAttribute).Value;
                attributes[DefaultAttribute] = node.Attribute(DefaultAttribute).Value;
                LogHost.Default.Debug("Adding configuration entry '{0}' ({1})", attributes[NameAttribute], attributes[TypeAttribute]);
                // Verify the type
                ConfigurationValueType valueType;
                if (!Enum.TryParse(attributes[TypeAttribute], false, out valueType))
                {
                    LogHost.Default.Warn("Unrecognised configuration type '{0}'", attributes[TypeAttribute]);
                    continue;
                }
                // Process the description of the entry
                ObjectDescription description = ProcessDescription(defaultLang, node);
                // Do type specific configuration
				List<IObjectDescription> options = null;
                if (valueType == ConfigurationValueType.OptionList)
                {
                    // Build a list of individual options
					IEnumerable<XElement> optionNodes =
						from option in node.Descendants(SelectionTag)
						where option.Attribute(NameAttribute) != null
						select option;
                    if (optionNodes.Count() == 0)
                    {
                        LogHost.Default.Error("Configuration values of type 'OptionList' require options.");
                        return null;
                    }
                    options = new List<IObjectDescription>();
                    foreach (XElement item in optionNodes)
                        options.Add(ProcessDescription(defaultLang, item));
                }
				UserObjectType objectType = UserObjectType.None;
                if ((valueType == ConfigurationValueType.ObjectList) || (valueType == ConfigurationValueType.ObjectValue))
                {
                    if (!Enum.TryParse(node.Attribute(SubtypeAttribute).Value, true, out objectType))
                    {
                        LogHost.Default.Error("Fields of type {0} require a valid '{1}' attribute.", valueType, SubtypeAttribute);
                        return null;
                    }
                }
				// Create and add the value
				ConfigurationValue configValue = new ConfigurationValue(
					attributes[NameAttribute],
					valueType,
					attributes[DefaultAttribute],
					description,
					options,
					objectType
					);
				values.Add(configValue);
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
		private static ObjectDescription ProcessDescription(string defaultLang, XElement parent)
		{
			ObjectDescription description = new ObjectDescription();
			// Get the text first
			description.DisplayName = GetLocalisedText(defaultLang, parent, DisplayNameTag);
			description.Description = GetLocalisedText(defaultLang, parent, ShortDescriptionTag);
			description.DetailedDescription = GetLocalisedText(defaultLang, parent, LongDescriptionTag);
            // Handle the icon differently
			IEnumerable<XElement> nodes =
				from node in parent.Descendants(IconTag)
				where node.Attribute(ImageAttribute) != null
				select node;
            if (nodes.Count() > 0)
            {
				description.Icon = nodes.First().Attribute(ImageAttribute).Value.Trim();
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
		private static void ProcessClass(string assembly, string className, string defaultLang, XElement element)
		{
			MasterObjectTable mot = Locator.Current.GetService<MasterObjectTable>();
            // Get the descriptions
			IEnumerable<XElement> nodes =
				from node in element.Descendants(DescriptionTag)
				select node;
            if (nodes.Count() > 0)
            {
                // Process the description
                LogHost.Default.Debug("Loading description definition for class '{0}'", className);
                ObjectDescription description = ProcessDescription(defaultLang, nodes.First());
                if ((description.Icon != null) && (description.Icon.Length > 0))
                    description.Icon = ServiceManager.BaseImageUrl + "/" + assembly + "." + description.Icon;
                mot.AddDescription(className, description);
            }
			// Get the configurations
			nodes =
				from node in element.Descendants(ConfigurationTag)
				select node;
			if (nodes.Count() > 0)
            {
                // Process the configuration
                LogHost.Default.Debug("Loading configuration definition for class '{0}'", className);
                IConfigurationDescription config = ProcessConfiguration(defaultLang, nodes.First());
                if (config != null)
                    mot.AddConfigurationDescription(className, config);
            }
		}
		#endregion

	}
}
