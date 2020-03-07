// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System;
using System.Linq;
using Httx.Requests.Awaiters.Async;
using Httx.Requests.Extensions;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public class UnityWebRequestAwaiter<TResult> : BaseUnityAwaiter<TResult> {
    private bool isResponseCodeOnly;

    public UnityWebRequestAwaiter(IRequest request) : base(request) { }

    public override IAsyncOperation Awake(IRequest request) {
      var verb = request.ResolveVerb();
      var url = request.ResolveUrl();
      var headers = request.ResolveHeaders()?.ToList();
      var body = request.ResolveBody()?.ToArray();

      var requestImpl = new UnityWebRequest(url, verb) {
        downloadHandler = new DownloadHandlerBuffer()
      };

      if (null != body && 0 != body.Length) {
        requestImpl.uploadHandler = new UploadHandlerRaw(body);
      }

      isResponseCodeOnly = headers.FetchHeader<bool>(InternalHeaders.ResponseCodeOnly);

      var cache = Context.DiskCache;
      var isDiskCacheEnabled = headers.FetchHeader<bool>(InternalHeaders.DiskCacheEnabled);

      Context.Logger.Log($"is-cache-enabled: {isDiskCacheEnabled}");

      if (!isDiskCacheEnabled) {
        return new UnityAsyncOperation(() => Send(requestImpl, headers));
      }

      // var cacheHitOp
      var tryCacheOp = new Func<IAsyncOperation, IAsyncOperation>(_ => {
        Debug.Log($"try-cache: {url}");
        return cache.Get(url);
      });

      // var requestOp
      var netRequestOp = new Func<IAsyncOperation, IAsyncOperation>(previous => {
        var cachedFileUrl = previous.Result as string;

        Debug.Log($"net-request: {cachedFileUrl}");

        if (!string.IsNullOrEmpty(cachedFileUrl)) {
          // XXX: Put to headers origin url?
          requestImpl.url = cachedFileUrl;
        }

        return new UnityAsyncOperation(() => Send(requestImpl, headers));
      });

      // var cacheSaveOp
      var persistCacheOp = new Func<IAsyncOperation, IAsyncOperation>(previous => {
        var resultRequest = (previous as UnityAsyncOperation)?.Request;

        Debug.Log($"persist-cache: {resultRequest?.LocalOrCached()}");

        if (resultRequest.LocalOrCached()) {
          return previous; // DoneAsyncOperation?
        }

        return cache.Put(resultRequest);
      });

      return new AsyncOperationQueue(tryCacheOp, netRequestOp, persistCacheOp);
    }

    public override TResult Map(IRequest request, IAsyncOperation completeOperation) {
      var requestImpl = (UnityWebRequest) completeOperation.Result;

      if (isResponseCodeOnly) {
        var result = (int) requestImpl.responseCode;
        return (TResult) (object)result;
      }

      var bytes = requestImpl.downloadHandler.data;

      if (null != bytes && 0 != bytes.Length) {
        return request.ResolveResultMapper<TResult>().FromResult(bytes);
      }

      var headers = requestImpl.GetResponseHeaders();
      return request.ResolveResultMapper<TResult>().FromResult(headers);
    }
  }
}
