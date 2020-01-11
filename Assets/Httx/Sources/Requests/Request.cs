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
using System.Reflection;
using Httx.Attributes;
using Httx.Extensions;
using Httx.Requests.Awaiters;

namespace Httx.Requests {
  public class Request<T> : IRequest<T> {
    private readonly Request<T> next;

    public Request(Request<T> next) => this.next = next;

    public virtual string Verb =>
      RightToLeft(false).Select(r => r.Verb).First(verb => !string.IsNullOrEmpty(verb));

    public virtual string Url =>
      RightToLeft(false).Select(r => r.Url).First(url => !string.IsNullOrEmpty(url));

    public virtual IEnumerable<byte> Body =>
      RightToLeft(false).Select(r => r.Body).First(body => null != body && 0 != body.Count());

    public virtual IDictionary<string, object> Headers =>
      LeftToRight(false).Select(r => r.Headers).Aggregate((a, b) => a.Merge(b));

    public IAwaiter<T> GetAwaiter() {
      var awaiterType = LeftToRight(false).Select(r => {
        var attribute = r.GetType().GetCustomAttribute<AwaiterAttribute>();
        return attribute?.AwaiterType;
      }).First(a => null != a);

      var awakeTypes = new[] { typeof(IRequest<T>) };
      var awakeConstructor = awaiterType.GetConstructor(awakeTypes);

      return (IAwaiter<T>) awakeConstructor?.Invoke(new object[] { this });
    }

    private IEnumerable<Request<T>> LeftToRight(bool includeSelf) {
      var result = new List<Request<T>>();
      var inner = this;

      while (null != inner) {
        result.Add(inner);
        inner = inner.next;
      }

      return includeSelf ? result : result.Skip(1);
    }

    private IEnumerable<Request<T>> RightToLeft(bool includeSelf) {
      return LeftToRight(includeSelf).Reverse();
    }
  }
}
