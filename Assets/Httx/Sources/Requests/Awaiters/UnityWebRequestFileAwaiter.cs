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

using System.Collections.Generic;
using System.Linq;
using Httx.Requests.Extensions;
using UnityEngine.Networking;

namespace Httx.Requests.Awaiters {
  public class UnityWebRequestFileAwaiter : BaseUnityAwaiter<int> {
    private string inputPath;

    public UnityWebRequestFileAwaiter(IRequest request) : base(request) { }

    public override IAsyncOperation Awake(IRequest request) {
      var verb = request.ResolveVerb();
      var url = request.ResolveUrl();
      var headers = request.ResolveHeaders()?.ToList();

      var requestImpl = new UnityWebRequest(url, verb);

      // TODO: Implement

      return new UnityAsyncOperation(() => Send(requestImpl, headers));
    }

    public override int Map(IRequest request, IAsyncOperation operation) {
      var webRequest = (UnityWebRequest) operation.Result;
      return (int) webRequest.responseCode;
    }

    private static string ResolvePath(IEnumerable<KeyValuePair<string, object>> headers) {
      var value = headers?.FirstOrDefault(h => h.Key == InternalHeaders.FilePath).Value;
      return value as string;
    }

    private static bool ResolveAppend(IEnumerable<KeyValuePair<string, object>> headers) {
      var value = headers?.FirstOrDefault(h => h.Key == InternalHeaders.FileAppend).Value;
      return value as bool? ?? false;
    }

    private static bool ResolveRemoveOnAbort(IEnumerable<KeyValuePair<string, object>> headers) {
      var value = headers?.FirstOrDefault(h => h.Key == InternalHeaders.FileRemoveOnAbort).Value;
      return value as bool? ?? false;
    }
  }
}
