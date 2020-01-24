// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;
using System.Linq;
using Httx.Requests.Extensions;
using Httx.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public class UnityWebRequestAwaiter<TResult> : BaseUnityAwaiter<TResult> {
    private bool isResponseCodeOnly;

    public UnityWebRequestAwaiter(IRequest request) : base(request) { }

    public override UnityWebRequestAsyncOperation Awake(IRequest request) {
      var verb = request.ResolveVerb();
      var url = request.ResolveUrl();
      var headers = request.ResolveHeaders()?.ToList();
      var body = request.ResolveBody()?.ToArray();

      Debug.Log(request.AsJson());

      var requestImpl = new UnityWebRequest(url, verb) {
        downloadHandler = new DownloadHandlerBuffer()
      };

      SetRequestHeaders(requestImpl, headers);

      if (null != body && 0 != body.Length) {
        requestImpl.uploadHandler = new UploadHandlerRaw(body);
      }

      isResponseCodeOnly = ResolveResponseCodeOnly(headers);
      var pRef = ResolveProgress(headers);

      if (null == pRef) {
        return requestImpl.SendWebRequest();
      }

      var wrapper = new UnityWebRequestReporter.ReporterWrapper(pRef, requestImpl);
      UnityWebRequestReporter.AddReporterRef(RequestId, wrapper);

      return requestImpl.SendWebRequest();
    }

    public override TResult OnResult(IRequest request, UnityWebRequestAsyncOperation operation) {
      Debug.Log(operation.AsJson());
      UnityWebRequestReporter.RemoveReporterRef(RequestId);

      var requestImpl = operation.webRequest;

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
