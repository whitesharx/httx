// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.Collections.Generic;
using Httx.Requests.Attributes;
using Httx.Requests.Awaiters;
using Httx.Requests.Extensions;

namespace Httx.Requests.Types {
  [Awaiter(typeof(UnityWebRequestFileAwaiter))]
  public class File : BaseRequest {
    private readonly string path;
    private readonly bool isAppend;
    private readonly bool isRemoveOnAbort;

    public File(string url, string path, bool isAppend = false, bool isRemoveOnAbort = false) : base(null) {
      Url = url;

      this.path = path;
      this.isAppend = isAppend;
      this.isRemoveOnAbort = isRemoveOnAbort;
    }

    public override string Url { get; }

    public override IEnumerable<KeyValuePair<string, object>> Headers =>
      new Dictionary<string, object> {
        [InternalHeaders.FilePath] = path,
        [InternalHeaders.FileAppend] = isAppend,
        [InternalHeaders.FileRemoveOnAbort] = isRemoveOnAbort
      };
  }
}
