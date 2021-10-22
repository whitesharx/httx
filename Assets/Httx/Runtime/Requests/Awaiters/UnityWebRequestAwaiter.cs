// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;
using System.Linq;
using Httx.Requests.Awaiters.Async;
using Httx.Requests.Extensions;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public class UnityWebRequestAwaiter<TResult> : BaseUnityAwaiter<TResult> {
    private readonly IReadOnlyCollection<KeyValuePair<string, object>> resolvedHeaders;
    private readonly byte[] resolvedBody;
    private readonly bool isResponseCodeOnly;

    public UnityWebRequestAwaiter(IRequest request) : base(request) {
      resolvedHeaders = request.ResolveHeaders()?.ToList();
      resolvedBody = request.ResolveBody()?.ToArray();
      isResponseCodeOnly = resolvedHeaders.FetchHeader<bool>(InternalHeaders.ResponseCodeOnly);
    }

    public override UnityWebRequest Copy(UnityWebRequest request) {
      var requestImpl = new UnityWebRequest(request.url, request.method) {
          downloadHandler = new DownloadHandlerBuffer()
      };

      if (null != resolvedBody && 0 != resolvedBody.Length) {
        requestImpl.uploadHandler = new UploadHandlerRaw(resolvedBody);
      }

      return requestImpl;
    }

    public override IAsyncOperation Awake(IRequest request) {
      var verb = request.ResolveVerb();
      var url = request.ResolveUrl();

      var requestImpl = new UnityWebRequest(url, verb) {
          downloadHandler = new DownloadHandlerBuffer()
      };

      if (null != resolvedBody && 0 != resolvedBody.Length) {
        requestImpl.uploadHandler = new UploadHandlerRaw(resolvedBody);
      }

      return SendCached(requestImpl, resolvedHeaders);
    }

    public override TResult Map(IRequest request, IAsyncOperation completeOperation) {
      var requestImpl = completeOperation.SafeResult<UnityWebRequest>();

      if (isResponseCodeOnly) {
        var result = (int)requestImpl.responseCode;
        return (TResult)(object)result;
      }

      var bytes = requestImpl.downloadHandler.data;

      if (null != bytes && 0 != bytes.Length) {
        return request.ResolveResultMapper<TResult>(Context).FromResult(bytes);
      }

      var headers = requestImpl.GetResponseHeaders();
      return request.ResolveResultMapper<TResult>(Context).FromResult(headers);
    }
  }
}
