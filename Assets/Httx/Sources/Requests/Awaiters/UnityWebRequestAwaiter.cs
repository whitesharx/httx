// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System;
using Httx.Requests.Awaiters;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests {
  public class UnityWebRequestAwaiter<T> : IAwaiter<T> {
    private IRequest<T> request;
    private UnityWebRequestAsyncOperation operation;
    private Action<AsyncOperation> continuationAction;

    public void Awake(IRequest<T> request) {
      Debug.Log("RequestAwaiter:Apply");

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

    public bool IsCompleted {
      get {
        Debug.Log("RequestAwaiter:IsCompleted");
        // return operation.isDone;
        return false;
      }
    }

    public T GetResult() {
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

    public void OnCompleted(Action continuation) {
      Debug.Log("RequestAwaiter:OnCompleted");

      // continuationAction = asyncOperation => continuation();
      // operation.completed += continuationAction;
    }
  }
}
