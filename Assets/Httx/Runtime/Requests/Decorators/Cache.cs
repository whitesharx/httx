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
using Httx.Requests.Extensions;

namespace Httx.Requests.Decorators {
  public enum Storage {
    Memory,
    Disk,
    Native
  }

  public class Cache : BaseRequest {
    private readonly Storage storageType;
    private readonly int maxAge;

    public Cache(IRequest next, Storage type, TimeSpan ttl = default) : base(next) {
      storageType = type;
      maxAge = (int)(ttl == default ? -1 : ttl.TotalMilliseconds);
    }

    public override IEnumerable<KeyValuePair<string, object>> Headers {
      get {
        var result = new Dictionary<string, object>();

        if (storageType == Storage.Memory) {
          result.Add(InternalHeaders.MemoryCacheEnabled, true);
        } else if (storageType == Storage.Disk) {
          result.Add(InternalHeaders.DiskCacheEnabled, true);
        } else if (storageType == Storage.Native) {
          result.Add(InternalHeaders.NativeCacheEnabled, true);
        }

        if (-1 != maxAge) {
          result.Add(InternalHeaders.CacheItemMaxAge, maxAge);
        }

        return result;
      }
    }
  }

  public static class CacheFluentExtensions {
    public static IRequest Cache(this IRequest request, Storage type, TimeSpan ttl = default) =>
        new Cache(request, type, ttl);
  }
}
