using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace HandyUI_PersonalWorkCategories
{
    public partial class OrderedDictionary<TKey, TValue>
    {
        List<TKey> _innerList;
        Dictionary<TKey, TValue> _innerDictionary;

        /*
        public List<TKey> Keys
        { 
            get
            {
                return GetKeysAsList();
            }

            set
            {
                _innerList = value;

                _innerDictionary = new Dictionary<TKey, TValue>();
                foreach (TKey key in _innerList)
                {
                    _innerDictionary.Add(key, new TValue());
                }
            }
        }
        */

        public OrderedDictionary(OrderedDictionary<TKey, TValue> source)
        {
            _innerList = source.GetKeysAsList();
            _innerDictionary = source.GetAsDictionary();
        }

        public OrderedDictionary()
        {
            _innerDictionary = new Dictionary<TKey, TValue>();
            _innerList = new List<TKey>();
        }

        public OrderedDictionary(List<TKey> list, Dictionary<TKey, TValue> dictionary)
        {
            _innerList = list.ListFullCopy();
            _innerDictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        public TValue GetAt(int index)
        {
            return _innerDictionary[_innerList[index]];
        }

        public void SetAt(int index, TValue value)
        {
            var key = _innerList[index];
            _innerDictionary[key] = value;
        }

        public void Insert(int index, TKey key, TValue value)
        {
            _innerList.Insert(index, key);
            _innerDictionary.Add(key, value);
        }

        public void Add(TKey key, TValue value)
        {
            _innerList.Add(key);
            _innerDictionary.Add(key, value);
        }

        void _AddValues(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            foreach (var kvp in collection)
            {
                Add(kvp.Key, kvp.Value);
            }
        }

        public List<TKey> GetKeysAsList()
        {
            return _innerList.ListFullCopy();
        }

        public Dictionary<TKey, TValue> GetAsDictionary()
        {
            return new Dictionary<TKey, TValue>(_innerDictionary);
        }

        public TValue GetByKey(TKey key)
        {
            return _innerDictionary[key];
        }

        public void Remove(TKey key)
        {
            _innerList.Remove(key);
            _innerDictionary.Remove(key);
        }

        internal int IndexOf(TKey key)
        {
            return _innerList.IndexOf(key);
        }
    }
}
