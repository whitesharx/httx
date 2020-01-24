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
using Httx.Requests.Attributes;
using Httx.Requests.Awaiters;
using Httx.Requests.Extensions;
using UnityEngine;

namespace Httx.Requests.Types {
  [Awaiter(typeof(UnityWebRequestAssetBundleAwaiter))]
  public class Bundle : BaseRequest {
    private readonly uint crc;
    private readonly uint version;
    private readonly Hash128 hash;

    public Bundle(string url, uint crc = 0) : base(null) {
      Url = url;
      this.crc = crc;
    }

    public Bundle(string url, uint version, uint crc = 0) : base(null) {
      Url = url;
      this.version = version;
      this.crc = crc;
    }

    public Bundle(string url, Hash128 hash, uint crc = 0) : base(null) {
      Url = url;
      this.hash = hash;
      this.crc = crc;
    }

    public override string Url { get; }

    public override IEnumerable<KeyValuePair<string, object>> Headers {
      get {
        var headers = new Dictionary<string, object> {
          ["Accept"] = "application/octet-stream"
        };

        if (0 != crc) {
          headers[InternalHeaders.AssetBundleCrc] = crc;
        }

        if (0 != version) {
          headers[InternalHeaders.AssetBundleVersion] = version;
        }

        if (default != hash) {
          headers[InternalHeaders.AssetBundleHash] = hash;
        }

        return headers;
      }
    }
  }
}
