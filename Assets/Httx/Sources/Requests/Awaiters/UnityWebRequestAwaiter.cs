// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Linq;
using Httx.Requests.Extensions;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public class UnityWebRequestAwaiter<TResult> : BaseUnityAwaiter<TResult> {
    public UnityWebRequestAwaiter(IRequest request) : base(request) { }

    public override UnityWebRequestAsyncOperation Awake(IRequest request) {
      var verb = request.ResolveVerb();
      var url = request.ResolveUrl();
      var headers = request.ResolveHeaders()?.ToList();
      var body = request.ResolveBody()?.ToArray();

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

      return requestImpl.SendWebRequest();
    }

    public override TResult OnResult(IRequest request, UnityWebRequestAsyncOperation operation) {
      var requestImpl = operation.webRequest;
      var bytes = requestImpl.downloadHandler.data;

      return request.ResolveResultMapper<TResult>().FromResult(bytes);
    }
  }
}
