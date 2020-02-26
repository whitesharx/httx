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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Httx.Caches.Disk;
using NUnit.Framework;
using UnityEditor.VersionControl;
using UnityEngine;
using FileMode = System.IO.FileMode;

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
    public void UnterminatedEditIsRevertedOnClose() {
      cache.Edit("k1");
      cache.Close();

      AssertJournalEquals("DIRTY k1", "REMOVE k1");
    }

    [Test]
    public void JournalDoesNotIncludeReadOfYetUnpublishedValue() {
      var creator = cache.Edit("k1");

      Assert.That(cache.Get("k1"), Is.Null);

      creator.Set(0, "A");
      creator.Set(1, "BC");
      creator.Commit();

      cache.Close();

      AssertJournalEquals("DIRTY k1", "CLEAN k1 1 2");
    }

    [Test]
    public void JournalWithEditAndPublishAndRead() {
      var k1Creator = cache.Edit("k1");
      k1Creator.Set(0, "AB");
      k1Creator.Set(1, "C");
      k1Creator.Commit();

      var k2Creator = cache.Edit("k2");
      k2Creator.Set(0, "DEF");
      k2Creator.Set(1, "G");
      k2Creator.Commit();

      var k1Snapshot = cache.Get("k1");
      k1Snapshot.Dispose();

      cache.Close();
      AssertJournalEquals("DIRTY k1", "CLEAN k1 2 1", "DIRTY k2", "CLEAN k2 3 1", "READ k1");
    }

    [Test]
    [Ignore("Not Implemented")]
    public void cannotOperateOnEditAfterPublish() {
      var editor = cache.Edit("k1");

      editor.Set(0, "A");
      editor.Set(1, "B");
      editor.Commit();

      // AssertInoperable(editor);
    }

    [Test]
    [Ignore("Not Implemented")]
    public void cannotOperateOnEditAfterRevert() {
      var editor = cache.Edit("k1");
      editor.Set(0, "A");
      editor.Set(1, "B");
      editor.Abort();

      // AssertInoperable(editor);
    }

    [Test]
    public void ExplicitRemoveAppliedToDiskImmediately() {
      var editor = cache.Edit("k1");
      editor.Set(0, "ABC");
      editor.Set(1, "B");
      editor.Commit();

      var k1 = GetCleanFile("k1", 0);

      Assert.That(ReadFile(k1), Is.EqualTo("ABC"));

      cache.Remove("k1");

      Assert.That(k1.Exists, Is.False);
    }

    /**
     * Each read sees a snapshot of the file at the time read was called.
     * This means that two reads of the same key can see different data.
     */
    [Test]
    public void ReadAndWriteOverlapsMaintainConsistency() {
      var v1Creator = cache.Edit("k1");
      v1Creator.Set(0, "AAaa");
      v1Creator.Set(1, "BBbb");
      v1Creator.Commit();

      var snapshot1 = cache.Get("k1");
      var inV1 = snapshot1.GetInputStream(0);

      Assert.That(inV1.ReadByte(), Is.EqualTo('A'));
      Assert.That(inV1.ReadByte(), Is.EqualTo('A'));

      var v1Updater = cache.Edit("k1");
      v1Updater.Set(0, "CCcc");
      v1Updater.Set(1, "DDdd");
      v1Updater.Commit();

      var snapshot2 = cache.Get("k1");
      Assert.That(snapshot2.GetString(0), Is.EqualTo("CCcc"));
      Assert.That(snapshot2.GetLength(0), Is.EqualTo(4));
      Assert.That(snapshot2.GetString(1), Is.EqualTo("DDdd"));
      Assert.That(snapshot2.GetLength(1), Is.EqualTo(4));
      snapshot2.Dispose();

      Assert.That(inV1.ReadByte(), Is.EqualTo('a'));
      Assert.That(inV1.ReadByte(), Is.EqualTo('a'));
      Assert.That(snapshot1.GetString(1), Is.EqualTo("BBbb"));
      Assert.That(snapshot1.GetLength(1), Is.EqualTo(4));
      snapshot1.Dispose();
    }

    [Test]
    public void OpenWithDirtyKeyDeletesAllFilesForThatKey() {
      cache.Close();

      var cleanFile0 = GetCleanFile("k1", 0);
      var cleanFile1 = GetCleanFile("k1", 1);
      var dirtyFile0 = GetDirtyFile("k1", 0);
      var dirtyFile1 = GetDirtyFile("k1", 1);

      WriteFile(cleanFile0, "A");
      WriteFile(cleanFile1, "B");
      WriteFile(dirtyFile0, "C");
      WriteFile(dirtyFile1, "D");

      // XXX: Original: createJournal("CLEAN k1 1 1", "DIRTY   k1");
      CreateJournal("CLEAN k1 1 1", "DIRTY k1");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      Assert.That(cleanFile0.Exists, Is.False);
      Assert.That(cleanFile1.Exists, Is.False);
      Assert.That(dirtyFile0.Exists, Is.False);
      Assert.That(dirtyFile1.Exists, Is.False);

      Assert.That(cache.Get("k1"), Is.Null);
    }

    [Test]
    public void OpenWithInvalidVersionClearsDirectory() {
      cache.Close();
      GenerateSomeGarbageFiles();
      CreateJournalWithHeader(DiskLruCache.Magic, "0", "100", "2", "");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
    }

    [Test]
    public void OpenWithInvalidAppVersionClearsDirectory() {
      cache.Close();
      GenerateSomeGarbageFiles();
      CreateJournalWithHeader(DiskLruCache.Magic, "1", "101", "2", "");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
    }

    [Test]
    public void OpenWithInvalidValueCountClearsDirectory() {
      cache.Close();
      GenerateSomeGarbageFiles();
      CreateJournalWithHeader(DiskLruCache.Magic, "1", "100", "1", "");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
    }

    [Test]
    public void OpenWithInvalidBlankLineClearsDirectory() {
      cache.Close();
      GenerateSomeGarbageFiles();
      CreateJournalWithHeader(DiskLruCache.Magic, "1", "100", "2", "x");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
    }

    [Test]
    public void OpenWithInvalidJournalLineClearsDirectory() {
      cache.Close();
      GenerateSomeGarbageFiles();
      CreateJournal("CLEAN k1 1 1", "BOGUS");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
      Assert.That(cache.Get("k1"), Is.Null);
    }

    [Test]
    public void OpenWithInvalidFileSizeClearsDirectory() {
      cache.Close();
      GenerateSomeGarbageFiles();
      CreateJournal("CLEAN k1 0000x001 1");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
      Assert.That(cache.Get("k1"), Is.Null);
    }

    [Test]
    [Ignore("For now, reader treats truncated line as correct. Fix later?")]
    public void OpenWithTruncatedLineDiscardsThatLine() {
      cache.Close();
      WriteFile(GetCleanFile("k1", 0), "A");
      WriteFile(GetCleanFile("k1", 1), "B");

      using (var writer = new StreamWriter(journalFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))) {
        writer.Write(DiskLruCache.Magic + "\n" + DiskLruCache.Version1 + "\n100\n2\n\nCLEAN k1 1 1");
      }

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);
      Assert.That(cache.Get("k1"), Is.Null);

      // The journal is not corrupt when editing after a truncated line.
      Set("k1", "C", "D");

      cache.Close();
      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertValue("k1", "C", "D");
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

    private void CreateJournal(params string[] bodyLines) {
      CreateJournalWithHeader(DiskLruCache.Magic, DiskLruCache.Version1, "100", "2", string.Empty, bodyLines);
    }

    private void CreateJournalWithHeader(string magic, string version, string appVersion,
      string valueCount, string blank, params string[] bodyLines) {

      var stream = journalFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);

      using (var writer = new StreamWriter(stream)) {
        writer.WriteLine(magic);
        writer.WriteLine(version);
        writer.WriteLine(appVersion);
        writer.WriteLine(valueCount);
        writer.WriteLine(blank);

        foreach (var line in bodyLines) {
          writer.WriteLine(line);
        }
      }
    }

    private FileInfo GetCleanFile(string key, int index) {
      return new FileInfo(Path.Combine(directory.FullName, $"{key}.{index}"));
    }

    private FileInfo GetDirtyFile(string key, int index) {
      return new FileInfo(Path.Combine(directory.FullName, $"{key}.{index}.tmp"));
    }

    private static string ReadFile(FileInfo file) {
      using (var reader = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
        return reader.ReadToEnd();
      }
    }

    private static void WriteFile(FileInfo file, string content) {
      using (var writer = new StreamWriter(file.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))) {
        writer.Write(content);
      }
    }

    private void GenerateSomeGarbageFiles() {
      var dir1 = new DirectoryInfo(Path.Combine(directory.FullName, "dir1"));
      var dir2 = new DirectoryInfo(Path.Combine(dir1.FullName, "dir1"));

      WriteFile(GetCleanFile("g1", 0), "A");
      WriteFile(GetCleanFile("g1", 1), "B");
      WriteFile(GetCleanFile("g2", 0), "C");
      WriteFile(GetCleanFile("g2", 1), "D");
      WriteFile(GetCleanFile("g2", 1), "D");

      WriteFile(new FileInfo(Path.Combine(directory.FullName, "otherFile0")), "E");

      Directory.CreateDirectory(dir1.FullName);
      Directory.CreateDirectory(dir2.FullName);

      WriteFile(new FileInfo(Path.Combine(dir2.FullName, "otherFile1")), "F");
    }

    private void AssertGarbageFilesAllDeleted() {
      Assert.That(GetCleanFile("g1", 0).Exists, Is.False);
      Assert.That(GetCleanFile("g1", 1).Exists, Is.False);
      Assert.That(GetCleanFile("g2", 0).Exists, Is.False);
      Assert.That(GetCleanFile("g2", 1).Exists, Is.False);

      var f1 = new FileInfo(Path.Combine(directory.FullName, "otherFile0"));
      var f2 = new FileInfo(Path.Combine(directory.FullName, "dir1"));

      Assert.That(f1.Exists, Is.False);
      Assert.That(f2.Exists, Is.False);
    }

    private void Set(string key, string value0, string value1) {
      var editor = cache.Edit(key);
      editor.Set(0, value0);
      editor.Set(1, value1);
      editor.Commit();
    }

    private void AssertAbsent(string key) {
      var snapshot = cache.Get(key);

      if (snapshot != null) {
        snapshot.Dispose();
        Assert.Fail();
      }

      Assert.That(GetCleanFile(key, 0).Exists, Is.False);
      Assert.That(GetCleanFile(key, 1).Exists, Is.False);
      Assert.That(GetDirtyFile(key, 0).Exists, Is.False);
      Assert.That(GetDirtyFile(key, 1).Exists, Is.False);
    }

    private void AssertValue(string key, string value0, string value1) {
      var snapshot = cache.Get(key);

      Assert.That(snapshot.GetString(0), Is.EqualTo(value0));
      Assert.That(snapshot.GetLength(0), Is.EqualTo(value0.Length));
      Assert.That(snapshot.GetString(1), Is.EqualTo(value1));
      Assert.That(snapshot.GetLength(1), Is.EqualTo(value1.Length));

      Assert.That(GetCleanFile(key, 0).Exists, Is.True);
      Assert.That(GetCleanFile(key, 1).Exists, Is.True);

      snapshot.Dispose();
    }

  }
}
