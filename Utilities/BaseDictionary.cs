using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sensaura.Utilities
{
    public class BaseDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    {
		// Internal dictionary instance
        private Dictionary<TKey, TValue> m_data;

		// Event for changes
		protected delegate void ValueChangedHandler(IDictionary<TKey, TValue> container, TKey key, TValue value);
		protected event ValueChangedHandler ValueChanged;

		// Event for removals
		protected delegate void ValueRemovedHandler(IDictionary<TKey, TValue> container, TKey key);
		protected event ValueRemovedHandler ValueRemoved;

		/// <summary>
		/// Fire the value changed event
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		private void FireValueChanged(TKey key, TValue value)
		{

		}

		/// <summary>
		/// Fire the value removed event
		/// </summary>
		/// <param name="key"></param>
		private void FireValueRemoved(TKey key)
		{

		}

		#region IDictionary Implementation
		public void Add(TKey key, TValue value)
		{
			if (m_data == null)
				m_data = new Dictionary<TKey, TValue>();
			m_data.Add(key, value);
			FireValueChanged(key, value);
		}

		public bool ContainsKey(TKey key)
		{
			if (m_data == null)
				return false;
			return m_data.ContainsKey(key);
		}

		public ICollection<TKey> Keys
		{
			get 
			{
				if (m_data == null)
					return Enumerable.Empty<TKey>().ToList();
				return m_data.Keys;
			}
		}

		public bool Remove(TKey key)
		{
			if (m_data == null)
				return false;
			if(m_data.Remove(key))
			{
				FireValueRemoved(key);
				return true;
			}
			return false;
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			if (m_data == null)
			{
				value = default(TValue);
				return false;
			}
			return m_data.TryGetValue(key, out value);
		}

		public ICollection<TValue> Values
		{
			get
			{
				if (m_data == null)
					return Enumerable.Empty<TValue>().ToList();
				return m_data.Values;
			}
		}

		public TValue this[TKey key]
		{
			get
			{
				if (m_data == null)
					throw new KeyNotFoundException();
				return m_data[key];
			}
			set
			{
				Add(key, value);
			}
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		public void Clear()
		{
			if (m_data != null)
				m_data.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			if (m_data == null)
				return false;
			return m_data.Contains(item);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			if (m_data == null)
				return;
			((IDictionary<TKey, TValue>)m_data).CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get 
			{
				if (m_data == null)
					return 0;
				return m_data.Count;
			}
		}

		public bool IsReadOnly
		{
			get 
			{
				return false;
			}
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			if (m_data == null)
				return false;
			if (((IDictionary<TKey, TValue>)m_data).Remove(item))
			{
				FireValueRemoved(item.Key);
				return true;
			}
			return false;
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			if (m_data == null)
				return Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
			return m_data.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			if (m_data == null)
				return new Enumeration.EmptyEnumerator();
			return ((IEnumerable)m_data).GetEnumerator();
		}
		#endregion

		#region Implementation of IReadOnlyDictionary
		bool IReadOnlyDictionary<TKey, TValue>.ContainsKey(TKey key)
		{
			if (m_data == null)
				return false;
			return m_data.ContainsKey(key);
		}

		IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys
		{
			get 
			{
				if (m_data == null)
					return Enumerable.Empty<TKey>();
				return m_data.Keys;
			}
		}

		bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
		{
			if (m_data == null)
			{
				value = default(TValue);
				return false;
			}
			return m_data.TryGetValue(key, out value);
		}

		IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values
		{
			get
			{
				if (m_data == null)
					return Enumerable.Empty<TValue>();
				return m_data.Values;
			}
		}

		TValue IReadOnlyDictionary<TKey, TValue>.this[TKey key]
		{
			get
			{
				if (m_data == null)
					throw new KeyNotFoundException();
				return m_data[key];
			}
		}

		int IReadOnlyCollection<KeyValuePair<TKey, TValue>>.Count
		{
			get
			{
				if (m_data == null)
					return 0;
				return m_data.Count;
			}
		}

		IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
		{
			if (m_data == null)
				return Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator();
			return ((IEnumerable<KeyValuePair<TKey, TValue>>)m_data).GetEnumerator();
		}
		#endregion
	}
}
