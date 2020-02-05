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
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Httx.Caches.Collections;

namespace Httx.Caches.Disk {
  /// <summary>
  /// Port of Disk LRU Cache from Android (c) Project
  ///
  /// <see cref="https://github.com/JakeWharton/DiskLruCache"/>
  /// <seealso cref="http://bit.ly/2S5O2px"/>
  /// </summary>
  public class DiskLruCache {
    private const string JournalFile = "journal";
    private const string JournalFileTemp = "journal.tmp";
    private const string JournalFileBackup = "journal.bkp";
    private const string Magic = "libcore.io.DiskLruCache";
    private const string Version1 = "1";
    private const long AnySequenceNumber = -1;

    private const string StringKeyPattern = "[a-z0-9_-]{1,120}";
    private readonly Regex LegalKeyPattern = new Regex(StringKeyPattern);

    private const string CleanFlag = "CLEAN";
    private const string DirtyFlag = "DIRTY";
    private const string RemoveFlag = "REMOVE";
    private const string ReadFlag = "READ";

    private readonly FileInfo journalFile;
    private readonly FileInfo journalFileTmp;
    private readonly FileInfo journalFileBackup;
    private readonly int appVersion;
    private long maxSize;
    private long size;
    private StreamWriter journalWriter;
    private int redundantOpCount;

    private readonly LinkedDictionary<string, Entry> lruEntries =
      new LinkedDictionary<string, Entry>();

    /// <summary>
    /// To differentiate between old and current snapshots, each entry is given
    /// a sequence number each time an edit is committed. A snapshot is stale if
    /// its sequence number is not equal to its entry's sequence number.
    /// </summary>
    private long nextSequenceNumber;

    private DiskLruCache(DirectoryInfo directory, int appVersion, int valueCount, long maxSize) {
      Directory = directory;
      ValueCount = valueCount;

      this.appVersion = appVersion;
      this.maxSize = maxSize;

      journalFile = new FileInfo(Path.Combine(directory.FullName, JournalFile));
      journalFileTmp = new FileInfo(Path.Combine(directory.FullName, JournalFileTemp));
      journalFileBackup = new FileInfo(Path.Combine(directory.FullName, JournalFileBackup));
    }

    /// <summary>
    /// Opens the cache in {@code directory}, creating a cache if none exists there.
    /// </summary>
    /// <param name="directory">a writable directory</param>
    /// <param name="appVersion">???</param>
    /// <param name="valueCount">the number of values per cache entry. Must be positive.</param>
    /// <param name="maxSize">the maximum number of bytes this cache should use to store</param>
    /// <returns></returns>
    public static DiskLruCache Open(DirectoryInfo directory, int appVersion, int valueCount, long maxSize) {
      var backupFile = new FileInfo(Path.Combine(directory.FullName, JournalFileBackup));

      // If a bkp file exists, use it instead.
      if (backupFile.Exists) {
        var journalFile = new FileInfo(Path.Combine(directory.FullName, JournalFileBackup));

        // If journal file also exists just delete backup file.
        if (journalFile.Exists) {
          backupFile.Delete();
        } else {
          // TODO: renameTo(backupFile, journalFile, false);
        }
      }

      var cache = new DiskLruCache(directory, appVersion, valueCount, maxSize);

      if (cache.journalFile.Exists) {
        try {

        } catch (IOException journalIsCorrupt) {
          // TODO: cache.Delete();
        }
      }

      // Create a new empty cache.
      System.IO.Directory.CreateDirectory(directory.FullName);
      cache = new DiskLruCache(directory, appVersion, valueCount, maxSize);
      // TODO: cache.RebuildJournal();

      return cache;
    }

    private void ReadJournal() {
      using (var reader = new StreamReader(journalFile.OpenRead())) {
        var magic = reader.ReadLine();
        var version = reader.ReadLine();
        var appVersionString = reader.ReadLine();
        var valueCountString = reader.ReadLine();
        var blank = reader.ReadLine();

        if (Magic != magic
          || Version1 != version
          || appVersion.ToString() != appVersionString
          || ValueCount.ToString() != valueCountString
          || string.Empty != blank) {
          throw new IOException($"unexpected journal header: [{magic}, {version}, {valueCountString}, {blank}");
        }

        var lineCount = 0;

        while (true) {
          try {
            ReadJournalLine(reader.ReadLine());
            lineCount++;
          } catch (Exception endOfJournal) {
            break;
          }
        }

        redundantOpCount = lineCount - lruEntries.Count;

        // XXX: Original, reader.hasUnterminatedLine()
        // If we ended on a truncated line, rebuild the journal before appending to it.

        journalWriter = new StreamWriter(journalFile.OpenWrite());
      }
    }

    private void ReadJournalLine(string line) {
      var firstSpace = line.IndexOf(' ');

      if (firstSpace == -1) {
        throw new IOException($"unexpected journal line: {line}");
      }

      var keyBegin = firstSpace + 1;
      var secondSpace = line.IndexOf(' ', keyBegin);

      string key;

      if (secondSpace == -1) {
        key = line.Substring(keyBegin);

        if (firstSpace == RemoveFlag.Length && line.StartsWith(RemoveFlag)) {
          lruEntries.Remove(key);
          return;
        }
      } else {
        key = line.Substring(keyBegin, secondSpace);
      }

      lruEntries.TryGetValue(key, out var entry);

      if (null == entry) {
        entry = new Entry(key, Directory, ValueCount);
        lruEntries[key] = entry;
      }

      if (secondSpace != -1 && firstSpace == CleanFlag.Length && line.StartsWith(CleanFlag)) {
        var parts = line.Substring(secondSpace + 1).Split(' ');

        entry.Readable = true;
        entry.CurrentEditor = null;
        entry.SetLengths(parts);
      } else if (secondSpace != -1 && firstSpace == DirtyFlag.Length && line.StartsWith(DirtyFlag)) {
        entry.CurrentEditor = new Editor(entry, this);
      } else if (secondSpace != -1 && firstSpace == ReadFlag.Length && line.StartsWith(ReadFlag)) {
        // This work was already done by calling lruEntries.get().
      } else {
        throw new IOException($"unexpected journal line: {line}");
      }
    }

    /// <summary>
    /// Computes the initial size and collects garbage as a part of opening the
    /// cache. Dirty entries are assumed to be inconsistent and will be deleted.
    /// </summary>
    private void ProcessJournal() {
      DeleteIfExists(journalFileTmp);

      var entries = lruEntries.Values.ToList();

      foreach (var entry in entries) {
        if (null == entry.CurrentEditor) {
          for (var i = 0; i < ValueCount; i++) {
            size += entry.Lengths[i];
          }
        } else {
          entry.CurrentEditor = null;

          for (var i = 0; i < ValueCount; i++) {
            DeleteIfExists(entry.GetCleanFile(i));
            DeleteIfExists(entry.GetDirtyFile(i));
          }

          // XXX: i.remove();
          lruEntries.Remove(entry.Key);
        }
      }
    }

    /// <summary>
    /// Creates a new journal that omits redundant information. This replaces the
    /// current journal if it exists.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    private void RebuildJournal() {
      journalWriter?.Close();

      using (var writer = new StreamWriter(journalFileTmp.OpenWrite())) {
        writer.WriteLine(Magic);
        writer.WriteLine(Version1);
        writer.WriteLine(appVersion.ToString());
        writer.WriteLine(ValueCount.ToString());
        writer.WriteLine();
        writer.WriteLine();

        foreach (var entry in lruEntries.Values) {
          if (null != entry.CurrentEditor) {
            writer.WriteLine($"{DirtyFlag} {entry.Key}");
          } else {
            writer.WriteLine($"{CleanFlag} {entry.Key} {entry.GetLengths()}");
          }
        }
      }

      if (journalFile.Exists) {
        RenameTo(journalFile, journalFileBackup, true);
      }

      RenameTo(journalFileTmp, journalFile, false);
      journalFileBackup.Delete();

      journalWriter = new StreamWriter(journalFile.OpenWrite());
    }


    /// <summary>
    /// Returns a snapshot of the entry named {@code key}, or null if it doesn't
    /// exist is not currently readable. If a value is returned, it is moved to
    /// the head of the LRU queue.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Snapshot Get(string key) {
      CheckNotClosed();
      ValidateKey(key);

      lruEntries.TryGetValue(key, out var entry);

      if (null == entry) {
        return null;
      }

      if (!entry.Readable) {
        return null;
      }

      // Open all streams eagerly to guarantee that we see a single published
      // snapshot. If we opened streams lazily then the streams could come
      // from different edits.
      var ins = new Stream[ValueCount];

      try {
        for (var i = 0; i < ValueCount; i++) {
          ins[i] = entry.GetCleanFile(i).OpenRead();
        }
      } catch (Exception e) when (e is DirectoryNotFoundException || e is UnauthorizedAccessException) {
        // A file must have been deleted manually!

        for (var i = 0; i < ValueCount; i++) {
          if (null != ins[i]) {
            ins[i].Close();
          } else {
            break;
          }
        }

        return null;
      }

      redundantOpCount++;
      journalWriter.WriteLine($"{ReadFlag} {key}");

      if (JournalRebuildRequired()) {
        // TODO: CleanUp
        // executorService.submit(cleanupCallable);
      }

      return new Snapshot(key, entry.SequenceNumber, ins, entry.Lengths);
    }



    /// <summary>
    /// Returns an editor for the entry named {@code key}, or null if another
    /// edit is in progress.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public Editor Edit(string key, long expectedSequenceNumber = AnySequenceNumber) {
      CheckNotClosed();
      ValidateKey(key);

      lruEntries.TryGetValue(key, out var entry);

      if (expectedSequenceNumber != AnySequenceNumber
        && (null == entry || entry.SequenceNumber != expectedSequenceNumber)) {
        return null; // Snapshot is stale.
      }

      if (null == entry) {
        entry = new Entry(key, Directory, ValueCount);
        lruEntries[key] = entry;
      } else if (null != entry.CurrentEditor) {
        return null; // Another edit is in progress.
      }

      var editor = new Editor(entry, this);
      entry.CurrentEditor = editor;

      // Flush the journal before creating files to prevent file leaks.
      journalWriter.WriteLine($"{DirtyFlag} {key}");
      journalWriter.Flush();

      return editor;
    }


    // TODO: Private???
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CompleteEdit(Editor editor, bool success) {
      var entry = editor.Entry;

      if (entry.CurrentEditor != editor) {
        throw new InvalidOperationException();
      }

      // If this edit is creating the entry for the first
      // time, every index must have a value.
      if (success && !entry.Readable) {
        for (var i = 0; i < ValueCount; i++) {
          // if (!editor.wri)
        }
      }


    }













    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool Remove(string key) {
      return true;
    }




    public int ValueCount { get; }

    /// <summary>
    /// Returns the directory where this cache stores its data.
    /// </summary>
    public DirectoryInfo Directory { get; }

    /// <summary>
    /// Changes the maximum number of bytes the cache can store and queues a job
    /// to trim the existing store, if necessary.
    ///
    /// Returns the maximum number of bytes that this cache should use to store
    /// its data.
    /// </summary>
    public long MaxSize {
      [MethodImpl(MethodImplOptions.Synchronized)]
      set {
        maxSize = value;

        // TODO: Clean Up?
        // Original: executorService.submit(cleanupCallable);
      }

      [MethodImpl(MethodImplOptions.Synchronized)]
      get => maxSize;
    }

    /// <summary>
    /// Returns the number of bytes currently being used to store the values in
    /// this cache. This may be greater than the max size if a background
    /// deletion is pending.
    /// </summary>
    public long Size {
      [MethodImpl(MethodImplOptions.Synchronized)]
      get => size;
    }

    private void CheckNotClosed() {
      if (null == journalWriter) {
        throw new InvalidOperationException("cache is closed");
      }
    }

    private void ValidateKey(string key) {
      var match = LegalKeyPattern.Match(key);

      if (!match.Success) {
        throw new InvalidOperationException($"keys must match regex {StringKeyPattern}: {key}");
      }
    }

    /// <summary>
    /// We only rebuild the journal when it will halve the size of the journal
    /// and eliminate at least 2000 ops.
    /// </summary>
    private bool JournalRebuildRequired() {
      const int redundantOpCompactThreshold = 2000;

      return redundantOpCount >= redundantOpCompactThreshold
        && redundantOpCount >= lruEntries.Count;
    }

    private static void DeleteIfExists(FileInfo file) {
      if (file.Exists) {
        file.Delete();
      }
    }

    private static void RenameTo(FileInfo from, FileInfo to, bool deleteDestination) {
      if (deleteDestination) {
        DeleteIfExists(to);
      }

      File.Move(from.FullName, to.FullName);
    }



  }
}
