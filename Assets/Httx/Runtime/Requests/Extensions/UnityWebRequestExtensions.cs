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
using System.Text;
using Httx.Externals.MiniJSON;
using Httx.Requests.Exceptions;
using UnityEngine.Networking;

namespace Httx.Requests.Extensions {
  public static class UnityWebRequestExtensions {
    public static HttpException AsException(this UnityWebRequest request) {
      if (!request.isHttpError && !request.isNetworkError) {
        return null;
      }

      var url = request.url;
      var code = request.responseCode;
      var msg = request.error;
      var headers = request.GetResponseHeaders();
      var body = Encoding.UTF8.GetBytes(request.downloadHandler.AsStringBody());

      return new HttpException(url, code, msg, headers, body);
    }

    public static string AsJson(this UnityWebRequest request, int bodySize = 256) {
      var jsonObject = new Dictionary<string, object>();

      var code = request.responseCode;
      var url = request.url;
      var error = request.error;
      var headers = request.GetResponseHeaders();
      var handler = request.downloadHandler;
      var body = handler.AsStringBody(bodySize);

      if (0 != code) {
        jsonObject["code"] = code;
      }

      if (!string.IsNullOrEmpty(url)) {
        jsonObject["url"] = url;
      }

      if (!string.IsNullOrEmpty(error)) {
        jsonObject["error"] = error;
      }

      if (null != headers && 0 != headers.Count) {
        jsonObject["headers"] = headers;
      }

      if (!string.IsNullOrEmpty(body)) {
        jsonObject["body"] = body;
      }

      return Json.Serialize(jsonObject);
    }

    public static string AsStringBody(this DownloadHandler handler, int bodySize = 256) {
      switch (handler) {
        case null:
          return string.Empty;
        case DownloadHandlerBuffer buffer when buffer.isDone && !string.IsNullOrEmpty(buffer.text): {
          var postfix = buffer.text.Length > bodySize ? "..." : string.Empty;
          return $"{buffer.text.Take(bodySize)}{postfix}";
        }
        case DownloadHandlerAssetBundle bundle when bundle.isDone && null != bundle.assetBundle:
          return $"AssetBundle({bundle.assetBundle.name})";
        case DownloadHandlerFile _:
          return "File()";
        case DownloadHandlerTexture texture when texture.isDone && null != texture.texture: {
          var w = texture.texture.width;
          var h = texture.texture.height;

          return $"Texture({w}x{h})";
        }
        case DownloadHandlerAudioClip clip when clip.isDone && null != clip.audioClip:
          return $"AudioClip({clip.audioClip.length})";
      }

      return string.Empty;
    }

    public static UnityWebRequest AppendHeaders(this UnityWebRequest request,
      IEnumerable<KeyValuePair<string, object>> headers) {

      if (null == headers) {
        return request;
      }

      var hx = headers.ToList();

      foreach (var p in hx.Where(p => !p.IsInternalHeader())) {
        request.SetRequestHeader(p.Key, p.Value?.ToString());
      }

      return request;
    }

    public static bool LocalOrCached(this UnityWebRequest request) {
      var url = request?.url;
      return !string.IsNullOrEmpty(url) && url.StartsWith("file://");
    }

    public static bool Success(this UnityWebRequest request) {
      var isSuccessCode = request.responseCode >= 200 && request.responseCode <= 299;
      return isSuccessCode && !request.isHttpError && !request.isNetworkError;
    }

    public static bool Redirect(this UnityWebRequest request) {
      return request.responseCode >= 300 && request.responseCode <= 399;
    }

    public static bool NotModified(this  UnityWebRequest request) {
      return request.responseCode == 304;
    }
  }
}
