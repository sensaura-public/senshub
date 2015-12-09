using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;
using SensHub.Server;

namespace SensHub.Server.Managers
{
    /// <summary>
    /// This class manages all the IUserObject instances in the system
    /// </summary>
    public class MasterObjectTable : IDictionary<Guid, IUserObject>, IPackable
    {
        /// <summary>
        /// Access the configuration information for objects based on ID
        /// </summary>
        public Configurations Configurations { get; private set; }

        public MasterObjectTable()
        {
            Configurations = new Configurations();
        }

        /// <summary>
        /// Pack the table in a form suitable for RPC
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, object> Pack()
        {
            throw new NotImplementedException();
        }

        #region Implementation of IDictionary<Guid, IUserObject>
        public IUserObject this[Guid key]
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public int Count
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ICollection<Guid> Keys
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public ICollection<IUserObject> Values
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Add(KeyValuePair<Guid, IUserObject> item)
        {
            throw new NotImplementedException();
        }

        public void Add(Guid key, IUserObject value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<Guid, IUserObject> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(Guid key)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<Guid, IUserObject>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<Guid, IUserObject>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<Guid, IUserObject> item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Guid key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(Guid key, out IUserObject value)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
