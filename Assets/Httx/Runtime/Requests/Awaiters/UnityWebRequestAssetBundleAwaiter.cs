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
using System.Linq;
using Httx.Requests.Awaiters.Async;
using Httx.Requests.Extensions;
using Httx.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public class UnityWebRequestAssetBundleAwaiter<TResult> : BaseUnityAwaiter<TResult> {
    private const string ManifestAsset = "assetbundlemanifest";
    private string currentBundleUrl;

    public UnityWebRequestAssetBundleAwaiter(IRequest request) : base(request) { }

    public override IAsyncOperation Awake(IRequest request) {
      var verb = request.ResolveVerb();
      var url = request.ResolveUrl();
      var headers = request.ResolveHeaders()?.ToList();

      currentBundleUrl = url;

      var crc = headers.FetchHeader<uint>(InternalHeaders.AssetBundleCrc);
      var version = headers.FetchHeader<uint>(InternalHeaders.AssetBundleVersion);
      var hash = headers.FetchHeader<Hash128>(InternalHeaders.AssetBundleHash);

      var isCacheEnabled = headers.FetchHeader<bool>(InternalHeaders.NativeCacheEnabled);
      var cache = Context.NativeCache;
      var cacheVersion = 0 != version ? version : cache.Version;

      var handler = new DownloadHandlerAssetBundle(url, crc);

      if (0 != cacheVersion && isCacheEnabled) {
        handler = new DownloadHandlerAssetBundle(url, cacheVersion, crc);
      }

      if (default != hash) {
        handler = new DownloadHandlerAssetBundle(url, hash, crc);
      }

      var requestImpl = new UnityWebRequest(url, verb) { downloadHandler = handler };
      var isManifestRequest = headers.FetchHeader<bool>(InternalHeaders.AssetBundleLoadManifest);

      var requestOperation = new Func<IAsyncOperation, IAsyncOperation>(_ => {
        AssetBundlePool.Instance.Release(url, false, Context);
        return new UnityAsyncOperation(() => Send(requestImpl, headers));
      });

      if (!isManifestRequest) {
        return requestOperation(null);
      }

      var manifestOperation = new Func<IAsyncOperation, IAsyncOperation>(previous => {
        var bundle = MapAssetBundle(url, previous);
        return new UnityAsyncOperation(() => bundle.LoadAssetAsync<AssetBundleManifest>(ManifestAsset));
      });

      return new AsyncOperationQueue(requestOperation, manifestOperation);
    }

    public override TResult Map(IRequest request, IAsyncOperation completeOperation) {
      if (typeof(TResult) == typeof(AssetBundle)) {
        return (TResult)(object)MapAssetBundle(currentBundleUrl, completeOperation);
      }

      if (typeof(TResult) == typeof(AssetBundleManifest)) {
        return (TResult)(object)MapAssetBundleManifest(completeOperation);
      }

      return default;
    }

    private AssetBundle MapAssetBundle(string url, IAsyncOperation operation) {
      var webRequest = (UnityWebRequest)operation.Result;
      var handler = (DownloadHandlerAssetBundle)webRequest.downloadHandler;
      var bundle = handler.assetBundle;

      if (null == bundle) {
        return bundle;
      }

      var pool = AssetBundlePool.Instance;
      pool.Retain(url, bundle.name, Context);

      return bundle;
    }

    private AssetBundleManifest MapAssetBundleManifest(IAsyncOperation operation) {
      return operation.Result as AssetBundleManifest;
    }
  }
}
