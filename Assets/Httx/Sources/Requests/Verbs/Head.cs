// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;
using Httx.Requests.Attributes;
using Httx.Requests.Aux;
using Httx.Requests.Awaiters;
using Httx.Requests.Mappers;
using UnityEngine.Networking;

namespace Httx.Requests.Verbs {
  [Awaiter(typeof(UnityWebRequestAwaiter<>))]
  [Mapper(typeof(HeadersMapper))]
  public class Head : As<IEnumerable<KeyValuePair<string, string>>> {
    public Head(string url) : base(null) {
      Url = url;
    }

    public override string Url { get; }
    public override string Verb => UnityWebRequest.kHttpVerbHEAD;
  }
}
