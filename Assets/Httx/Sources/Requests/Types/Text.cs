// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;
using System.Linq;
using Httx.Requests.Awaiters;

namespace Httx.Requests.Types {
  public class Text : Request<string> {
    public Text(string url, string body = null) : base(null) {
      Url = url;
      Body = string.IsNullOrEmpty(body) ? null : System.Text.Encoding.UTF8.GetBytes(body);
    }

    public override string Url { get; }

    public override IEnumerable<byte> Body { get; }

    public override IDictionary<string, object> Headers {
      get {
        var headers = new Dictionary<string, object> {
          { "Accept", "text/plain;charset=UTF-8" }
        };

        if (null != Body && 0 != Body.Count()) {
          headers.Add("Content-Type", "text/plain;charset=UTF-8");
        }

        return headers;
      }
    }

    public override IAwaiter<string> GetAwaiter() => new UnityWebRequestAwaiter<string>();
  }
}
