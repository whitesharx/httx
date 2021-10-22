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
using Httx.Requests.Awaiters;
using Httx.Requests.Extensions;
using JetBrains.Annotations;

namespace Httx.Requests.Executors {
  public class ETag {
    public ETag([CanBeNull] string ifNoneMatch, Action<string> updated) {
      IfNoneMatch = ifNoneMatch;
      Updated = updated;
    }

    [CanBeNull]
    public string IfNoneMatch { get; }

    public Action<string> Updated { get; }

    public override string ToString() => string.IsNullOrEmpty(IfNoneMatch) ? "empty" : IfNoneMatch;
  }

  public class As<TResult> : BaseRequest, IAwaitable<TResult> {
    private readonly ETag tag;

    public As(IRequest next, ETag eTag = null) : base(next) {
      tag = eTag;
    }

    public IAwaiter<TResult> GetAwaiter() => this.ResolveAwaiter<TResult>(Context.Instance);

    public override IEnumerable<KeyValuePair<string, object>> Headers {
      get {
        var headers = new Dictionary<string, object>();

        if (null == tag) {
          return headers;
        }

        headers[InternalHeaders.ETagObject] = tag;

        if (!string.IsNullOrEmpty(tag.IfNoneMatch)) {
          headers["If-None-Match"] = tag.IfNoneMatch;
        }

        return headers;
      }
    }
  }
}
