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
using Httx.Requests.Awaiters;
using UnityEngine;

namespace Httx.Requests {
  public class Request<T> : IRequest<T> {
    private readonly Request<T> next;

    public Request(Request<T> next) => this.next = next;



    // public virtual string Verb {
    //   get {
    //     var inner = this;
    //
    //     while (null != inner && string.IsNullOrEmpty(inner.Verb)) {
    //       Debug.Log($"Searching verb... {inner.GetType().Name}");
    //       inner = next;
    //     }
    //
    //     return inner?.Verb;
    //   }
    // }


    public virtual string Verb => null;
    public virtual string Url => null;
    public virtual IEnumerable<byte> Body => null;
    public virtual IDictionary<string, object> Headers => null;




    public virtual IAwaiter<T> GetAwaiter() {
      var awaiter = LeftToRight(false).Select(r => r.GetAwaiter()).First(a => null != a);
      awaiter.Awake(this);

      return awaiter;
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
