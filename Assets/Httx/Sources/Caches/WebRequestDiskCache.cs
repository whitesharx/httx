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
using System.Threading.Tasks;
using Httx.Caches.Disk;
using Httx.Requests.Awaiters;
using UnityEngine.Networking;

namespace Httx.Httx.Sources.Caches {
  public class WebRequestCacheArgs { // todo: Logger
    public WebRequestCacheArgs(string path, int version, int maxSize, int collectFrequency) {
      Path = path;
      Version = version;
      MaxSize = maxSize;
      CollectFrequency = collectFrequency;
    }

    public string Path { get; }
    public int Version { get; }
    public int MaxSize { get; }
    public int CollectFrequency { get; }
  }

  public class WebRequestDiskCache : IDisposable {
    private readonly string path;
    private readonly int version;
    private readonly int maxSize;
    private readonly int collectFrequency;

    private int opsCount;
    private DiskLruCache cacheImpl;

    public WebRequestDiskCache(WebRequestCacheArgs args) {
      path = args.Path;
      version = args.Version;
      maxSize = args.MaxSize;
      collectFrequency = args.CollectFrequency;
    }

    public async void Initialize(Action<Exception> onComplete) {
      Exception exception = null;

      try {
        await Task.Run(() => {
          cacheImpl = DiskLruCache.Open(path, version, maxSize, collectFrequency);
        });
      } catch (Exception e) {
        exception = e;
      } finally {
        onComplete(exception);
      }
    }

    public async void Put(UnityWebRequest request, Action<Exception> onComplete) {
      Exception exception = null;

      try {
        await Task.Run(() => {
          // TODO: Serialize


          // var editor = cacheImpl.Edit(Crypto.Sha256(key));
          // editor.Put(value);
          // editor.Commit();
        });
      } catch (Exception e) {
        exception = e;
      } finally {
        onComplete(exception);
      }
    }

    public IAsyncOperation Put(UnityWebRequest request) {
      var operation = new MutableAsyncOperation();

      Put(request, result => {
        operation.Progress = 1.0f;
        operation.Result = result;
        operation.Done = true;

        operation.InvokeSafe();
      });

      return operation;
    }


    public void GetFileUrl(string key, Action<string> onComplete) {

    }

    public void Lock(string key, Action<bool> onComplete) {

    }

    public void Unlock(string key, Action<bool> onComplete) {

    }

    public void Clear(Action<Exception> onComplete) {

    }

    public void Dispose() => cacheImpl?.Dispose();
  }
}
