using UnityEngine.Networking;

namespace Httx.Requests.Executors {
  public class ContentType : As<string> {
    public ContentType(string url) : base(null) {
      Url = url;
    }

    public override string Url { get; }
    public override string Verb => UnityWebRequest.kHttpVerbHEAD;
  }
}
