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
using Httx.Requests.Exceptions;
using Httx.Requests.Extensions;
using Httx.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public abstract class BaseUnityAwaiter<TResult> : IAwaiter<TResult> {
    private readonly IRequest inputRequest;
    private UnityWebRequestAsyncOperation operation;
    private Action<AsyncOperation> continuationAction;
    private bool isAwaken;

    public BaseUnityAwaiter(IRequest request) {
      inputRequest = request;
    }

    public void OnCompleted(Action continuation) {
      continuationAction = asyncOperation => continuation();
      operation.completed += continuationAction;
    }

    public bool IsCompleted {
      get {
        if (isAwaken) {
          return operation.isDone;
        }

        Debug.Log(inputRequest.AsJson());

        RequestId = Guid.NewGuid().ToString();
        operation = Awake(inputRequest);
        isAwaken = true;

        return operation.isDone;
      }
    }

    public TResult GetResult() {
      var e = operation.webRequest.AsException();

      if (null != e) {
        Debug.Log(e.AsJson());
        throw e;
      }

      if (null == continuationAction) {
        Debug.Log(operation.AsJson());
        return OnResult(inputRequest, operation);
      }

      operation.completed -= continuationAction;
      continuationAction = null;

      try {
        UnityWebRequestReporter.RemoveReporterRef(RequestId);
        return OnResult(inputRequest, operation);
      } finally {
        operation.webRequest.Dispose();
        operation = null;
      }
    }

    protected string RequestId { get; private set; }

    protected UnityWebRequestAsyncOperation SendWithProgress(
      UnityWebRequest request, IEnumerable<KeyValuePair<string, object>> internalHeaders) {

      var pRef = ResolveProgress(internalHeaders);

      if (null == pRef) {
        return request.SendWebRequest();
      }

      var wrapper = new UnityWebRequestReporter.ReporterWrapper(pRef, request);
      UnityWebRequestReporter.AddReporterRef(RequestId, wrapper);

      return request.SendWebRequest();
    }

    protected static WeakReference<IProgress<float>> ResolveProgress(IEnumerable<KeyValuePair<string, object>> headers) {
      var pRef = headers?.FirstOrDefault(h => h.Key == InternalHeaders.ProgressObject).Value;
      return pRef as WeakReference<IProgress<float>>;
    }

    public abstract UnityWebRequestAsyncOperation Awake(IRequest request);
    public abstract TResult OnResult(IRequest request, UnityWebRequestAsyncOperation operation);
  }
}
