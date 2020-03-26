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
using System.Threading.Tasks;
using Httx;
using Httx.Requests.Decorators;
using Httx.Requests.Executors;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using UnityEngine;
using Cache = Httx.Requests.Decorators.Cache;
using Text = UnityEngine.UI.Text;

class SandboxBehaviour : MonoBehaviour, IProgress<float> {
  [SerializeField]
  private Text debugText;

  [UsedImplicitly]
  private void Start() {
    const int appVersion = 1;
    Context.InitializeDefault(appVersion, OnContextReady);
  }

  private async void OnContextReady() {
    var textUrl = "http://www.mocky.io/v2/5e63496b3600007500e8dcd5";
    var jsonUrl = "http://www.mocky.io/v2/5e69dddb2d0000aa005f9e20";
    var imageUrl = "https://upload.wikimedia.org/wikipedia/en/7/7d/Lenna_%28test_image%29.png";

    var maxAge = TimeSpan.FromSeconds(2);
    var bundleUrl = "https://emilystories.app/static/v49/story/bundles/scene_1.apple-bundle";
    var bundle0 = await new As<AssetBundle>(new Get(new Cache(new Bundle(bundleUrl), Storage.Memory, maxAge)));
    Debug.Log($"0-bundle: {bundle0.name} waiting 1 sec...");

    await Task.Delay(TimeSpan.FromSeconds(1));

    var bundle1 = await new As<AssetBundle>(new Get(new Cache(new Bundle(bundleUrl), Storage.Memory, maxAge)));
    Debug.Log($"1-bundle: {bundle1.name} waiting 4 sec...");

    await Task.Delay(TimeSpan.FromSeconds(4));

    var bundle2 = await new As<AssetBundle>(new Get(new Cache(new Bundle(bundleUrl), Storage.Memory, maxAge)));
    Debug.Log($"2-bundle: {bundle2.name}");
  }

  public void Report(float value) { }
}
