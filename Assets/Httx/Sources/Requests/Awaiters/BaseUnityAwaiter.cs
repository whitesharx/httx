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

        Debug.Log(inputRequest.AsJson());

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
        Debug.Log(e.AsJson());
        throw e;
      }

      if (null == continuationAction) {
        if (null != requestOpt) {
          Debug.Log(requestOpt.AsJson());
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
    public abstract TResult Map(IRequest request, IAsyncOperation operation);

    protected UnityWebRequestAsyncOperation Send(UnityWebRequest request,
      IEnumerable<KeyValuePair<string, object>> headers) {

      var hx = headers?.ToList() ?? new List<KeyValuePair<string, object>>();
      var pRef = ResolveProgress(hx);

      if (null == pRef) {
        return request.AppendHeaders(hx).SendWebRequest();
      }

      var wrapper = new UnityWebRequestReporter.ReporterWrapper(pRef, request);
      UnityWebRequestReporter.AddReporterRef(requestId, wrapper);

      return request.SendWebRequest();
    }

    private static WeakReference<IProgress<float>> ResolveProgress(IEnumerable<KeyValuePair<string, object>> headers) {
      var pRef = headers?.FirstOrDefault(h => h.Key == InternalHeaders.ProgressObject).Value;
      return pRef as WeakReference<IProgress<float>>;
    }
  }
}
