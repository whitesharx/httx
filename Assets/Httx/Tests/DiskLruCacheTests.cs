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
      cache.Dispose();
    }

    [Test]
    public void EmptyCache() {
      cache.Dispose();
      AssertJournalEquals();
    }

    [Test]
    public void ValidateKey() {
      string key;

      try {
        key = "has_space ";
        cache.Edit(key);
        Assert.Fail("Exepcting an IllegalArgumentException as the key was invalid.");
      } catch (ArgumentException iae) {
        Assert.That(iae.Message, Contains.Substring("keys must match regex"));
      }

      try {
        key = "has_CR\r";
        cache.Edit(key);
        Assert.Fail("Exepcting an IllegalArgumentException as the key was invalid.");
      } catch (ArgumentException iae) {
        Assert.That(iae.Message, Contains.Substring("keys must match regex"));
      }

      try {
        key = "has_LF\n";
        cache.Edit(key);
        Assert.Fail("Exepcting an IllegalArgumentException as the key was invalid.");
      } catch (ArgumentException iae) {
        Assert.That(iae.Message, Contains.Substring("keys must match regex"));
      }

      try {
        key = "has_invalid/";
        cache.Edit(key);
        Assert.Fail("Exepcting an IllegalArgumentException as the key was invalid.");
      } catch (ArgumentException iae) {
        Assert.That(iae.Message, Contains.Substring("keys must match regex"));
      }

      try {
        key = "has_invalid\u2603";
        cache.Edit(key);
        Assert.Fail("Exepcting an IllegalArgumentException as the key was invalid.");
      } catch (ArgumentException iae) {
        Assert.That(iae.Message, Contains.Substring("keys must match regex"));
      }

      try {
        key = "this_is_way_too_long_this_is_way_too_long_this_is_way_too_long_"
          + "this_is_way_too_long_this_is_way_too_long_this_is_way_too_long";
        cache.Edit(key);
        Assert.Fail("Exepcting an IllegalArgumentException as the key was too long.");
      } catch (ArgumentException iae) {
        Assert.That(iae.Message, Contains.Substring("keys must match regex"));
      }

      key = "0123456789012345678901234567890123456789012345678901234567890123456789"
        + "01234567890123456789012345678901234567890123456789";
      cache.Edit(key).Abort();

      key = "abcdefghijklmnopqrstuvwxyz_0123456789";
      cache.Edit(key).Abort();

      key = "-20384573948576";
      cache.Edit(key).Abort();
    }

    [Test]
    public void WriteAndReadEntry() {
      var editor = cache.Edit("k1");
      editor.PutAt(0, "ABC");
      editor.PutAt(1, "DE");

      Assert.That(editor.StringAt(0), Is.Null);
      Assert.That(editor.ReaderInstanceAt(0), Is.Null);
      Assert.That(editor.StringAt(1), Is.Null);
      Assert.That(editor.ReaderInstanceAt(1), Is.Null);

      editor.Commit();

      var snapshot = cache.Get("k1");

      Assert.That(snapshot.StringAt(0), Is.EqualTo("ABC"));
      Assert.That(snapshot.LengthAt(0), Is.EqualTo(3));
      Assert.That(snapshot.StringAt(1), Is.EqualTo("DE"));
      Assert.That(snapshot.LengthAt(1), Is.EqualTo(2));
    }

    [Test]
    public void ReadAndWriteEntryAcrossCacheOpenAndClose() {
      var editor = cache.Edit("k1");
      editor.PutAt(0, "A");
      editor.PutAt(1, "B");
      editor.Commit();

      cache.Dispose();

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);
      var snapshot = cache.Get("k1");

      Assert.That(snapshot.StringAt(0), Is.EqualTo("A"));
      Assert.That(snapshot.LengthAt(0), Is.EqualTo(1));
      Assert.That(snapshot.StringAt(1), Is.EqualTo("B"));
      Assert.That(snapshot.LengthAt(1), Is.EqualTo(1));

      snapshot.Dispose();
    }

    [Test]
    public void ReadAndWriteEntryWithoutProperClose() {
      var creator = cache.Edit("k1");
      creator.PutAt(0, "A");
      creator.PutAt(1, "B");
      creator.Commit();

      // Simulate a dirty close of 'cache' by opening the cache directory again.
      var cache2 = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);
      var snapshot = cache2.Get("k1");

      Assert.That(snapshot.StringAt(0), Is.EqualTo("A"));
      Assert.That(snapshot.LengthAt(0), Is.EqualTo(1));
      Assert.That(snapshot.StringAt(1), Is.EqualTo("B"));
      Assert.That(snapshot.LengthAt(1), Is.EqualTo(1));

      snapshot.Dispose();
      cache2.Dispose();
    }

    [Test]
    public void JournalWithEditAndPublish() {
      var creator = cache.Edit("k1");

      AssertJournalEquals("DIRTY k1"); // DIRTY must always be flushed.

      creator.PutAt(0, "AB");
      creator.PutAt(1, "C");
      creator.Commit();

      cache.Dispose();

      AssertJournalEquals("DIRTY k1", "CLEAN k1 2 1");
    }

    [Test]
    public void RevertedNewFileIsRemoveInJournal() {
      var creator = cache.Edit("k1");

      AssertJournalEquals("DIRTY k1"); // DIRTY must always be flushed.

      creator.PutAt(0, "AB");
      creator.PutAt(1, "C");
      creator.Abort();

      cache.Dispose();
      AssertJournalEquals("DIRTY k1", "REMOVE k1");
    }

    [Test]
    public void UnterminatedEditIsRevertedOnClose() {
      cache.Edit("k1");
      cache.Dispose();

      AssertJournalEquals("DIRTY k1", "REMOVE k1");
    }

    [Test]
    public void JournalDoesNotIncludeReadOfYetUnpublishedValue() {
      var creator = cache.Edit("k1");

      Assert.That(cache.Get("k1"), Is.Null);

      creator.PutAt(0, "A");
      creator.PutAt(1, "BC");
      creator.Commit();

      cache.Dispose();

      AssertJournalEquals("DIRTY k1", "CLEAN k1 1 2");
    }

    [Test]
    public void JournalWithEditAndPublishAndRead() {
      var k1Creator = cache.Edit("k1");
      k1Creator.PutAt(0, "AB");
      k1Creator.PutAt(1, "C");
      k1Creator.Commit();

      var k2Creator = cache.Edit("k2");
      k2Creator.PutAt(0, "DEF");
      k2Creator.PutAt(1, "G");
      k2Creator.Commit();

      var k1Snapshot = cache.Get("k1");
      k1Snapshot.Dispose();

      cache.Dispose();
      AssertJournalEquals("DIRTY k1", "CLEAN k1 2 1", "DIRTY k2", "CLEAN k2 3 1", "READ k1");
    }

    [Test]
    public void CannotOperateOnEditAfterPublish() {
      var editor = cache.Edit("k1");

      editor.PutAt(0, "A");
      editor.PutAt(1, "B");
      editor.Commit();

      AssertInoperable(editor);
    }

    [Test]
    public void CannotOperateOnEditAfterRevert() {
      var editor = cache.Edit("k1");
      editor.PutAt(0, "A");
      editor.PutAt(1, "B");
      editor.Abort();

      AssertInoperable(editor);
    }

    [Test]
    public void ExplicitRemoveAppliedToDiskImmediately() {
      var editor = cache.Edit("k1");
      editor.PutAt(0, "ABC");
      editor.PutAt(1, "B");
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
      v1Creator.PutAt(0, "AAaa");
      v1Creator.PutAt(1, "BBbb");
      v1Creator.Commit();

      var snapshot1 = cache.Get("k1");
      var inV1 = snapshot1.ReaderAt(0);

      Assert.That(inV1.ReadByte(), Is.EqualTo('A'));
      Assert.That(inV1.ReadByte(), Is.EqualTo('A'));

      var v1Updater = cache.Edit("k1");
      v1Updater.PutAt(0, "CCcc");
      v1Updater.PutAt(1, "DDdd");
      v1Updater.Commit();

      var snapshot2 = cache.Get("k1");
      Assert.That(snapshot2.StringAt(0), Is.EqualTo("CCcc"));
      Assert.That(snapshot2.LengthAt(0), Is.EqualTo(4));
      Assert.That(snapshot2.StringAt(1), Is.EqualTo("DDdd"));
      Assert.That(snapshot2.LengthAt(1), Is.EqualTo(4));
      snapshot2.Dispose();

      Assert.That(inV1.ReadByte(), Is.EqualTo('a'));
      Assert.That(inV1.ReadByte(), Is.EqualTo('a'));
      Assert.That(snapshot1.StringAt(1), Is.EqualTo("BBbb"));
      Assert.That(snapshot1.LengthAt(1), Is.EqualTo(4));
      snapshot1.Dispose();
    }

    [Test]
    public void OpenWithDirtyKeyDeletesAllFilesForThatKey() {
      cache.Dispose();

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
      cache.Dispose();
      GenerateSomeGarbageFiles();
      CreateJournalWithHeader(DiskLruCache.Magic, "0", "100", "2", "");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
    }

    [Test]
    public void OpenWithInvalidAppVersionClearsDirectory() {
      cache.Dispose();
      GenerateSomeGarbageFiles();
      CreateJournalWithHeader(DiskLruCache.Magic, "1", "101", "2", "");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
    }

    [Test]
    public void OpenWithInvalidValueCountClearsDirectory() {
      cache.Dispose();
      GenerateSomeGarbageFiles();
      CreateJournalWithHeader(DiskLruCache.Magic, "1", "100", "1", "");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
    }

    [Test]
    public void OpenWithInvalidBlankLineClearsDirectory() {
      cache.Dispose();
      GenerateSomeGarbageFiles();
      CreateJournalWithHeader(DiskLruCache.Magic, "1", "100", "2", "x");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
    }

    [Test]
    public void OpenWithInvalidJournalLineClearsDirectory() {
      cache.Dispose();
      GenerateSomeGarbageFiles();
      CreateJournal("CLEAN k1 1 1", "BOGUS");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
      Assert.That(cache.Get("k1"), Is.Null);
    }

    [Test]
    public void OpenWithInvalidFileSizeClearsDirectory() {
      cache.Dispose();
      GenerateSomeGarbageFiles();
      CreateJournal("CLEAN k1 0000x001 1");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
      Assert.That(cache.Get("k1"), Is.Null);
    }

    [Test]
    [Ignore("For now, reader treats truncated line as correct. Fix later?")]
    public void OpenWithTruncatedLineDiscardsThatLine() {
      cache.Dispose();
      WriteFile(GetCleanFile("k1", 0), "A");
      WriteFile(GetCleanFile("k1", 1), "B");

      using (var writer =
        new StreamWriter(journalFile.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))) {
        writer.Write(DiskLruCache.Magic + "\n" + DiskLruCache.Version1 + "\n100\n2\n\nCLEAN k1 1 1");
      }

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);
      Assert.That(cache.Get("k1"), Is.Null);

      // The journal is not corrupt when editing after a truncated line.
      Set("k1", "C", "D");

      cache.Dispose();
      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertValue("k1", "C", "D");
    }

    [Test]
    public void OpenWithTooManyFileSizesClearsDirectory() {
      cache.Dispose();
      GenerateSomeGarbageFiles();
      CreateJournal("CLEAN k1 1 1 1");

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      AssertGarbageFilesAllDeleted();
      Assert.That(cache.Get("k1"), Is.Null);
    }

    [Test]
    public void KeyWithSpaceNotPermitted() {
      Assert.Catch<ArgumentException>(() => {
        cache.Edit("my key");
      });
    }

    [Test]
    public void KeyWithNewlineNotPermitted() {
      Assert.Catch<ArgumentException>(() => {
        cache.Edit("my\nkey");
      });
    }

    [Test]
    public void KeyWithCarriageReturnNotPermitted() {
      Assert.Catch<ArgumentException>(() => {
        cache.Edit("my\rkey");
      });
    }

    [Test]
    public void NullKeyThrows() {
      Assert.Catch<ArgumentException>(() => {
        cache.Edit(null);
      });
    }

    [Test]
    public void CreateNewEntryWithTooFewValuesFails() {
      var creator = cache.Edit("k1");
      creator.PutAt(1, "A");

      Assert.Catch<InvalidOperationException>(() => {
        creator.Commit();
      });

      Assert.That(GetCleanFile("k1", 0).Exists, Is.False);
      Assert.That(GetCleanFile("k1", 1).Exists, Is.False);
      Assert.That(GetDirtyFile("k1", 0).Exists, Is.False);
      Assert.That(GetDirtyFile("k1", 1).Exists, Is.False);

      Assert.That(cache.Get("k1"), Is.Null);

      var creator2 = cache.Edit("k1");
      creator2.PutAt(0, "B");
      creator2.PutAt(1, "C");
      creator2.Commit();
    }

    [Test]
    public void RevertWithTooFewValues() {
      var creator = cache.Edit("k1");
      creator.PutAt(1, "A");
      creator.Abort();

      Assert.That(GetCleanFile("k1", 0).Exists, Is.False);
      Assert.That(GetCleanFile("k1", 1).Exists, Is.False);
      Assert.That(GetDirtyFile("k1", 0).Exists, Is.False);
      Assert.That(GetDirtyFile("k1", 1).Exists, Is.False);

      Assert.That(cache.Get("k1"), Is.Null);
    }

    [Test]
    public void UpdateExistingEntryWithTooFewValuesReusesPreviousValues() {
      var creator = cache.Edit("k1");
      creator.PutAt(0, "A");
      creator.PutAt(1, "B");
      creator.Commit();

      var updater = cache.Edit("k1");
      updater.PutAt(0, "C");
      updater.Commit();

      var snapshot = cache.Get("k1");

      Assert.That(snapshot.StringAt(0), Is.EqualTo("C"));
      Assert.That(snapshot.LengthAt(0), Is.EqualTo(1));
      Assert.That(snapshot.StringAt(1), Is.EqualTo("B"));
      Assert.That(snapshot.LengthAt(1), Is.EqualTo(1));

      snapshot.Dispose();
    }

    [Test]
    public void GrowMaxSize() {
      cache.Dispose();
      cache = DiskLruCache.Open(directory, AppVersion, 2, 10);

      Set("a", "a", "aaa"); // size 4
      Set("b", "bb", "bbbb"); // size 6

      cache.MaxSize = 20;

      Set("c", "c", "c"); // size 12

      Assert.That(cache.Size, Is.EqualTo(12));
    }

    [Test]
    [Ignore("Not Implemented")]
    public void ShrinkMaxSizeEvicts() {
      // TODO: Implement
    }

    [Test]
    public void EvictOnInsert() {
      cache.Dispose();
      cache = DiskLruCache.Open(directory, AppVersion, 2, 10);

      Set("a", "a", "aaa"); // size 4
      Set("b", "bb", "bbbb"); // size 6
      Assert.That(cache.Size, Is.EqualTo(10));

      // Cause the size to grow to 12 should evict 'A'.
      Set("c", "c", "c");
      cache.Flush();
      Assert.That(cache.Size, Is.EqualTo(8));
      AssertAbsent("a");
      AssertValue("b", "bb", "bbbb");
      AssertValue("c", "c", "c");

      // Causing the size to grow to 10 should evict nothing.
      Set("d", "d", "d");
      cache.Flush();
      Assert.That(cache.Size, Is.EqualTo(10));
      AssertAbsent("a");
      AssertValue("b", "bb", "bbbb");
      AssertValue("c", "c", "c");
      AssertValue("d", "d", "d");

      // Causing the size to grow to 18 should evict 'B' and 'C'.
      Set("e", "eeee", "eeee");
      cache.Flush();
      Assert.That(cache.Size, Is.EqualTo(10));
      AssertAbsent("a");
      AssertAbsent("b");
      AssertAbsent("c");
      AssertValue("d", "d", "d");
      AssertValue("e", "eeee", "eeee");
    }

    [Test]
    public void EvictOnUpdate() {
      cache.Dispose();
      cache = DiskLruCache.Open(directory, AppVersion, 2, 10);

      Set("a", "a", "aa"); // size 3
      Set("b", "b", "bb"); // size 3
      Set("c", "c", "cc"); // size 3

      Assert.That(cache.Size, Is.EqualTo(9));

      // Causing the size to grow to 11 should evict 'A'.
      Set("b", "b", "bbbb");
      cache.Flush();

      Assert.That(cache.Size, Is.EqualTo(8));

      AssertAbsent("a");
      AssertValue("b", "b", "bbbb");
      AssertValue("c", "c", "cc");
    }

    [Test]
    public void EvictionHonorsLruFromCurrentSession() {
      cache.Dispose();
      cache = DiskLruCache.Open(directory, AppVersion, 2, 10);
      Set("a", "a", "a");
      Set("b", "b", "b");
      Set("c", "c", "c");
      Set("d", "d", "d");
      Set("e", "e", "e");

      cache.Get("b").Dispose(); // 'B' is now least recently used.

      // Causing the size to grow to 12 should evict 'A'.
      Set("f", "f", "f");

      // Causing the size to grow to 12 should evict 'C'.
      Set("g", "g", "g");

      cache.Flush();

      Assert.That(cache.Size, Is.EqualTo(10));

      AssertAbsent("a");
      AssertValue("b", "b", "b");
      AssertAbsent("c");
      AssertValue("d", "d", "d");
      AssertValue("e", "e", "e");
      AssertValue("f", "f", "f");
    }

    [Test]
    public void EvictionHonorsLruFromPreviousSession() {
      Set("a", "a", "a");
      Set("b", "b", "b");
      Set("c", "c", "c");
      Set("d", "d", "d");
      Set("e", "e", "e");
      Set("f", "f", "f");

      cache.Get("b").Dispose(); // 'B' is now least recently used.
      Assert.That(cache.Size, Is.EqualTo(12));

      cache.Flush();
      cache = DiskLruCache.Open(directory, AppVersion, 2, 10);

      Set("g", "g", "g");
      cache.Flush();

      Assert.That(cache.Size, Is.EqualTo(10));

      AssertAbsent("a");
      AssertValue("b", "b", "b");
      AssertAbsent("c");
      AssertValue("d", "d", "d");
      AssertValue("e", "e", "e");
      AssertValue("f", "f", "f");
      AssertValue("g", "g", "g");
    }

    [Test]
    public void CacheSingleEntryOfSizeGreaterThanMaxSize() {
      cache.Dispose();
      cache = DiskLruCache.Open(directory, AppVersion, 2, 10);
      Set("a", "aaaaa", "aaaaaa"); // size=11
      cache.Flush();
      AssertAbsent("a");
    }

    [Test]
    public void CacheSingleValueOfSizeGreaterThanMaxSize() {
      cache.Dispose();
      cache = DiskLruCache.Open(directory, AppVersion, 2, 10);
      Set("a", "aaaaaaaaaaa", "a"); // size=12
      cache.Flush();
      AssertAbsent("a");
    }

    [Test]
    public void ConstructorDoesNotAllowZeroCacheSize() {
      Assert.Catch<ArgumentException>(() => {
        DiskLruCache.Open(directory, AppVersion, 2, 0);
      });
    }

    [Test]
    public void ConstructorDoesNotAllowZeroValuesPerEntry() {
      Assert.Catch<ArgumentException>(() => {
        DiskLruCache.Open(directory, AppVersion, 0, 10);
      });
    }

    [Test]
    public void RemoveAbsentElement() {
      cache.Remove("a");
    }

    [Test]
    public void ReadingTheSameStreamMultipleTimes() {
      Set("a", "a", "b");

      var snapshot = cache.Get("a");
      Assert.That(snapshot.ReaderAt(0), Is.SameAs(snapshot.ReaderAt(0)));
      snapshot.Dispose();
    }

    [Test]
    public void RebuildJournalOnRepeatedReads() {
      Set("a", "a", "a");
      Set("b", "b", "b");

      long lastJournalLength = 0;
      var maxReads = 2000;

      while (true) {
        journalFile.Refresh();
        var journalLength = journalFile.Length;

        AssertValue("a", "a", "a");
        AssertValue("b", "b", "b");

        if (journalLength < lastJournalLength) {
          Assert.Pass($"Journal compacted from {lastJournalLength} bytes to {journalLength} bytes");
          break;
        }

        lastJournalLength = journalLength;
        maxReads--;

        if (maxReads <= 0) {
          Assert.Fail("exceeded max reads but journal was not rebuilt");
        }
      }
    }

    [Test]
    public void RebuildJournalOnRepeatedEdits() {
      long lastJournalLength = 0;
      var maxWrites = 2000;

      while (true) {
        journalFile.Refresh();
        var journalLength = journalFile.Length;

        Set("a", "a", "a");
        Set("b", "b", "b");

        if (journalLength < lastJournalLength) {
          Assert.Pass($"Journal compacted from {lastJournalLength} bytes to {journalLength} bytes");
          break;
        }

        lastJournalLength = journalLength;
        maxWrites--;

        if (maxWrites <= 0) {
          Assert.Fail("exceeded max writes but journal was not rebuilt");
        }
      }

      // Sanity check that a rebuilt journal behaves normally.
      AssertValue("a", "a", "a");
      AssertValue("b", "b", "b");
    }

    [Test]
    public void RebuildJournalOnRepeatedReadsWithOpenAndClose() {
      Set("a", "a", "a");
      Set("b", "b", "b");

      long lastJournalLength = 0;
      var maxReads = 2000;

      while (true) {
        journalFile.Refresh();
        var journalLength = journalFile.Length;

        AssertValue("a", "a", "a");
        AssertValue("b", "b", "b");

        cache.Dispose();
        cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

        if (journalLength < lastJournalLength) {
          Assert.Pass($"Journal compacted from {lastJournalLength} bytes to {journalLength} bytes");
          break;
        }

        lastJournalLength = journalLength;
        maxReads--;

        if (maxReads <= 0) {
          Assert.Fail("exceeded max reads but journal was not rebuilt");
        }
      }
    }

    [Test]
    public void RebuildJournalOnRepeatedEditsWithOpenAndClose() {
      long lastJournalLength = 0;
      var maxWrites = 2000;

      while (true) {
        journalFile.Refresh();
        var journalLength = journalFile.Length;

        Set("a", "a", "a");
        Set("b", "b", "b");

        cache.Dispose();
        cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

        if (journalLength < lastJournalLength) {
          Assert.Pass($"Journal compacted from {lastJournalLength} bytes to {journalLength} bytes");
          break;
        }

        lastJournalLength = journalLength;
        maxWrites--;

        if (maxWrites <= 0) {
          Assert.Fail("exceeded max writes but journal was not rebuilt");
        }
      }
    }

    [Test]
    public void RestoreBackupFile() {
      var creator = cache.Edit("k1");
      creator.PutAt(0, "ABC");
      creator.PutAt(1, "DE");
      creator.Commit();
      cache.Dispose();

      if (journalBackupFile.Exists) {
        journalBackupFile.Delete();
      }

      File.Move(journalFile.FullName, journalBackupFile.FullName);
      Assert.That(journalFile.Exists, Is.False);

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      var snapshot = cache.Get("k1");

      Assert.That(snapshot, Is.Not.Null);
      Assert.That(snapshot.StringAt(0), Is.EqualTo("ABC"));
      Assert.That(snapshot.LengthAt(0), Is.EqualTo(3));
      Assert.That(snapshot.StringAt(1), Is.EqualTo("DE"));
      Assert.That(snapshot.LengthAt(1), Is.EqualTo(2));

      journalFile.Refresh();

      Assert.That(journalBackupFile.Exists, Is.False);
      Assert.That(journalFile.Exists, Is.True);
    }

    [Test]
    public void JournalFileIsPreferredOverBackupFile() {
      var creator = cache.Edit("k1");
      creator.PutAt(0, "ABC");
      creator.PutAt(1, "DE");
      creator.Commit();
      cache.Flush();

      File.Copy(journalFile.FullName, journalBackupFile.FullName);

      creator = cache.Edit("k2");
      creator.PutAt(0, "F");
      creator.PutAt(1, "GH");
      creator.Commit();
      cache.Dispose();

      Assert.That(journalFile.Exists, Is.True);
      Assert.That(journalBackupFile.Exists, Is.True);

      cache = DiskLruCache.Open(directory, AppVersion, 2, int.MaxValue);

      var snapshotA = cache.Get("k1");

      Assert.That(snapshotA.StringAt(0), Is.EqualTo("ABC"));
      Assert.That(snapshotA.LengthAt(0), Is.EqualTo(3));
      Assert.That(snapshotA.StringAt(1), Is.EqualTo("DE"));
      Assert.That(snapshotA.LengthAt(1), Is.EqualTo(2));

      var snapshotB = cache.Get("k2");

      Assert.That(snapshotB.StringAt(0), Is.EqualTo("F"));
      Assert.That(snapshotB.LengthAt(0), Is.EqualTo(1));
      Assert.That(snapshotB.StringAt(1), Is.EqualTo("GH"));
      Assert.That(snapshotB.LengthAt(1), Is.EqualTo(2));

      Assert.That(journalBackupFile.Exists, Is.True);
      Assert.That(journalFile.Exists, Is.True);
    }

    [Test]
    [Ignore("Not Implemented")]
    public void OpenCreatesDirectoryIfNecessary() { }

    [Test]
    public void FileDeletedExternally() {
      Set("a", "a", "a");
      GetCleanFile("a", 1).Delete();
      Assert.That(cache.Get("a"), Is.Null);
    }

    [Test]
    public void EditSameVersion() {
      Set("a", "a", "a");

      var snapshot = cache.Get("a");
      var editor = snapshot.Edit();
      editor.PutAt(1, "a2");
      editor.Commit();

      AssertValue("a", "a", "a2");
    }

    [Test]
    public void EditSnapshotAfterChangeAborted() {
      Set("a", "a", "a");

      var snapshot = cache.Get("a");
      var toAbort = snapshot.Edit();
      toAbort.PutAt(0, "b");
      toAbort.Abort();

      var editor = snapshot.Edit();
      editor.PutAt(1, "a2");
      editor.Commit();

      AssertValue("a", "a", "a2");
    }

    [Test]
    public void EditSnapshotAfterChangeCommitted() {
      Set("a", "a", "a");
      var snapshot = cache.Get("a");
      var toAbort = snapshot.Edit();
      toAbort.PutAt(0, "b");
      toAbort.Commit();
      Assert.That(snapshot.Edit(), Is.Null);
    }

    [Test]
    public void EditSinceEvicted() {
      cache.Dispose();
      cache = DiskLruCache.Open(directory, AppVersion, 2, 10);
      Set("a", "aa", "aaa"); // size 5
      var snapshot = cache.Get("a");
      Set("b", "bb", "bbb"); // size 5
      Set("c", "cc", "ccc"); // size 5; will evict 'A'
      cache.Flush();
      Assert.That(snapshot.Edit(), Is.Null);
    }

    [Test]
    public void EditSinceEvictedAndRecreated() {
      cache.Dispose();
      cache = DiskLruCache.Open(directory, AppVersion, 2, 10);
      Set("a", "aa", "aaa"); // size 5
      var snapshot = cache.Get("a");
      Set("b", "bb", "bbb"); // size 5
      Set("c", "cc", "ccc"); // size 5; will evict 'A'
      Set("a", "a", "aaaa"); // size 5; will evict 'B'
      cache.Flush();
      Assert.That(snapshot.Edit(), Is.Null);
    }

    [Test]
    public void AggressiveClearingHandlesWrite() {
      directory.Delete(true);

      Set("a", "a", "a");
      AssertValue("a", "a", "a");
    }

    [Test]
    public void AggressiveClearingHandlesEdit() {
      Set("a", "a", "a");
      var a = cache.Get("a").Edit();
      directory.Delete(true);
      a.PutAt(1, "a2");
      a.Commit();
    }

    [Test]
    public void RemoveHandlesMissingFile() {
      Set("a", "a", "a");
      GetCleanFile("a", 0).Delete();
      cache.Remove("a");
    }

    [Test]
    public void AggressiveClearingHandlesPartialEdit() {
      Set("a", "a", "a");
      Set("b", "b", "b");
      var a = cache.Get("a").Edit();
      a.PutAt(0, "a1");
      directory.Delete(true);
      a.PutAt(1, "a2");
      a.Commit();
      Assert.That(cache.Get("a"), Is.Null);
    }

    [Test]
    public void AggressiveClearingHandlesRead() {
      directory.Delete(true);
      Assert.That(cache.Get("a"), Is.Null);
    }

    private static void AssertInoperable(Editor editor) {
      Assert.Catch<InvalidOperationException>(() => {
        editor.StringAt(0);
      });

      Assert.Catch<InvalidOperationException>(() => {
        editor.PutAt(0, "A");
      });

      Assert.Catch<InvalidOperationException>(() => {
        editor.ReaderInstanceAt(0);
      });

      Assert.Catch<InvalidOperationException>(() => {
        editor.WriterInstanceAt(0);
      });

      Assert.Catch<InvalidOperationException>(() => {
        editor.Commit();
      });

      Assert.Catch<InvalidOperationException>(() => {
        editor.Abort();
      });
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
          .Split(new[] { Environment.NewLine }, StringSplitOptions.None)
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
      editor.PutAt(0, value0);
      editor.PutAt(1, value1);
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

      Assert.That(snapshot.StringAt(0), Is.EqualTo(value0));
      Assert.That(snapshot.LengthAt(0), Is.EqualTo(value0.Length));
      Assert.That(snapshot.StringAt(1), Is.EqualTo(value1));
      Assert.That(snapshot.LengthAt(1), Is.EqualTo(value1.Length));

      Assert.That(GetCleanFile(key, 0).Exists, Is.True);
      Assert.That(GetCleanFile(key, 1).Exists, Is.True);

      snapshot.Dispose();
    }
  }
}
