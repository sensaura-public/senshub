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
	public class ConfigurationValue : IDescribed, IObjectDescription
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

        public string Icon { get; set; }

        public string DisplayName { get; private set; }

		public string Description { get; private set; }

        public string DetailedDescription { get; set; }

        public ValueType Type { get; private set; }

		public object DefaultValue { get; private set; }

		public List<IObjectDescription> Options { get; set; }

		public UserObjectType Subtype { get; set; }

        public ConfigurationValue(string name, ValueType type, object defaultValue, IObjectDescription description)
		{
			DisplayName = name;
			Type = type;
            Description = description.Description;
            DetailedDescription = description.DetailedDescription;
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
}
