// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;
using System.Linq;
using Httx.Requests.Attributes;
using Httx.Requests.Awaiters;
using Httx.Requests.Mappers;

namespace Httx.Requests.Types {
  [Awaiter(typeof(UnityWebRequestAwaiter<>))]
  [Mapper(typeof(Utf8JsonUtilityMapper<,>))]
  public class Json<T> : BaseRequest {
    public Json(string url, T body = default) : base(null) {
      Url = url;
      Body = Equals(body, default(T)) ? default : new Utf8JsonUtilityMapper<T>().AsBody(body);
    }

    public override string Url { get; }
    public override IEnumerable<byte> Body { get; }

    public override IEnumerable<KeyValuePair<string, object>> Headers {
      get {
        var headers = new Dictionary<string, object> {
          { "Accept", "application/json;charset=UTF-8" }
        };

        if (null != Body && 0 != Body.Count()) {
          headers.Add("Content-Type", "application/json;charset=UTF-8");
        }

        return headers;
      }
    }
  }
}
