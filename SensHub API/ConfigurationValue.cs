using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	/// <summary>
	/// Describes a single configuration entry.
	/// </summary>
	public class ConfigurationValue : IDescribed
	{
		public enum ValueType
		{
			BooleanValue,
			NumericValue,
			DateValue,
			TimeValue,
			StringValue,
			TopicValue,
			TextValue,
			PasswordValue,
			ScriptValue,
			ObjectValue,
			ObjectList,
			OptionList,
		}

		public string DisplayName { get; private set; }

		public string Description { get; private set; }

		public ValueType Type { get; private set; }

		public object DefaultValue { get; private set; }

		public List<IObjectDescription> Options { get; set; }

		public Configuration Subtype { get; set; }

		public ConfigurationValue(string name, ValueType type, object defaultValue, string description)
		{
			DisplayName = name;
			Type = type;
			Description = description;
			DefaultValue = defaultValue;
		}

		/// <summary>
		/// Validate a value for this configuration entry
		/// </summary>
		/// <param name="value">The value to validate</param>
		/// <returns>The actual value to store. May be the same object or a newly created one.</returns>
		public object Validate(object value)
		{
			// TODO: Implement this
			return value;
		}
	}
}
