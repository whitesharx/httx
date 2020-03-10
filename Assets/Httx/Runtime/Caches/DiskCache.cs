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
using Httx.Requests.Awaiters.Async;
using Httx.Utils;
using UnityEngine.Networking;

namespace Httx.Caches {
  public class DiskCacheArgs {
    public DiskCacheArgs(string path, int version, int maxSize, int collectFrequency) {
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

  public class DiskCache : IDisposable {
    private readonly string path;
    private readonly int version;
    private readonly int maxSize;
    private readonly int collectFrequency;

    private int opsCount;
    private DiskLruCache cacheImpl;

    public DiskCache(DiskCacheArgs args) {
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
        operation.Result = completeRequest;
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

    public IAsyncOperation Lock(string requestUrl) {
      var operation = new MutableAsyncOperation();

      LockImpl(requestUrl, editor => {
        operation.Progress = 1.0f;
        operation.Result = editor;
        operation.Done = true;

        operation.InvokeSafe();
      });

      return operation;
    }

    public IAsyncOperation Unlock(Editor editor) {
      var operation = new MutableAsyncOperation();

      UnlockImpl(editor, () => {
        operation.Progress = 1.0f;
        operation.Done = true;

        operation.InvokeSafe();
      });

      return operation;
    }

    public async void Delete(Action onComplete) {
      await Task.Run(() => {
        cacheImpl.Delete();
      });

      onComplete();
    }

    public void Dispose() => cacheImpl?.Dispose();

    private async void GetImpl(string requestUrl, Action<string> onComplete) {
      var fileUrl = await Task.Run(() => {
        var key = Crypto.Sha256(requestUrl);
        var snapshot = cacheImpl.Get(key);

        return snapshot?.UnsafeUrl;
      });

      onComplete(fileUrl);
    }

    private async void PutImpl(UnityWebRequest completeRequest, Action onComplete) {
      var key = Crypto.Sha256(completeRequest.url);
      var value = completeRequest.downloadHandler?.data;

      await Task.Run(() => {
        if (null == value || 0 == value.Length) {
          return;
        }

        var editor = cacheImpl.Edit(key);
        editor.Put(value);
        editor.Commit();
      });

      onComplete();
    }

    private async void LockImpl(string requestUrl, Action<Editor> onComplete) {
      var result = await Task.Run(() => {
        var key = Crypto.Sha256(requestUrl);
        var snapshot = cacheImpl.Get(key);

        return snapshot?.Edit();
      });

      onComplete(result);
    }

    private async void UnlockImpl(Editor editor, Action onComplete) {
      if (null == editor) {
        onComplete();
      }

      await Task.Run(() => {
        editor?.Commit();
      });

      onComplete();
    }
  }
}
