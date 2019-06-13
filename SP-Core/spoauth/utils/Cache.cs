using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace spoauth
{
    public class CacheItem<T>
    {
        public T data { get; }
        public DateTime? expiration { get; }
        public CacheItem(T data, DateTime? expiration = null)
        {
            this.data = data;
            this.expiration = expiration;
        }
    }

    public class Cache<T>
    {
        private IDictionary<string, CacheItem<T>> _cache;

        public Cache()
        {
            _cache = new Dictionary<string, CacheItem<T>>();
        }

        public void set(string key, T data, DateTime? expiration = null)
        {
            CacheItem<T> cacheItem = null;
            key = getHashCode(key);
            if (null == expiration)
                cacheItem = new CacheItem<T>(data);
            else
                cacheItem = new CacheItem<T>(data, expiration);
            if (_cache.ContainsKey(key))
                _cache[key] = cacheItem;
            else
                _cache.Add(key, cacheItem);
        }

        public T get(string key)
        {
            key = getHashCode(key);
            if (!_cache.ContainsKey(key)) return default(T);
            var cacheItem = _cache[key];
            if (null == cacheItem) return default(T);
            if (null != cacheItem.expiration)
            {
                if (DateTime.Now > cacheItem.expiration)
                {
                    _cache.Remove(key);
                    return default(T);
                }
            }
            return cacheItem.data;
        }

        public void remove(string key)
        {
            key = getHashCode(key);
            if (_cache.ContainsKey(key))
                _cache.Remove(key);
        }

        public void Clear()
        {
            _cache = new Dictionary<string, CacheItem<T>>();
        }

        private string getHashCode(string key)
        {
            using (var alg = MD5.Create())
                return Convert.ToBase64String(alg.ComputeHash(Encoding.UTF8.GetBytes(key)));
        }
    }
}