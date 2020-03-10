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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Httx.Externals.MiniJSON;

namespace Httx.Requests.Exceptions {
  public class HttpException : Exception {
    public HttpException(long code, string message,
      IEnumerable<KeyValuePair<string, string>> headers, IEnumerable<byte> body) : base(message) {

      Code = code;
      Headers = headers;
      Body = body;
    }

    public long Code { get; }
    public IEnumerable<KeyValuePair<string, string>> Headers { get; }
    public IEnumerable<byte> Body { get; }
  }

  public static class HttpExceptionExtensions {
    public static string AsJson(this HttpException e) {
      var result = new Dictionary<string, object>();

      result["code"] = e.Code;

      if (!string.IsNullOrEmpty(e.Message)) {
        result["message"] = e.Message;
      }

      if (null != e.Headers && 0 != e.Headers.Count()) {
        result["headers"] = e.Headers;
      }

      if (null != e.Body && 0 != e.Body.Count()) {
        result["body"] = Encoding.UTF8.GetString(e.Body.ToArray());
      }

      return Json.Serialize(result);
    }
  }
}
