// Copyright (C) 2020 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace Httx.Requests.Decorators {
  public class Basic : BaseRequest {
    private readonly string token;

    public Basic(IRequest next, string userName, string password) : base(next) {
      token = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userName}:{password}"));
    }

    public override IEnumerable<KeyValuePair<string, object>> Headers =>
        new Dictionary<string, object> { ["Authorization"] = $"Basic {token}" };
  }

  public static class BasicFluentExtensions {
    public static IRequest Basic(this IRequest request, string userName, string password) =>
        new Basic(request, userName, password);
  }
}
