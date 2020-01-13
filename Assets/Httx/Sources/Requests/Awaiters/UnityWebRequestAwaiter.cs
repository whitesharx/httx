// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System;
using System.Linq;
using Httx.Requests.Awaiters;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests {
  public class UnityWebRequestAwaiter<T> : BaseAwaiter<T> {
    private IRequest<T> inputRequest;
    private UnityWebRequestAsyncOperation operation;
    private Action<AsyncOperation> continuationAction;

    [UsedImplicitly]
    public UnityWebRequestAwaiter(IRequest<T> request) {
      inputRequest = request;

      var verb = request.Verb;
      var url = request.Url;
      var headers = request.Headers;
      var body = request.Body?.ToArray();

      var requestImpl = new UnityWebRequest(url, verb) {
        downloadHandler = new DownloadHandlerBuffer()
      };

      if (null != headers && 0 != headers.Count) {
        foreach (var p in headers) {
          requestImpl.SetRequestHeader(p.Key, p.Value?.ToString());
        }
      }

      if (null != body && 0 != body.Length) {
        requestImpl.uploadHandler = new UploadHandlerRaw(body);
      }

      operation = requestImpl.SendWebRequest();
    }

    public override bool IsCompleted => operation.isDone;

    public override void OnCompleted(Action continuation) {
      continuationAction = asyncOperation => continuation();
      operation.completed += continuationAction;
    }

    public override T GetResult() {
      Debug.Log($"UnityWebRequestAwaiter:GetResult:{operation.webRequest.error}");

      if (!string.IsNullOrEmpty(operation.webRequest.error)) {
        throw new Exception(operation.webRequest.error);
      }

      var requestImpl = operation.webRequest;
      var bytes = requestImpl.downloadHandler.data;

      Debug.Log($"RequestAwaiter:Bytes:{bytes?.Length}");

      if (null != continuationAction) {
        operation.completed -= continuationAction;
        operation = null;
        continuationAction = null;
      }

      return default;
    }
  }
}
