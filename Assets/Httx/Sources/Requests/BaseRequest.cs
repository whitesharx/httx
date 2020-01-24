// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;

namespace Httx.Requests {
  public class BaseRequest : IRequest {
    public BaseRequest(IRequest next) {
      Next = next;
    }

    public IRequest Next { get; }
    public virtual string Verb => Next?.Verb;
    public virtual string Url => Next?.Url;
    public virtual IEnumerable<byte> Body => Next?.Body;
    public virtual IEnumerable<KeyValuePair<string, object>> Headers => Next?.Headers;
  }
}
