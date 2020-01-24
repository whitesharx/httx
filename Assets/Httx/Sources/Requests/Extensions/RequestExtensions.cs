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
using System.Reflection;
using System.Text;
using Httx.Externals.MiniJSON;
using Httx.Requests.Attributes;
using Httx.Requests.Awaiters;
using Httx.Requests.Mappers;

namespace Httx.Requests.Extensions {
  public static class InternalHeaders {
    private const string Prefix = "X-Httx-";

    public const string ProgressObject = Prefix + "Progress-Object";
    public const string FilePath = Prefix + "File-Path";
    public const string ResponseCodeOnly = Prefix + "ResponseCodeOnly";
    public const string AssetBundleCrc = Prefix + "AssetBundle-Crc";
    public const string AssetBundleHash = Prefix + "AssetBundle-Hash";
    public const string AssetBundleVersion = Prefix + "AssetBundle-Verison";

    public static bool IsInternalHeader(this KeyValuePair<string, object> header) {
      return !string.IsNullOrEmpty(header.Key) && header.Key.StartsWith(Prefix);
    }
  }

  public static class RequestExtensions {
    public static string ResolveVerb(this IRequest request) {
      return LeftToRight(request).Select(r => r.Verb).Last(verb => !string.IsNullOrEmpty(verb));
    }

    public static string ResolveUrl(this IRequest request) {
      return LeftToRight(request).Select(r => r.Url).Last(url => !string.IsNullOrEmpty(url));
    }

    public static IEnumerable<byte> ResolveBody(this IRequest request) {
      return LeftToRight(request).Select(r => r.Body).LastOrDefault(body => null != body && 0 != body.Count());
    }

    public static IEnumerable<KeyValuePair<string, object>> ResolveHeaders(this IRequest request) {
      return LeftToRight(request)
        .Select(r => r.Headers ?? Enumerable.Empty<KeyValuePair<string, object>>())
        .Aggregate((a, b) => a.Concat(b));
    }

    public static IBodyMapper<TBody> ResolveBodyMapper<TBody>(this IRequest request) {
      var mapperType = LeftToRight(request)
        .Select(r => r.GetType().GetCustomAttribute<MapperAttribute>()?.MapperType)
        .LastOrDefault(t => null != t);

      if (null == mapperType) {
        throw new InvalidOperationException("[resolve body mapper]: mapper of not found");
      }

      var mapperArgs = mapperType.GetGenericArguments();
      var mapperArgsCount = mapperArgs.Length;

      if (0 == mapperArgsCount) {
        return (IBodyMapper<TBody>) Activator.CreateInstance(mapperType);
      }

      var typeArgs = mapperArgs
        .Select((t, idx) => 0 == idx ? typeof(TBody) : typeof(object))
        .ToArray();

      return (IBodyMapper<TBody>) Activator.CreateInstance(mapperType.MakeGenericType(typeArgs));
    }

    public static IResultMapper<TResult> ResolveResultMapper<TResult>(this IRequest request) {
      var mapperType = LeftToRight(request)
        .Select(r => r.GetType().GetCustomAttribute<MapperAttribute>()?.MapperType)
        .FirstOrDefault(t => null != t);

      if (null == mapperType) {
        throw new InvalidOperationException("[resolve result mapper]: result of not found");
      }

      var mapperArgs = mapperType.GetGenericArguments();
      var mapperArgsCount = mapperArgs.Length;

      if (0 == mapperArgsCount) {
        return (IResultMapper<TResult>) Activator.CreateInstance(mapperType);
      }

      var resultArgs = mapperArgs
        .Select((t, idx) => mapperArgsCount - 1 == idx ? typeof(TResult) : typeof(object))
        .ToArray();

      return (IResultMapper<TResult>) Activator.CreateInstance(mapperType.MakeGenericType(resultArgs));
    }

    public static IAwaiter<TResult> ResolveAwaiter<TResult>(this IRequest request) {
      var awaiterType = LeftToRight(request).Select(r => {
        var attribute = r.GetType().GetCustomAttribute<AwaiterAttribute>();
        return attribute?.AwaiterType;
      }).FirstOrDefault(t => null != t);

      if (null == awaiterType) {
        throw new InvalidOperationException("[resolve awaiter]: awaiter not found");
      }

      var resultType = awaiterType.ContainsGenericParameters ?
        awaiterType.MakeGenericType(typeof(TResult)) : awaiterType;

      var awakeConstructor = resultType.GetConstructor(new[] { typeof(IRequest) });
      return (IAwaiter<TResult>) awakeConstructor?.Invoke(new object[] { request });
    }

    public static string AsJson(this IRequest request, int bodySize = 256) {
      var jsonObject = new Dictionary<string, object>();

      jsonObject["verb"] = request.ResolveVerb();
      jsonObject["url"] = request.ResolveUrl();
      jsonObject["request"] = LeftToRight(request).Select(r => r.GetType().Name).ToArray();

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
