// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;
using System.Linq;
using Httx.Requests.Extensions;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public class UnityWebRequestAwaiter<TResult> : BaseUnityAwaiter<TResult> {
    private bool isResponseCodeOnly;

    public UnityWebRequestAwaiter(IRequest request) : base(request) { }

    public override IAsyncOperation Awake(IRequest request) {
      var verb = request.ResolveVerb();
      var url = request.ResolveUrl();
      var headers = request.ResolveHeaders()?.ToList();
      var body = request.ResolveBody()?.ToArray();

      var requestImpl = new UnityWebRequest(url, verb) {
        downloadHandler = new DownloadHandlerBuffer()
      };

      if (null != body && 0 != body.Length) {
        requestImpl.uploadHandler = new UploadHandlerRaw(body);
      }

      isResponseCodeOnly = ResolveResponseCodeOnly(headers);

      return new UnityAsyncOperation(() => Send(requestImpl, headers));
    }

    public override TResult Map(IRequest request, IAsyncOperation operation) {
      var requestImpl = (UnityWebRequest) operation.Result;

      if (isResponseCodeOnly) {
        var result = (int) requestImpl.responseCode;
        return (TResult) (object)result;
      }

      var bytes = requestImpl.downloadHandler.data;

      if (null != bytes && 0 != bytes.Length) {
        return request.ResolveResultMapper<TResult>().FromResult(bytes);
      }

      var headers = requestImpl.GetResponseHeaders();
      return request.ResolveResultMapper<TResult>().FromResult(headers);
    }

    private static bool ResolveResponseCodeOnly(IEnumerable<KeyValuePair<string, object>> headers) {
      var value = headers?.FirstOrDefault(h => h.Key == InternalHeaders.ResponseCodeOnly).Value;
      return value as bool? ?? false;
    }
  }
}
