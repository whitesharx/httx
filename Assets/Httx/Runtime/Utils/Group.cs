using System.Collections.Generic;
using Httx.Requests;

namespace Httx.Utils {
  public class Group : BaseRequest {
    private readonly string group;

    public Group(IRequest next, string abGroup = null) : base(next) {
      group = abGroup;
    }

    public override IEnumerable<KeyValuePair<string, object>> Headers =>
        string.IsNullOrEmpty(group)
            ? new Dictionary<string, object>()
            : new Dictionary<string, object> { ["X-Accept-Group"] = group };
  }

  public static class GroupFluentExtensions {
    public static IRequest Group(this IRequest request, string abGroup = null) => new Group(request, abGroup);
  }
}
