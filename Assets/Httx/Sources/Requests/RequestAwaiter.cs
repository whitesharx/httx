// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests {
  public class RequestAwaiter<T> : INotifyCompletion {
    private readonly string url;
    private readonly UnityWebRequestAsyncOperation operation;
    private Action<AsyncOperation> continuationAction;

    public RequestAwaiter(string url) {
      this.url = url;

      var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET) {
        downloadHandler = new DownloadHandlerBuffer()
      };

      operation = request.SendWebRequest();
      // operation.completed += OnCompleted()
    }

    public bool IsCompleted {
      get {
        Debug.Log("RequestAwaiter:IsCompleted");
        return operation.isDone;
      }
    }

    public T GetResult() {
      Debug.Log($"RequestAwaiter:GetResult:{operation.webRequest.error}");

      if (!string.IsNullOrEmpty(operation.webRequest.error)) {
        throw new Exception(operation.webRequest.error);
      }

      var request = operation.webRequest;
      var bytes = request.downloadHandler.data;

      Debug.Log($"RequestAwaiter:Bytes:{bytes?.Length}");

      if (null != continuationAction) {
        operation.completed -= continuationAction;
        // operation = null;
        continuationAction = null;
      }

      return default;
    }

    public void OnCompleted(Action continuation) {
      Debug.Log("RequestAwaiter:OnCompleted");

      continuationAction = asyncOperation => continuation();
      operation.completed += continuationAction;
    }
  }
}
