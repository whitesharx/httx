// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  // public class UnityWebRequestAwaiter : BaseAwaiter<byte[]> {
  //   private IRequest inputRequest;
  //   private UnityWebRequestAsyncOperation operation;
  //   private Action<AsyncOperation> continuationAction;
  //
  //   [UsedImplicitly]
  //   public UnityWebRequestAwaiter(IRequest request) {
  //     inputRequest = request;
  //
  //     var verb = request.Verb;
  //     var url = request.Url;
  //     var headers = request.Headers;
  //     var body = request.Body?.ToArray();
  //
  //     var requestImpl = new UnityWebRequest(url, verb) {
  //       downloadHandler = new DownloadHandlerBuffer()
  //     };
  //
  //     if (null != headers && 0 != headers.Count) {
  //       foreach (var p in headers) {
  //         requestImpl.SetRequestHeader(p.Key, p.Value?.ToString());
  //       }
  //     }
  //
  //     if (null != body && 0 != body.Length) {
  //       requestImpl.uploadHandler = new UploadHandlerRaw(body);
  //     }
  //
  //     operation = requestImpl.SendWebRequest();
  //   }
  //
  //   public override bool IsCompleted => operation.isDone;
  //
  //   public override void OnCompleted(Action continuation) {
  //     continuationAction = asyncOperation => continuation();
  //     operation.completed += continuationAction;
  //   }
  //
  //   public override byte[] GetResult() {
  //     Debug.Log($"UnityWebRequestAwaiter:GetResult:{operation.webRequest.error}");
  //
  //     if (!string.IsNullOrEmpty(operation.webRequest.error)) {
  //       throw new Exception(operation.webRequest.error);
  //     }
  //
  //     var requestImpl = operation.webRequest;
  //     var bytes = requestImpl.downloadHandler.data;
  //
  //     Debug.Log($"RequestAwaiter:Bytes:{bytes?.Length}");
  //
  //     if (null == continuationAction) {
  //       return bytes;
  //     }
  //
  //     operation.completed -= continuationAction;
  //     operation = null;
  //     continuationAction = null;
  //
  //     return bytes;
  //   }
  // }
}
