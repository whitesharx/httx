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
using Httx.Requests.Attributes;
using Httx.Requests.Awaiters;
using Httx.Requests.Mappers;
using UnityEngine;

namespace Httx.Requests.Extensions {
  public static class RequestExtensions {
    public static string ResolveVerb(this IRequest request) {
      return LeftToRight(request).Select(r => r.Verb).First(verb => !string.IsNullOrEmpty(verb));
    }

    public static string ResolveUrl(this IRequest request) {
      return LeftToRight(request).Select(r => r.Url).First(url => !string.IsNullOrEmpty(url));
    }

    public static IEnumerable<byte> ResolveBody(this IRequest request) {
      return LeftToRight(request).Select(r => r.Body).FirstOrDefault(body => null != body && 0 != body.Count());
    }

    public static IEnumerable<KeyValuePair<string, object>> ResolveHeaders(this IRequest request) {
      return LeftToRight(request)
        .Select(r => r.Headers ?? Enumerable.Empty<KeyValuePair<string, object>>())
        .Aggregate((a, b) => a.Concat(b));
    }

    public static IBodyMapper<TBody> ResolveBodyMapper<TBody>(this IRequest request) {
      var mapperType = LeftToRight(request)
        .Select(r => r.GetType().GetCustomAttribute<MapperAttribute>()?.MapperType)
        .FirstOrDefault(t => null != t);

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

    private static IEnumerable<IRequest> LeftToRight(IRequest request) {
      var result = new List<IRequest>();
      var inner = request;

      while (null != inner) {
        result.Add(inner);
        inner = inner.Next;
      }

      Debug.Log($"LeftToRight: {string.Join("->", result.Select(r => r.GetType().Name))}");

      return result;
    }
  }
}
