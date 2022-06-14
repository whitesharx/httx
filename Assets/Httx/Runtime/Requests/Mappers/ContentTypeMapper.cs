using System.Collections.Generic;

namespace Httx.Requests.Mappers {
  public class ContentTypeMapper : IResultMapper<string> {
    public string FromResult(object result) {
      if (!(result is Dictionary<string, string> headers)) {
        return string.Empty;
      }

      headers.TryGetValue("Content-Type", out var contentType);

      return contentType;
    }
  }
}
