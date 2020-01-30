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
using System.Linq;
using System.Net;
using System.Threading;
using Httx.Requests.Awaiters;
using Httx.Requests.Decorators;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Networking;

class SandboxBehaviour : MonoBehaviour, IProgress<float> {
  [UsedImplicitly]
  private async void Start() {
    // var result = await new As<string>(new Get(new Text("https://google.com")));

    // var r2 = await new Head("https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle");
    // var r3 = await new Length("https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle");

    // var result = await new As<string>(new Get(new Text("http://time.jsontest.com")));
    // Debug.Log($"Result: {result}");

    // var url = "https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle";
    // var assetBundle = await new As<AssetBundle>(new Get(new Bundle(url), this));
    // var manifest = assetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
    //
    // Debug.Log($"AssetBundle: {assetBundle} Manifest: {manifest}");

    // var fileUrl = "file:///Users/selfsx/Downloads/iOS.manifest";
    // var fileUrl = "file:///Users/selfsx/Downloads/scene_1.apple-bundle";
    // var fileUrl = "file:///Users/selfsx/Downloads/iOS";
    //
    // var result = await new As<AssetBundle>(new Get(new Bundle(fileUrl)));
    //
    // result.GetAllAssetNames().ToList().ForEach(asset => {
    //   Debug.Log($"Result: {asset}");
    // });

    // var fileUrl = "file:///Users/selfsx/Downloads/iOS";
    //
    // var op1 = new Func<IAsyncOperation, IAsyncOperation>(previousOrNull =>
    //   new UnityAsyncOperation(() => UnityWebRequestAssetBundle.GetAssetBundle(fileUrl).SendWebRequest()));
    //
    // var op2 = new Func<IAsyncOperation, IAsyncOperation>(previousOrNull => {
    //   var request = previousOrNull.Result as UnityWebRequest;
    //   var assetBundle = ((DownloadHandlerAssetBundle) request.downloadHandler).assetBundle;
    //   return new UnityAsyncOperation(() => assetBundle.LoadAllAssetsAsync());
    // });
    //
    // var opQueue = new AsyncOperationQueue(op1, op2);
    // opQueue.OnComplete += () => {
    //   Debug.Log($"OpQueueResult: {opQueue.Result}");
    // };

  }

  public void Report(float value) => Debug.Log($"SandboxBehaviour({value})");
}
