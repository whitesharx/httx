﻿// Copyright (c) 2020 Sergey Ivonchik
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
using Httx.Caches.Collections;
using Httx.Caches.Disk;
using Httx.Requests.Awaiters;
using Httx.Requests.Decorators;
using Httx.Requests.Executors;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;

class SandboxBehaviour : MonoBehaviour, IProgress<float> {
  private const int AppVersion = 100;
  private const string CacheDirectory = "__httx_cache_tests";

  private DirectoryInfo directory;
  private DiskLruCache cache;

  private FileInfo journalFile;
  private FileInfo journalBackupFile;

  public void SetUp() {
    var cacheDirectory = Path.Combine(Application.dataPath, "../", CacheDirectory);
    var cachePath = Path.GetFullPath(cacheDirectory);

    journalFile = new FileInfo(Path.Combine(cachePath, DiskLruCache.JournalFile));
    journalBackupFile = new FileInfo(Path.Combine(cachePath, DiskLruCache.JournalFileBackup));

    if (Directory.Exists(cachePath)) {
      Directory.Delete(cachePath, true);
    }

    directory = new DirectoryInfo(cachePath);
    cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);
  }

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

    SetUp();

    var editor = cache.Edit("k1");
    editor.SetAt(0, "A");
    editor.SetAt(1, "B");
    editor.Commit();

    cache.Close();

    cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);
    var snapshot = cache.Get("k1");

    Debug.Log("snapshot: " + snapshot);

    //
    // // Assert.That(snapshot.GetString(0), Is.EqualTo("A"));
    // // Assert.That(snapshot.GetLength(0), Is.EqualTo(1));
    // // Assert.That(snapshot.GetString(1), Is.EqualTo("B"));
    // // Assert.That(snapshot.GetLength(1), Is.EqualTo(1));
    //
    // Debug.Log(snapshot.GetString(0));
    // Debug.Log(snapshot.GetString(1));
    //
    // snapshot.Dispose();
  }

  public void Report(float value) => Debug.Log($"SandboxBehaviour({value})");
}
