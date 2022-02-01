using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandyUI_PersonalWorkCategories.Utils
{
    public static class GetKeySafelyUtil
    {
        public static TValue GetSafety<TKey, TValue>(this Dictionary<TKey, TValue> source, TKey key)
        {
            try
            {
                return source[key];
            }
            catch (KeyNotFoundException ex)
            {
                throw new ValueKeyNotFoundException((object)key, ex.Message, ex.InnerException);
            }
        }
    }

    public class ValueKeyNotFoundException : KeyNotFoundException
    {
        public ValueKeyNotFoundException(object key, string message) : this(key, message, null) { }

        public ValueKeyNotFoundException(object key, string message, Exception innerException) : base(message, innerException)
        {
            this.Key = key;
        }

        public override string ToString()
        {
            return "Key [" + Key + "]" + " not found in the dictionary.\n";
        }

        public object Key { get; private set; }
    }
}
