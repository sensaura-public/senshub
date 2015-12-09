using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SensHub.Plugins;

namespace SensHub.Server.Managers
{
    public class Configurations : IDictionary<Guid, Configuration>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        internal Configurations()
        {

        }

        #region Implementation of IDictionary<Guid, Configuration>
        public Configuration this[Guid key]
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

        public ICollection<Configuration> Values
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Add(KeyValuePair<Guid, Configuration> item)
        {
            throw new NotImplementedException();
        }

        public void Add(Guid key, Configuration value)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<Guid, Configuration> item)
        {
            throw new NotImplementedException();
        }

        public bool ContainsKey(Guid key)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(KeyValuePair<Guid, Configuration>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<Guid, Configuration>> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<Guid, Configuration> item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Guid key)
        {
            throw new NotImplementedException();
        }

        public bool TryGetValue(Guid key, out Configuration value)
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
