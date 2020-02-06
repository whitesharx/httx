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

using System.IO;
using Httx.Caches.Disk;
using NUnit.Framework;
using UnityEngine;

namespace Httx.Tests {
  public class DiskLruCacheTests {
    private const int AppVersion = 100;
    private const string CacheDirectory = "__httx_cache_tests";

    private DirectoryInfo directory;
    private DiskLruCache cache;

    [SetUp]
    public void SetUp() {
      var cacheDirectory = Path.Combine(Application.dataPath, "../", CacheDirectory);
      var cachePath = Path.GetFullPath(cacheDirectory);

      Debug.Log($"Set Up: {cachePath}");

      directory = new DirectoryInfo(cachePath);
      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);
    }

    [TearDown]
    public void TearDown() {
      cache.Close();
    }

    [Test]
    public void TestSuccess() {
      Assert.IsTrue(true, "Test has passed. Woohooo.");
    }

    [Test]
    public void TestFailure() {
      Assert.IsTrue(false, "Test has NOT passed.");
    }
  }
}
