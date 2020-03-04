// Copyright (c) 2020 Sergey Ivonchik
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
// OR OTHER DEALINGS IN THE SOFTWARE.

using System.Collections.Generic;
using Httx.Caches.Disk;
using Httx.Caches.Memory;
using Httx.Requests.Awaiters;

namespace Httx.Caches {
  public class Cache {
    private static Cache instance;
    public static Cache Instance => instance ?? (instance = new Cache());

    private readonly Dictionary<string, MemoryCache<object>> memoryCaches =
      new Dictionary<string, MemoryCache<object>>();

    private readonly Dictionary<string, DiskLruCache> diskCaches =
      new Dictionary<string, DiskLruCache>();

    public void Put<T>(string key, T value, Args cacheArgs) {
      var cacheId = cacheArgs.Id;

      memoryCaches.TryGetValue(cacheId, out var cache);

      if (null == cache) {
        var maxAge = default == cacheArgs.MaxAge ? 0 : (int) cacheArgs.MaxAge.TotalMilliseconds;
        var collectFrequency = cacheArgs.EvictValue;

        cache = new MemoryCache<object>(cacheArgs.MaxSize, maxAge, collectFrequency);
        memoryCaches[cacheId] = cache;
      }

      cache.Put(key, value);
    }

    public T Get<T>(string key, Args cacheArgs) {
      var cacheId = cacheArgs.Id;

      memoryCaches.TryGetValue(cacheId, out var cache);

      if (null == cache) {
        return default;
      }

      return (T) cache.Get(key);
    }

    public IAsyncOperation PutAsync(string key, IEnumerable<byte> value, Args cacheArgs) {
      return null;
    }

    public IAsyncOperation GetAsync(string key, Args cacheArgs) {
      return null;
    }

    public void Clear() {

    }
  }
}
