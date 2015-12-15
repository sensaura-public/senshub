using System;
using System.Collections.Generic;

namespace SensHub.Plugins
{
    public abstract class AbstractAction : IUserCreatableObject
    {
        private const UserObjectType MyType = UserObjectType.Action;

        #region IUserCreatableObject
        /// <summary>
        /// Always returns Action
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

        /// <summary>
        /// Invoke the action with the given message.
        /// 
        /// This will be performed on a background thread in the threadpool.
        /// </summary>
        /// <param name="message">
        /// The message that triggered the action. This may be null.
        /// </param>
        public abstract void Invoke(Message message);
    }

    /// <summary>
    /// Factory class for Action objects
    /// </summary>
    public abstract class AbstractActionFactory : IUserObjectFactory<AbstractAction>
    {
        private const UserObjectType MyType = UserObjectType.ActionFactory;

        /// <summary>
        /// Always returns ActionFactory
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

		public abstract AbstractAction CreateInstance(Guid id, IConfigurationDescription description, IDictionary<string, object> values);
    }
}
