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
using System.Threading;
using Httx.Externals.MiniJSON;
using Httx.Requests.Awaiters;
using Httx.Requests.Decorators;
using Httx.Requests.Mappers;
using UnityEngine.Networking;

namespace Httx.Requests.Extensions {
  public static class InternalHeaders {
    private const string Prefix = "X-Httx-";

    public const string CompletedResult = Prefix + "Completed-Result";
    public const string MemoryCacheEnabled = Prefix + "MemoryCache-Enabled";
    public const string DiskCacheEnabled = Prefix + "DiskCache-Enabled";
    public const string NativeCacheEnabled = Prefix + "NativeCache-Enabled";
    public const string CacheItemMaxAge = Prefix + "CacheItem-MaxAge";
    public const string ProgressObject = Prefix + "Progress-Object";
    public const string TextureReadable = Prefix + "Texture-NonReadable";
    public const string FilePath = Prefix + "File-Path";
    public const string FileAppend = Prefix + "File-Append";
    public const string FileRemoveOnAbort = Prefix + "File-RemoveOnAbort";
    public const string ResponseCodeOnly = Prefix + "ResponseCodeOnly";
    public const string AssetBundleCrc = Prefix + "AssetBundle-Crc";
    public const string AssetBundleHash = Prefix + "AssetBundle-Hash";
    public const string AssetBundleVersion = Prefix + "AssetBundle-Verison";
    public const string AssetBundleLoadManifest = Prefix + "AssetBundle-LoadManifest";
    public const string ResourcePath = Prefix + "Resource-Path";
    public const string CancelToken = Prefix + "CancelToken";
    public const string ConditionObject = Prefix + "Condition-Object";
    public const string HookObject = Prefix + "Hook-Object";

    public static bool IsInternalHeader(this KeyValuePair<string, object> header) {
      return !string.IsNullOrEmpty(header.Key) && header.Key.StartsWith(Prefix);
    }
  }

  public static class RequestExtensions {
    public static string ResolveVerb(this IRequest request) {
      return LeftToRight(request).Select(r => r.Verb).Last(verb => !string.IsNullOrEmpty(verb));
    }

    public static string ResolveUrl(this IRequest request) {
      return LeftToRight(request)
          .Select(r => r.Url)
          .Last(url => !string.IsNullOrEmpty(url))
          .NormalizeStreamingAssetsUrl();
    }

    public static IEnumerable<byte> ResolveBody(this IRequest request) {
      return LeftToRight(request).Select(r => r.Body).LastOrDefault(body => null != body && 0 != body.Count());
    }

    public static IEnumerable<KeyValuePair<string, object>> ResolveHeaders(this IRequest request) {
      return LeftToRight(request)
          .Select(r => r.Headers ?? Enumerable.Empty<KeyValuePair<string, object>>())
          .Aggregate((a, b) => a.Concat(b));
    }

    public static IBodyMapper<TBody> ResolveBodyMapper<TBody>(this IRequest request, Context ctx) {
      var mapperType = LeftToRight(request)
          .Select(r => ctx.ResolveMapper(r.GetType()))
          .LastOrDefault(t => null != t);

      if (null == mapperType) {
        throw new InvalidOperationException("[resolve body mapper]: mapper of not found");
      }

      var mapperArgs = mapperType.GetGenericArguments();
      var mapperArgsCount = mapperArgs.Length;

      if (0 == mapperArgsCount) {
        return (IBodyMapper<TBody>)Activator.CreateInstance(mapperType);
      }

      var typeArgs = mapperArgs
          .Select((t, idx) => 0 == idx ? typeof(TBody) : typeof(object))
          .ToArray();

      return (IBodyMapper<TBody>)Activator.CreateInstance(mapperType.MakeGenericType(typeArgs));
    }

    public static IResultMapper<TResult> ResolveResultMapper<TResult>(this IRequest request, Context ctx) {
      var mapperType = LeftToRight(request)
          .Select(r => ctx.ResolveMapper(r.GetType()))
          .FirstOrDefault(t => null != t);

      if (null == mapperType) {
        throw new InvalidOperationException("[resolve result mapper]: result of not found");
      }

      var mapperArgs = mapperType.GetGenericArguments();
      var mapperArgsCount = mapperArgs.Length;

      if (0 == mapperArgsCount) {
        return (IResultMapper<TResult>)Activator.CreateInstance(mapperType);
      }

      var resultArgs = mapperArgs
          .Select((t, idx) => mapperArgsCount - 1 == idx ? typeof(TResult) : typeof(object))
          .ToArray();

      return (IResultMapper<TResult>)Activator.CreateInstance(mapperType.MakeGenericType(resultArgs));
    }

    public static IAwaiter<TResult> ResolveAwaiter<TResult>(this IRequest request, Context ctx) {
      var awaiterType = LeftToRight(request)
          .Select(r => ctx.ResolveAwaiter(r.GetType()))
          .FirstOrDefault(t => null != t);

      if (null == awaiterType) {
        throw new InvalidOperationException("[resolve awaiter]: awaiter not found");
      }

      var resultType = awaiterType.ContainsGenericParameters
          ? awaiterType.MakeGenericType(typeof(TResult))
          : awaiterType;

      var awakeConstructor = resultType.GetConstructor(new[] { typeof(IRequest) });
      return (IAwaiter<TResult>)awakeConstructor?.Invoke(new object[] { request });
    }

    public static T FetchHeader<T>(this IEnumerable<KeyValuePair<string, object>> headers, string key,
        T defaultValue = default) {
      var value = headers?.FirstOrDefault(h => h.Key == key).Value;
      return default != value ? (T)value : defaultValue;
    }

    public static bool IsMemoryCacheEnabled(this IRequest request) {
      var headers = request.ResolveHeaders()?.ToList() ?? new List<KeyValuePair<string, object>>();
      var isEnabled = headers.FetchHeader<bool>(InternalHeaders.MemoryCacheEnabled);

      return isEnabled;
    }

    public static int FetchCacheItemMaxAge(this IRequest request) {
      var headers = request.ResolveHeaders()?.ToList() ?? new List<KeyValuePair<string, object>>();
      var maxAge = headers.FetchHeader<int>(InternalHeaders.CacheItemMaxAge);

      return maxAge;
    }

    public static CancellationToken FetchCancelToken(this IRequest request) {
      var headers = request.ResolveHeaders()?.ToList() ?? new List<KeyValuePair<string, object>>();
      var token = headers.FetchHeader<CancellationToken>(InternalHeaders.CancelToken);

      return token;
    }

    public static Condition FetchConditionObject(this IRequest request) {
      var headers = request.ResolveHeaders()?.ToList() ?? new List<KeyValuePair<string, object>>();
      var condition = headers.FetchHeader<Condition>(InternalHeaders.ConditionObject);

      return condition;
    }

    public static void CallOnBeforeRequestSent(this IRequest request, UnityWebRequest unityWebRequest) {
      var headers = request.ResolveHeaders()?.ToList() ?? new List<KeyValuePair<string, object>>();
      var callbackObject = headers.FetchHeader<Callback<UnityWebRequest>>(InternalHeaders.HookObject);

      callbackObject?.OnBeforeRequestSent?.Invoke(unityWebRequest);
    }

    public static void CallOnOnResponseReceived(this IRequest request, UnityWebRequest unityWebRequest) {
      var headers = request.ResolveHeaders()?.ToList() ?? new List<KeyValuePair<string, object>>();
      var callbackObject = headers.FetchHeader<Callback<UnityWebRequest>>(InternalHeaders.HookObject);

      callbackObject?.OnResponseReceived?.Invoke(unityWebRequest);
    }

    public static string AsJson(this IRequest request, int bodySize = 256) {
      var jsonObject = new Dictionary<string, object> {
          ["verb"] = request.ResolveVerb(),
          ["url"] = request.ResolveUrl(),
          ["request"] = LeftToRight(request).Select(r => r.GetType().Name).ToArray()
      };

      var body = request.ResolveBody()?.ToArray();

      if (null != body && 0 != body.Length) {
        var postfix = body.Length > bodySize ? "..." : string.Empty;
        var bodyContent = Encoding.UTF8.GetString(body.Take(bodySize).ToArray());

        jsonObject["body"] = $"{bodyContent}{postfix}";
      }

      var headers = request.ResolveHeaders()?.ToArray();

      if (null == headers || 0 == headers.Length) {
        return Json.Serialize(jsonObject);
      }

      var headersBuffer = new Dictionary<string, object>();

      foreach (var keyValue in headers) {
        headersBuffer[keyValue.Key] = keyValue.Value;
      }

      jsonObject["headers"] = headersBuffer;

      return Json.Serialize(jsonObject);
    }

    private static IEnumerable<IRequest> LeftToRight(IRequest request) {
      var result = new List<IRequest>();
      var inner = request;

      while (null != inner) {
        result.Add(inner);
        inner = inner.Next;
      }

      return result;
    }
  }
}
