using System;

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
        public abstract void ApplyConfiguration(Configuration configuration);
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

        public abstract void ApplyConfiguration(Configuration configuration);
        public abstract AbstractSource CreateInstance(Guid id, Configuration config);
    }
}
