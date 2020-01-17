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
using Httx.Attributes;
using Httx.Extensions;
using Httx.Requests.Awaiters;
using Httx.Requests.Mappers;
using UnityEngine;

namespace Httx.Requests {
  public class Request : IRequest, IAwaitable<object> {
    private readonly Request next;

    public Request(Request next) => this.next = next;

    public virtual string Verb =>
      RightToLeft(false).Select(r => r.Verb).First(verb => !string.IsNullOrEmpty(verb));

    public virtual string Url =>
      RightToLeft(false).Select(r => r.Url).First(url => !string.IsNullOrEmpty(url));

    public virtual IEnumerable<byte> Body =>
      RightToLeft(false).Select(r => r.Body).FirstOrDefault(body => null != body && 0 != body.Count());

    public virtual IDictionary<string, object> Headers =>
      LeftToRight(false).Select(r => r.Headers).Aggregate((a, b) => a.Merge(b));

    public IAwaiter<object> GetAwaiter() {
      var awaiterType = LeftToRight(false).Select(r => {
        var attribute = r.GetType().GetCustomAttribute<AwaiterAttribute>();
        return attribute?.AwaiterType;
      }).First(a => null != a);

      var awakeTypes = new[] { typeof(IRequest) };
      var awakeConstructor = awaiterType.GetConstructor(awakeTypes);

      return (IAwaiter<object>) awakeConstructor?.Invoke(new object[] { this });
    }



    // ???
    // protected IMapper<A, B> GetMapper<A, B>() {
    //   var mapperType = LeftToRight(false).Select(r => {
    //     var attribute = r.GetType().GetCustomAttribute<MapperAttribute>();
    //     return attribute?.MapperType;
    //   }).First(a => null != a);
    //
    //   var args = mapperType.GetGenericArguments();
    //
    //   if (0 == args.Length) {
    //     return (IMapper<A, B>) Activator.CreateInstance(mapperType);
    //   }
    //
    //   if (1 == args.Length) {
    //     return (IMapper<A, B>) Activator.CreateInstance(mapperType.MakeGenericType(typeof(A)));
    //   }
    //
    //   var t = mapperType.MakeGenericType(typeof(A), typeof(B));
    //   return (IMapper<A, B>) Activator.CreateInstance(t);
    // }



    private IEnumerable<Request> LeftToRight(bool includeSelf) {
      var result = new List<Request>();
      var inner = this;

      while (null != inner) {
        result.Add(inner);
        inner = inner.next;
      }

      return includeSelf ? result : result.Skip(1);
    }

    private IEnumerable<Request> RightToLeft(bool includeSelf) {
      return LeftToRight(includeSelf).Reverse();
    }
  }
}
