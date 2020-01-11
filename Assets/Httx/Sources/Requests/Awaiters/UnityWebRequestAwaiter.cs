// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System;
using Httx.Requests.Awaiters;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests {
  public class UnityWebRequestAwaiter<T> : BaseAwaiter<T> {
    private IRequest<T> request;
    private UnityWebRequestAsyncOperation operation;
    private Action<AsyncOperation> continuationAction;

    [UsedImplicitly]
    public UnityWebRequestAwaiter(IRequest<T> request) {
      if (null != request) {
        Debug.Log($"Verb: {request.Verb} Url: {request.Url}");

        var headers = request.Headers;

        if (null != headers && 0 != headers.Count) {
          foreach (var p in headers) {
            Debug.Log($"Header: {p.Key}->{p.Value}");
          }
        }

        //   this.request = request;
        //
        //   Debug.Log($"url: {request.Url} verb: {request.Verb}");
        //
        //   var requestImpl = new UnityWebRequest(request.Url, request.Verb) {
        //     downloadHandler = new DownloadHandlerBuffer()
        //   };
        //
        //   operation = requestImpl.SendWebRequest();
        //   // operation.completed += OnCompleted()
      }

      Debug.Log("RequestAwaiter:Apply");
    }

    public override bool IsCompleted {
      get {
        Debug.Log("RequestAwaiter:IsCompleted");
        // return operation.isDone;
        return false;
      }
    }

    public override T GetResult() {
      // Debug.Log($"RequestAwaiter:GetResult:{operation.webRequest.error}");
      //
      // if (!string.IsNullOrEmpty(operation.webRequest.error)) {
      //   throw new Exception(operation.webRequest.error);
      // }
      //
      // var requestImpl = operation.webRequest;
      // var bytes = requestImpl.downloadHandler.data;
      //
      // Debug.Log($"RequestAwaiter:Bytes:{bytes?.Length}");
      //
      // if (null != continuationAction) {
      //   operation.completed -= continuationAction;
      //   // operation = null;
      //   continuationAction = null;
      // }

      return default;
    }

    public override void OnCompleted(Action continuation) {
      Debug.Log("RequestAwaiter:OnCompleted");

      // continuationAction = asyncOperation => continuation();
      // operation.completed += continuationAction;
    }
  }
}
