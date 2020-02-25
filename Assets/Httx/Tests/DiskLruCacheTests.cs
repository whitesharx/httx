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
using System.IO;
using System.Linq;
using Httx.Caches.Disk;
using NUnit.Framework;
using UnityEngine;

namespace Httx.Tests {
  public class DiskLruCacheTests {
    private const int AppVersion = 100;
    private const string CacheDirectory = "__httx_cache_tests";

    private DirectoryInfo directory;
    private DiskLruCache cache;

    private FileInfo journalFile;
    private FileInfo journalBackupFile;

    [SetUp]
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

    [TearDown]
    public void TearDown() {
      cache.Close();
    }

    [Test]
    public void EmptyCache() {
      cache.Close();
      AssertJournalEquals();
    }

    [Test]
    public void WriteAndReadEntry() {
      var editor = cache.Edit("k1");
      editor.Set(0, "ABC");
      editor.Set(1, "DE");

      Assert.That(editor.GetString(0), Is.Null);
      Assert.That(editor.NewInputStream(0), Is.Null);
      Assert.That(editor.GetString(1), Is.Null);
      Assert.That(editor.NewInputStream(1), Is.Null);

      editor.Commit();

      var snapshot = cache.Get("k1");

      Assert.That(snapshot.GetString(0), Is.EqualTo("ABC"));
      Assert.That(snapshot.GetLength(0), Is.EqualTo(3));
      Assert.That(snapshot.GetString(1), Is.EqualTo("DE"));
      Assert.That(snapshot.GetLength(1), Is.EqualTo(2));
    }

    [Test]
    public void ReadAndWriteEntryAcrossCacheOpenAndClose() {
      var editor = cache.Edit("k1");
      editor.Set(0, "A");
      editor.Set(1, "B");
      editor.Commit();

      cache.Close();

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);
      var snapshot = cache.Get("k1");

      Assert.That(snapshot.GetString(0), Is.EqualTo("A"));
      Assert.That(snapshot.GetLength(0), Is.EqualTo(1));
      Assert.That(snapshot.GetString(1), Is.EqualTo("B"));
      Assert.That(snapshot.GetLength(1), Is.EqualTo(1));

      snapshot.Dispose();
    }

    [Test]
    // [Ignore("skipping for now...")]
    public void ReadAndWriteEntryWithoutProperClose() {
      var creator = cache.Edit("k1");
      creator.Set(0, "A");
      creator.Set(1, "B");
      creator.Commit();

      // Simulate a dirty close of 'cache' by opening the cache directory again.
      var cache2 = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);
      var snapshot = cache2.Get("k1");

      Assert.That(snapshot.GetString(0), Is.EqualTo("A"));
      Assert.That(snapshot.GetLength(0), Is.EqualTo(1));
      Assert.That(snapshot.GetString(1), Is.EqualTo("B"));
      Assert.That(snapshot.GetLength(1), Is.EqualTo(1));

      snapshot.Dispose();
      cache2.Close();
    }

    [Test]
    public void JournalWithEditAndPublish() {
      var creator = cache.Edit("k1");

      AssertJournalEquals("DIRTY k1"); // DIRTY must always be flushed.

      creator.Set(0, "AB");
      creator.Set(1, "C");
      creator.Commit();

      cache.Close();

      AssertJournalEquals("DIRTY k1", "CLEAN k1 2 1");
    }

    [Test]
    public void RevertedNewFileIsRemoveInJournal() {
      var creator = cache.Edit("k1");

      AssertJournalEquals("DIRTY k1"); // DIRTY must always be flushed.

      creator.Set(0, "AB");
      creator.Set(1, "C");
      creator.Abort();

      cache.Close();
      AssertJournalEquals("DIRTY k1", "REMOVE k1");
    }

    [Test]
    public void unterminatedEditIsRevertedOnClose() {
      cache.Edit("k1");
      cache.Close();

      AssertJournalEquals("DIRTY k1", "REMOVE k1");
    }









    private void AssertJournalEquals(params string[] expectedBodyLines) {
      var lines = new List<string> {
        DiskLruCache.Magic,
        DiskLruCache.Version1,
        "100",
        "2",
        string.Empty
      };

      if (null != expectedBodyLines && 0 != expectedBodyLines.Length) {
        lines.AddRange(expectedBodyLines);
      }

      Assert.That(ReadJournalLines(), Is.EqualTo(lines));
    }

    private List<string> ReadJournalLines() {
      using (var reader = new StreamReader(journalFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
        var lines = reader
          .ReadToEnd()
          .Split(new [] { Environment.NewLine }, StringSplitOptions.None)
          .ToList();

        lines.RemoveAt(lines.Count - 1);

        return lines;
      }
    }
  }
}
