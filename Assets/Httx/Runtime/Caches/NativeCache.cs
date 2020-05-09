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

using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Httx.Sources.Caches {
  public class NativeCacheArgs {
    public NativeCacheArgs(string path, uint version, int maxSize) {
      Path = path;
      Version = version;
      MaxSize = maxSize;
    }

    public string Path { get; }
    public uint Version { get; }
    public int MaxSize { get; }
  }

  public class NativeCache : IDisposable {
    private readonly string path;
    private readonly int maxSize;
    private Cache currentCache;

    public NativeCache(NativeCacheArgs args) {
      path = args.Path;
      Version = args.Version;
      maxSize = args.MaxSize;
    }

    public async void Initialize(Action onComplete) {
      await Task.Run(() => {
        if (!Directory.Exists(path)) {
          Directory.CreateDirectory(path);
        }
      });

      var cacheOpt = Caching.GetCacheByPath(path);

      currentCache = default != cacheOpt ? cacheOpt : Caching.AddCache(path);
      currentCache.maximumAvailableStorageSpace = maxSize;

      if (!currentCache.valid) {
        throw new InvalidOperationException($"cache {currentCache} is not valid");
      }

      Caching.currentCacheForWriting = currentCache;

      onComplete();
    }

    public async void Delete(Action onComplete) {
      await Task.Run(() => {
        if (!Directory.Exists(path)) {
          Directory.Delete(path, true);
        }
      });

      currentCache.ClearCache();
      onComplete();
    }

    public void Dispose() {
      Caching.RemoveCache(currentCache);
    }

    public uint Version { get; }
  }
}
