using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensHub.Plugins
{
	/// <summary>
	/// The types of configuration values supported.
	/// </summary>
	public enum ConfigurationValueType
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

	/// <summary>
	/// Describes a configuration value
	/// </summary>
	public interface IConfigurationValue : IObjectDescription
	{
		/// <summary>
		/// The type of the value being represented
		/// </summary>
		ConfigurationValueType Type { get; }

		/// <summary>
		/// The default value for this entry
		/// </summary>
		object DefaultValue { get; }

		/// <summary>
		/// The list of options if for OptionList types
		/// </summary>
		List<IObjectDescription> Options { get; set; }

		/// <summary>
		/// The type of user objects supported for ObjectValue and ObjectList types
		/// </summary>
		UserObjectType Subtype { get; set; }

		/// <summary>
		/// Validate a value for this field.
		/// </summary>
		/// <param name="value">The value to validate</param>
		/// <param name="adjusted">The adjust value to store in the confguration</param>
		/// <returns></returns>
		bool Validate(object value, out object adjusted);
	}
}
