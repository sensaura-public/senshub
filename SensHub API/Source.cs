using System;
using System.Collections.Generic;

namespace SensHub.Plugins
{
    public abstract class AbstractSource : IUserCreatableObject
    {
        private const UserObjectType MyType = UserObjectType.Source;

        #region IUserCreatableObject
        /// <summary>
        /// Always returns Source
        /// </summary>
        public UserObjectType ObjectType { get { return MyType; } }

        public abstract Guid ParentUUID { get; }
        public abstract Guid UUID { get; }
        #endregion

        #region IConfigurable
		public virtual bool ValidateConfiguration(IConfigurationDescription description, System.Collections.Generic.IDictionary<string, object> values, System.Collections.Generic.IDictionary<string, string> failures)
		{
			// Default is to do nothing
			return true;
		}

		public abstract void ApplyConfiguration(IConfigurationDescription description, System.Collections.Generic.IDictionary<string, object> values);
		#endregion
	}

    /// <summary>
    /// Factory class for Source objects
    /// </summary>
    public abstract class AbstractSourceFactory : IUserObjectFactory<AbstractSource>
    {
        private const UserObjectType MyType = UserObjectType.SourceFactory;

        /// <summary>
        /// Always returns SourceFactory
        /// </summary>
        public UserObjectType ObjectType { get { return MyType; } }

        public abstract Guid UUID { get; }

		#region IConfigurable
		public virtual bool ValidateConfiguration(IConfigurationDescription description, System.Collections.Generic.IDictionary<string, object> values, System.Collections.Generic.IDictionary<string, string> failures)
		{
			// Default is to do nothing
			return true;
		}

		public abstract void ApplyConfiguration(IConfigurationDescription description, System.Collections.Generic.IDictionary<string, object> values);
		#endregion
		
		public abstract AbstractSource CreateInstance(Guid id, IConfigurationDescription description, IDictionary<string, object> values);
    }
}
