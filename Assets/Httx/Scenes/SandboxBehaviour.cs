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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Httx;
using Httx.Caches.Collections;
using Httx.Caches.Disk;
using Httx.Httx.Sources.Caches;
using Httx.Loggers;
using Httx.Requests.Awaiters;
using Httx.Requests.Decorators;
using Httx.Requests.Executors;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using Httx.Utils;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using Cache = Httx.Requests.Decorators.Cache;
using Texture = Httx.Requests.Types.Texture;

class SandboxBehaviour : MonoBehaviour, IProgress<float> {
  [UsedImplicitly]
  private async void Start() {
    // var result = await new As<string>(new Get(new Text("https://google.com")));

    // var r2 = await new Head("https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle");
    // var r3 = await new Length("https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle");

    // var jsonTextResult = await new As<string>(new Get(new Text("http://time.jsontest.com")));
    // Debug.Log($"JsonResult: {jsonTextResult}");

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
    // var bundleResult = await new As<AssetBundle>(new Get(new Bundle(fileUrl)));
    // Debug.Log($"BundleResult: {bundleResult}");
    //
    // bundleResult.Unload(true);
    //
    // var manifestResult = await new As<AssetBundleManifest>(new Get(new Manifest(fileUrl)));
    // Debug.Log($"ManifestResult: {manifestResult}");














    var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../", "__httx_cache_tests"));

    if (Directory.Exists(path)) {
      Directory.Delete(path, true);
    }

    var maxSize = 1024 * 1024 * 8;
    var diskCacheArgs = new DiskCacheArgs(path, 1, maxSize, 128);

    var diskCache = new DiskCache(diskCacheArgs);
    diskCache.Initialize(() => {
      var builder = new Context.Builder();
      builder.WithLogger(new UnityDefaultLogger());
      builder.WithDiskCache(diskCache);

      OnContextReady(builder.Instantiate());
    });
  }








  private async void OnContextReady(Context context) {
    Debug.Log($"======== OnContextReady: {context}");

    var textUrl = "http://www.mocky.io/v2/5e63496b3600007500e8dcd5";
    var imageUrl = "https://upload.wikimedia.org/wikipedia/en/7/7d/Lenna_%28test_image%29.png";
    var bundleUrl = "https://emilystories.app/static/v29/story/bundles/scene_1.apple-bundle";

    // ---

    var noCacheText = await new As<string>(new Get(new Text(textUrl)));

    Debug.Log($"text-no-cache: {noCacheText}");

    var withCacheText = await new As<string>(new Get(new Cache(new Text(textUrl), Storage.Disk)));

    Debug.Log($"text-with-cache: {withCacheText}");

    // ---

    var noCacheImage = await new As<UnityEngine.Texture>(new Get(new Texture(imageUrl)));

    Debug.Log($"image-no-cache: {noCacheImage}");

    var withCacheImage = await new As<UnityEngine.Texture>(new Get(new Cache(new Texture(imageUrl), Storage.Disk)));

    Debug.Log($"image-with-cache: {withCacheImage}");

    // --- TODO:

    // var noCacheBundle = await new As<AssetBundle>(new Get(new Bundle(bundleUrl), this));
    //
    // Debug.Log($"bundle-no-cache: {noCacheBundle}");
    //
    // var withCacheBundle = await new As<AssetBundle>(new Get(new Cache(new Bundle(bundleUrl), Storage.Disk), this));
    //
    // Debug.Log($"bundle-with-cache: {withCacheBundle}");
  }




  public void Report(float value) => Debug.Log($"SandboxBehaviour({value})");
}
