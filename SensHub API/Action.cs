using System;

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
        public abstract void ApplyConfiguration(Configuration configuration);
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

        public abstract void ApplyConfiguration(Configuration configuration);
        public abstract AbstractAction CreateInstance(Guid id, Configuration config);
    }
}
