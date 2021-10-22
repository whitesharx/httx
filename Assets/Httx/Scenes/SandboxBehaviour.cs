// Copyright (c) 2020 Sergey Ivonchik
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE
// OR OTHER DEALINGS IN THE SOFTWARE.

using Httx;
using Httx.Requests.Decorators;
using Httx.Requests.Executors;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using UnityEngine;
using Cache = Httx.Requests.Decorators.Cache;

public class SandboxBehaviour : MonoBehaviour {
  private const int Version = 1;

  [UsedImplicitly]
  private void Awake() {
    Context.InitializeDefault(Version, OnContextReady);
  }

  private async void OnContextReady() {

    // const string contentETag = "\"4363e79751f42f61b7a7b2bd50e53837\"";
    // const string url = "https://whitesharx.app/temporary/etag.txt";
    //
    // // XXX: Tagged request requires Cache decorator
    //
    // var eTag = new ETag(contentETag, newTag => Debug.Log($"new-tag: {newTag}"));
    //
    // var response = await new As<string>(new Get(new Cache(new Text(url), Storage.Disk)), eTag);
    //
    // Debug.Log($"response: {response}");


    // var response = await new As<string>(new Get(new Text(url)));
    //
    // Debug.Log($"response: {response}");
  }
}
