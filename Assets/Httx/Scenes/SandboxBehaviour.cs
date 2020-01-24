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

using System;
using System.Collections.Generic;
using System.Linq;
using Httx.Requests.Decorators;
using Httx.Requests.Mappers;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using Httx.Utils;
using JetBrains.Annotations;
using UnityEngine;

public class Reporter : IProgress<float> {
  public void Report(float value) => Debug.Log($"Reporter({value})");
}

public class SandboxBehaviour : MonoBehaviour, IProgress<float> {
  [UsedImplicitly]
  private async void Start() {
    var url = "https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle";

    var result = await new As<string>(new Get(new Text("https://google.com")));

    Debug.Log(result);

    // StartRequest1(url);
    // StartRequest2(url);


    // var r2 = await new Head("https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle");
    // var r3 = await new Length("https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle");



    // var result = await new As<string>(new Get(new Text("http://time.jsontest.com")));
    // Debug.Log($"Result: {result}");

    // https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle

  }

  public void Report(float value) => Debug.Log($"SandboxBehaviour({value})");

  private async void StartRequest1(string url) {
    var r1 = await new As<byte[]>(new Get(new Bytes(url), this));
    Debug.Log("r1-complete: " + r1.Length);
  }

  private async void StartRequest2(string url) {
    var r2 = await new As<byte[]>(new Get(new Bytes(url), new Reporter()));
    Debug.Log("r2-complete: " + r2.Length);
  }
}
