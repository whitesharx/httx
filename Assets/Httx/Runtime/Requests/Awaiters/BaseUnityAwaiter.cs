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
using System.Threading;
using Httx.Caches.Disk;
using Httx.Requests.Awaiters.Async;
using Httx.Requests.Decorators;
using Httx.Requests.Exceptions;
using Httx.Requests.Extensions;
using Httx.Utils;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public abstract class BaseUnityAwaiter<TResult> : IAwaiter<TResult> {
    private readonly IRequest inputRequest;
    private IAsyncOperation operation;
    private Action continuationAction;
    private CancellationToken cancelToken;
    private bool isAwaken;
    private string requestId;
    private bool isMemoryCacheEnabled;
    private int cacheItemMaxAge;
    private string cacheKey;
    private object cachedResult;

    public BaseUnityAwaiter(IRequest request) {
      inputRequest = request;
    }

    public void OnCompleted(Action continuation) {
      if (cancelToken.IsCancellationRequested) {
        TryDispose();
        throw new OperationCanceledException();
      }

      continuationAction = continuation;
      operation.OnComplete += continuationAction;
    }

    public bool IsCompleted {
      get {
        if (default != cancelToken && cancelToken.IsCancellationRequested) {
          throw new OperationCanceledException();
        }

        if (default != cachedResult) {
          return true;
        }

        if (isAwaken) {
          return operation.Done;
        }

        Context = Context.Instance;

        if (null == Context) {
          const string msg = "request context is not set, instantiate default context before usage";
          throw new InvalidOperationException(msg);
        }

        Log(inputRequest.AsJson());

        requestId = Guid.NewGuid().ToString();
        cancelToken = inputRequest.FetchCancelToken();
        isMemoryCacheEnabled = inputRequest.IsMemoryCacheEnabled();

        if (isMemoryCacheEnabled && null == Context.MemoryCache) {
          throw new InvalidOperationException("memory cache operation requested, but cache is not initialized");
        }

        if (isMemoryCacheEnabled) {
          cacheItemMaxAge = inputRequest.FetchCacheItemMaxAge();
          cacheKey = Crypto.Sha256(inputRequest.ResolveUrl());
          cachedResult = Context.MemoryCache.Get(cacheKey);

          if (default != cachedResult) {
            return true;
          }
        }

        if (cancelToken.IsCancellationRequested) {
          throw new OperationCanceledException();
        }

        operation = Awake(inputRequest);
        isAwaken = true;

        return operation.Done;
      }
    }

    public TResult GetResult() {
      if (cancelToken.IsCancellationRequested) {
        TryDispose();
        throw new OperationCanceledException();
      }

      if (default != cachedResult) {
        return (TResult)cachedResult;
      }

      var requestOpt = operation.UnsafeResult<UnityWebRequest>();
      var e = requestOpt?.AsException();

      inputRequest.CallOnOnResponseReceived(requestOpt);

      try {
        if (null != e) {
          Log(e.AsJson());
          throw e;
        }

        if (null == continuationAction) {
          if (null != requestOpt) {
            Context.Logger.Log(requestOpt.AsJson());
          }

          return MapInternal(inputRequest, operation);
        }

        operation.OnComplete -= continuationAction;
        continuationAction = null;

        return MapInternal(inputRequest, operation);
      } finally {
        TryDispose();
      }
    }

    public abstract IAsyncOperation Awake(IRequest request);
    public abstract TResult Map(IRequest request, IAsyncOperation completeOperation);

    protected UnityWebRequestAsyncOperation Send(UnityWebRequest request,
        IEnumerable<KeyValuePair<string, object>> headers) {
      var hx = headers?.ToList() ?? new List<KeyValuePair<string, object>>();
      var pRef = hx.FetchHeader<WeakReference<IProgress<float>>>(InternalHeaders.ProgressObject);

      inputRequest.CallOnBeforeRequestSent(request);

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

    protected Context Context { get; private set; }

    private IAsyncOperation CreateCacheOperation(UnityWebRequest request,
        IReadOnlyCollection<KeyValuePair<string, object>> headers) {
      var conditionObject = headers.FetchHeader<Condition>(InternalHeaders.ConditionObject);
      var hasCondition = !string.IsNullOrEmpty(conditionObject?.Value);

      var url = request.url;
      var cache = Context.DiskCache;

      if (null == cache) {
        throw new InvalidOperationException("disk cache operation requested, but cache is not initialized");
      }

      Editor cacheValueEditor = null;

      // XXX: Hit cache and return local data file url.
      var tryHitCache = new Func<IAsyncOperation, IAsyncOperation>(_ => cache.Get(url));

      // XXX: Try send network request.
      var netRequest = new Func<IAsyncOperation, IAsyncOperation>(previous => {
        // XXX: If condition enabled and set, always skip initial cache hit.
        var cachedFileUrl = hasCondition ? null : previous.UnsafeResult<string>();

        // XXX: No cache entry found or fresh request.
        if (string.IsNullOrEmpty(cachedFileUrl)) {
          return new UnityAsyncOperation(() => Send(request, headers));
        }

        // XXX: Cache entry found. Lock entry and fetch result from local file ('file://' url).
        request.url = cachedFileUrl;

        // @formatter:off
        return new AsyncOperationQueue(_ =>
            cache.Lock(cachedFileUrl),
            pLock => {
              cacheValueEditor = pLock.UnsafeResult<Editor>();
              return new UnityAsyncOperation(() => Send(request, headers));
            });
        // @formatter:on
      });

      // XXX: Put raw request data to cache.
      var tryPutCache = new Func<IAsyncOperation, IAsyncOperation>(previous => {
        var resultRequest = previous.UnsafeResult<UnityWebRequest>();

        if (null == resultRequest
            || resultRequest.LocalOrCached()
            || resultRequest.Redirect()
            || !resultRequest.Success()) {
          return previous;
        }

        // @formatter:off
        return new AsyncOperationQueue(
            _ => cache.Put(resultRequest),
            _ => cache.Unlock(cacheValueEditor));
        // @formatter:on
      });

      return new AsyncOperationQueue(tryHitCache, netRequest, tryPutCache);
    }

    private TResult MapInternal(IRequest request, IAsyncOperation completeOperation) {
      var conditionObject = request.FetchConditionObject();

      if (null != conditionObject) {
        var requestImpl = completeOperation.UnsafeResult<UnityWebRequest>();

        if (null != requestImpl) {
          if (requestImpl.NotModified()) {
            var url = requestImpl.url;
            var headers = requestImpl.GetResponseHeaders();

            throw new NotModifiedException(url, "content with given tag was not modified.", headers);
          }

          var eTag = requestImpl.GetResponseHeader("ETag");

          if (eTag != conditionObject.Value) {
            conditionObject.OnValueChanged?.Invoke(eTag);
          }
        }
      }

      var result = Map(request, completeOperation);

      if (!isMemoryCacheEnabled || string.IsNullOrEmpty(cacheKey)) {
        return result;
      }

      if (0 != cacheItemMaxAge) {
        Context.MemoryCache.Put(cacheKey, result, cacheItemMaxAge);
      } else {
        Context.MemoryCache.Put(cacheKey, result);
      }

      return result;
    }

    private void Log(object message) {
      var logger = Context?.Logger;
      logger?.Log(message);
    }

    private void TryDispose() {
      if (!string.IsNullOrEmpty(requestId)) {
        UnityWebRequestReporter.RemoveReporterRef(requestId);
      }

      if (operation.Result is UnityWebRequest requestOpt) {
        requestOpt.Dispose();
      }

      operation = null;
    }
  }
}
