// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;
using Httx.Requests.Attributes;
using Httx.Requests.Awaiters;
using Httx.Requests.Extensions;

namespace Httx.Requests.Types {
  [Awaiter(typeof(UnityWebRequestTextureAwaiter))]
  public class Texture : BaseRequest {
    private readonly bool readable;

    public Texture(string url, bool isReadable = false) : base(null) {
      Url = url;
      readable = isReadable;
    }

    public override string Url { get; }

    public override IEnumerable<KeyValuePair<string, object>> Headers =>
      new Dictionary<string, object> {
        [InternalHeaders.TextureReadable] = readable
      };
  }
}
