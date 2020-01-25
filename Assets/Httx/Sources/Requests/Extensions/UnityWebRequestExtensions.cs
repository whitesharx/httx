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
using Httx.Externals.MiniJSON;
using Httx.Requests.Exceptions;
using UnityEngine.Networking;

namespace Httx.Requests.Extensions {
  public static class UnityWebRequestExtensions {
    public static HttpException AsException(this UnityWebRequest request) {
      if (!request.isHttpError && !request.isNetworkError) {
        return null;
      }

      var code = request.responseCode;
      var msg = request.error;
      var headers = request.GetResponseHeaders();
      var body = request.downloadHandler?.data ?? new byte[] { };

      return new HttpException(code, msg, headers, body);
    }

    public static string AsJson(this UnityWebRequestAsyncOperation operation, int bodySize = 256) {
      var jsonObject = new Dictionary<string, object>();

      var code = operation.webRequest.responseCode;
      var error = operation.webRequest.error;
      var headers = operation.webRequest.GetResponseHeaders();
      var handler = operation.webRequest.downloadHandler;

      if (0 != code) {
        jsonObject["code"] = code;
      }

      if (!string.IsNullOrEmpty(error)) {
        jsonObject["error"] = error;
      }

      if (null != headers && 0 != headers.Count) {
        jsonObject["headers"] = headers;
      }

      switch (handler) {
        case null:
          return Json.Serialize(jsonObject);
        case DownloadHandlerBuffer buffer when !string.IsNullOrEmpty(buffer.text): {
          var postfix = buffer.text.Length > bodySize ? "..." : string.Empty;
          jsonObject["body"] = $"{buffer.text.Take(bodySize)}{postfix}";
          break;
        }
        case DownloadHandlerAssetBundle bundle when null != bundle.assetBundle:
          jsonObject["body"] = $"AssetBundle({bundle.assetBundle.name}B)";
          break;
        case DownloadHandlerFile _:
          jsonObject["body"] = "File()";
          break;
        case DownloadHandlerTexture texture when null != texture.texture: {
          var w = texture.texture.width;
          var h = texture.texture.height;

          jsonObject["body"] = $"Texture({w}x{h})";
          break;
        }
        case DownloadHandlerAudioClip clip when null != clip.audioClip:
          jsonObject["body"] = $"AudioClip({clip.audioClip.length})";
          break;
      }

      return Json.Serialize(jsonObject);
    }

    public static UnityWebRequest AppendHeaders(this UnityWebRequest request, List<KeyValuePair<string, object>> headers) {
      if (null == headers || 0 == headers.Count) {
        return request;
      }

      foreach (var p in headers.Where(p => !p.IsInternalHeader())) {
        request.SetRequestHeader(p.Key, p.Value?.ToString());
      }

      return request;
    }
  }
}
