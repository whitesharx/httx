// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;
using System.Linq;

namespace Httx.Requests.Types {
  public class Json<A, B> : Request<B> {
    public Json(string url, A body = default) : base(null) {
      Url = url;
      Body = null;
    }

    public override string Url { get; }

    public override IEnumerable<byte> Body { get; }

    public override IDictionary<string, object> Headers =>
      new Dictionary<string, object> {
        { "Accept", "application/json" },
        { "Content-Type", "application/json" }
      };
  }

  public class Json : Json<object, object> {
    public Json(string url, object body = null) : base(url, body) { }
  }
}
