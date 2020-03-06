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
using Httx.Utils;
using UnityEngine.Networking;

namespace Httx.Httx.Sources.Caches {
  public class WebRequestCacheArgs {
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

    public async void Initialize(Action onComplete) {
      await Task.Run(() => {
        cacheImpl = DiskLruCache.Open(path, version, maxSize, collectFrequency);
      });

      onComplete();
    }

    public IAsyncOperation Put(UnityWebRequest completeRequest) {
      var operation = new MutableAsyncOperation();

      PutImpl(completeRequest, () => {
        operation.Progress = 1.0f;
        operation.Done = true;

        operation.InvokeSafe();
      });

      return operation;
    }

    public IAsyncOperation Get(string requestUrl) {
      var operation = new MutableAsyncOperation();

      GetImpl(requestUrl, (cachedFileUrl) => {
        operation.Progress = 1.0f;
        operation.Result = cachedFileUrl;
        operation.Done = true;

        operation.InvokeSafe();
      });

      return operation;
    }

    public void Lock(string key, Action<bool> onComplete) {

    }

    public void Unlock(string key, Action<bool> onComplete) {

    }

    public async void Delete(Action onComplete) {
      await Task.Run(() => {
        cacheImpl.Delete();
      });

      onComplete();
    }

    public void Dispose() => cacheImpl?.Dispose();

    private async void GetImpl(string requestUrl, Action<string> onComplete) {
      var fileUrl = await new Task<string>(() => {
        var key = Crypto.Sha256(requestUrl);
        var snapshot = cacheImpl.Get(key);

        return snapshot?.UnsafeUrl;
      });

      onComplete(fileUrl);
    }

    private async void PutImpl(UnityWebRequest completeRequest, Action onComplete) {
      await Task.Run(() => {
        var value = completeRequest.downloadHandler?.data;

        if (null == value || 0 == value.Length) {
          return;
        }

        var key = Crypto.Sha256(completeRequest.url);
        var editor = cacheImpl.Edit(Crypto.Sha256(key));
        editor.Put(value);
        editor.Commit();
      });

      onComplete();
    }
  }
}
