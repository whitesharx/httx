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
using System.Collections.Generic;
using System.Linq;
using Httx.Caches.Disk;
using Httx.Requests.Awaiters.Async;
using Httx.Requests.Exceptions;
using Httx.Requests.Extensions;
using Httx.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public abstract class BaseUnityAwaiter<TResult> : IAwaiter<TResult> {
    private readonly IRequest inputRequest;
    private IAsyncOperation operation;
    private Action continuationAction;
    private bool isAwaken;
    private string requestId;

    public BaseUnityAwaiter(IRequest request) {
      inputRequest = request;
    }

    public void OnCompleted(Action continuation) {
      continuationAction = continuation;
      operation.OnComplete += continuationAction;
    }

    public bool IsCompleted {
      get {
        if (isAwaken) {
          return operation.Done;
        }

        Context = Context.Instance;

        if (null == Context) {
          const string msg = "request context is not set, instantiate default context before usage";
          throw new InvalidOperationException(msg);
        }

        Context.Logger.Log(inputRequest.AsJson());

        requestId = Guid.NewGuid().ToString();
        operation = Awake(inputRequest);
        isAwaken = true;

        return operation.Done;
      }
    }

    public TResult GetResult() {
      var requestOpt = operation.Result as UnityWebRequest;
      var e = requestOpt?.AsException();

      if (null != e) {
        Context.Logger.Log(e.AsJson());
        throw e;
      }

      if (null == continuationAction) {
        if (null != requestOpt) {
          Context.Logger.Log(requestOpt.AsJson());
        }

        return Map(inputRequest, operation);
      }

      operation.OnComplete -= continuationAction;
      continuationAction = null;

      try {
        UnityWebRequestReporter.RemoveReporterRef(requestId);
        return Map(inputRequest, operation);
      } finally {
        requestOpt?.Dispose();
        operation = null;
      }
    }

    public abstract IAsyncOperation Awake(IRequest request);
    public abstract TResult Map(IRequest request, IAsyncOperation completeOperation);

    protected UnityWebRequestAsyncOperation Send(UnityWebRequest request,
      IEnumerable<KeyValuePair<string, object>> headers) {

      var hx = headers?.ToList() ?? new List<KeyValuePair<string, object>>();
      var pRef = hx.FetchHeader<WeakReference<IProgress<float>>>(InternalHeaders.ProgressObject);

      if (null == pRef) {
        return request.AppendHeaders(hx).SendWebRequest();
      }

      var wrapper = new UnityWebRequestReporter.ReporterWrapper(pRef, request);
      UnityWebRequestReporter.AddReporterRef(requestId, wrapper);

      return request.SendWebRequest();
    }

    protected IAsyncOperation SendCached(UnityWebRequest request,
      IEnumerable<KeyValuePair<string, object>> headers) {

      var hx = headers?.ToList() ?? new List<KeyValuePair<string, object>>();
      var isDiskCacheEnabled = hx.FetchHeader<bool>(InternalHeaders.DiskCacheEnabled);

      return !isDiskCacheEnabled
        ? new UnityAsyncOperation(() => Send(request, hx))
        : CreateCacheOperation(request, hx);
    }

    private IAsyncOperation CreateCacheOperation(UnityWebRequest request,
      IEnumerable<KeyValuePair<string, object>> headers) {

      var url = request.url;
      var cache = Context.DiskCache;

      Editor unsafeEditor = null;

      var tryCache = new Func<IAsyncOperation, IAsyncOperation>(_ => {
        Debug.Log($"try-cache: {url}");
        return cache.Get(url);
      });

      var netRequest = new Func<IAsyncOperation, IAsyncOperation>(previous => {
        var cachedFileUrl = previous.Result as string;

        if (string.IsNullOrEmpty(cachedFileUrl)) {
          return new UnityAsyncOperation(() => Send(request, headers));
        }

        Debug.Log($"net-request: {cachedFileUrl}");

        request.url = cachedFileUrl;

        return new AsyncOperationQueue(
          _ => cache.Lock(cachedFileUrl),
          pLock => {
            unsafeEditor = pLock.Result as Editor;
            return new UnityAsyncOperation(() => Send(request, headers));
          });
      });

      var putCache = new Func<IAsyncOperation, IAsyncOperation>(previous => {
        var resultRequest = previous?.Result as UnityWebRequest;

        if (resultRequest.LocalOrCached()) {
          return previous;
        }

        return new AsyncOperationQueue(
          _ => cache.Put(resultRequest),
          _ => cache.Unlock(unsafeEditor));
      });

      return new AsyncOperationQueue(tryCache, netRequest, putCache);
    }

    protected Context Context { get; private set; }
  }
}
