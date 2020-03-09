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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Httx;
using Httx.Caches;
using Httx.Caches.Collections;
using Httx.Caches.Disk;
using Httx.Caches.Memory;
using Httx.Loggers;
using Httx.Requests.Awaiters;
using Httx.Requests.Decorators;
using Httx.Requests.Executors;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using Httx.Sources.Caches;
using Httx.Utils;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using Cache = Httx.Requests.Decorators.Cache;
using Debug = UnityEngine.Debug;
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














    var diskCachePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../", "__httx_cache_disk_tests"));
    var nativeCachePath = Path.GetFullPath(Path.Combine(Application.dataPath, "../", "__httx_cache_native_tests"));

    if (Directory.Exists(diskCachePath)) {
      Directory.Delete(diskCachePath, true);
    }

    if (Directory.Exists(nativeCachePath)) {
      Directory.Delete(nativeCachePath, true);
    }






    const int maxSize = 1024 * 1024 * 12;
    var diskCacheArgs = new DiskCacheArgs(diskCachePath, 1, maxSize, 128);
    var nativeCacheArgs = new NativeCacheArgs(nativeCachePath, 7, maxSize);

    var diskCache = new DiskCache(diskCacheArgs);
    var nativeCache = new NativeCache(nativeCacheArgs);

    diskCache.Initialize(() => {
      nativeCache.Initialize(() => {
        var builder = new Context.Builder();
        builder.WithLogger(new UnityDefaultLogger());
        builder.WithMemoryCache(new MemoryCache(12));
        builder.WithDiskCache(diskCache);
        builder.WithNativeCache(nativeCache);

        OnContextReady(builder.Instantiate());
      });
    });



  }








  private async void OnContextReady(Context context) {
    Debug.Log($"======== OnContextReady: {context}");

    var textUrl = "http://www.mocky.io/v2/5e63496b3600007500e8dcd5";
    var imageUrl = "https://upload.wikimedia.org/wikipedia/en/7/7d/Lenna_%28test_image%29.png";
    var bundleUrl = "https://emilystories.app/static/v46/story/bundles/scene_1.apple-bundle";

    // // ---
    //
    // var noCacheText = await new As<string>(new Get(new Text(textUrl)));
    //
    // Debug.Log($"text-no-cache: {noCacheText}");
    //
    // var withCacheText = await new As<string>(new Get(new Cache(new Text(textUrl), Storage.Disk)));
    //
    // Debug.Log($"text-with-cache: {withCacheText}");
    //
    // // ---
    //
    // var noCacheImage = await new As<UnityEngine.Texture>(new Get(new Texture(imageUrl)));
    //
    // Debug.Log($"image-no-cache: {noCacheImage}");
    //
    // var withCacheImage = await new As<UnityEngine.Texture>(new Get(new Cache(new Texture(imageUrl), Storage.Disk)));
    //
    // Debug.Log($"image-with-cache: {withCacheImage}");

    // ---

    var s1 = new Stopwatch();
    var s2 = new Stopwatch();

    s1.Start();

    var noCacheBundle = await new As<AssetBundle>(new Get(new Cache(new Bundle(bundleUrl), Storage.Memory)));
    Debug.Log($"bundle-no-cache: {noCacheBundle}");

    s1.Stop();

    // noCacheBundle.Unload(true);




    s2.Start();

    var withCacheBundle = await new As<AssetBundle>(new Get(new Cache(new Bundle(bundleUrl), Storage.Memory)));
    Debug.Log($"bundle-with-cache: {withCacheBundle}");

    s2.Stop();

    // withCacheBundle.Unload(true);




    Debug.Log($"s1: {s1.Elapsed} s2: {s2.Elapsed}");
  }



  public void Report(float value) { }
  // public void Report(float value) => Debug.Log($"SandboxBehaviour({value})");
}
