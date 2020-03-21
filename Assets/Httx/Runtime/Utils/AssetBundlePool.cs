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

using System.Collections.Generic;
using System.Linq;
using Httx.Requests.Extensions;
using UnityEngine;

namespace Httx.Utils {
  public class AssetBundlePool {
    private static AssetBundlePool instance;
    public static AssetBundlePool Instance => instance ?? (instance = new AssetBundlePool());

    private readonly Dictionary<string, string> refs = new Dictionary<string, string>();

    private AssetBundlePool() { }

    public void Retain(string url, string bundleName, Context ctx) {
      ctx.Logger?.Log($"AssetBundlePool [Retain]: {bundleName} -> {url}");
      refs[url] = bundleName;
    }

    public void Release(string url, bool unloadAllLoadedObjects, Context ctx) {
      refs.TryGetValue(url, out var bundleName);
      refs.Remove(url);

      if (string.IsNullOrEmpty(bundleName)) {
        ctx.Logger?.Log($"AssetBundlePool [Release]: {bundleName} -> no bundle found");
        return;
      }

      var bundles = AssetBundle.GetAllLoadedAssetBundles()?.ToList();

      if (null == bundles || 0 == bundles.Count) {
        return;
      }

      var bundleOpt = bundles.FirstOrDefault(b => b.name == bundleName);

      if (null == bundleOpt) {
        return;
      }

      ctx.Logger?.Log($"AssetBundlePool [Release/Unsafe]: {bundleName} -> {url}");
      bundleOpt.UnloadUnsafe(unloadAllLoadedObjects);
    }
  }
}
