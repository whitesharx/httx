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
  /// Direct port of Disk LRU Cache from Android project.
  ///
  /// Copyright 2012 Jake Wharton
  /// Copyright 2011 The Android Open Source Project
  ///
  /// Differences:
  ///  - Object classes separated from each other
  ///  - Public API changed to apply to .net style
  ///  - LinkedList replaced by custom LinkedDictionary
  ///  - StrictLineReader replaced by standard .net *ReadLine
  ///  - Util class replaced by standard .net handles
  ///  - Public, synchronous Evict method instead of worker thread,
  /// you need to manage it manually, as you decide.
  ///
  /// <see cref="https://github.com/JakeWharton/DiskLruCache"/>
  /// <seealso cref="http://bit.ly/2S5O2px"/>
  /// </summary>
  public class DiskLruCache : IDisposable {
    public const string JournalFile = "journal";
    public const string JournalFileTemp = "journal.tmp";
    public const string JournalFileBackup = "journal.bkp";
    public const string Magic = "libcore.io.DiskLruCache";
    public const string Version1 = "1";

    private const long AnySequenceNumber = -1;
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

    private readonly object evictLock = new object();
    private readonly Regex legalKeyPattern = new Regex("^[a-z0-9_-]{1,120}$");
    private readonly LinkedDictionary<string, UnsafeEntry> lruEntries =
      new LinkedDictionary<string, UnsafeEntry>();

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
      if (maxSize <= 0) {
        throw new ArgumentException("maxSize <= 0");
      }

      if (valueCount <= 0) {
        throw new ArgumentException("valueCount <= 0");
      }

      var backupFile = new FileInfo(Path.Combine(directory.FullName, JournalFileBackup));

      // If a bkp file exists, use it instead.
      if (backupFile.Exists) {
        var journalFile = new FileInfo(Path.Combine(directory.FullName, JournalFile));

        // If journal file also exists just delete backup file.
        if (journalFile.Exists) {
          backupFile.Delete();
        } else {
          RenameTo(backupFile, journalFile, false);
        }
      }

      var cache = new DiskLruCache(directory, appVersion, valueCount, maxSize);

      if (cache.journalFile.Exists) {
        try {
          cache.ReadJournal();
          cache.ProcessJournal();

          return cache;
        } catch (IOException) {
          // Journal is corrupt
          cache.Delete();
        }
      }

      // Create a new empty cache.
      System.IO.Directory.CreateDirectory(directory.FullName);
      cache = new DiskLruCache(directory, appVersion, valueCount, maxSize);
      cache.RebuildJournal();

      return cache;
    }

    public static DiskLruCache Open(string path, int appVersion, long maxSize = int.MaxValue) {
      return Open(new DirectoryInfo(path), appVersion, 1, maxSize);
    }

    private void ReadJournal() {
      using (var reader = new StreamReader(journalFile.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite))) {
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
          } catch (EndOfStreamException) {
            break;
          }
        }

        redundantOpCount = lineCount - lruEntries.Count;
      }

      // XXX: Original, reader.hasUnterminatedLine()
      // If we ended on a truncated line, rebuild the journal before appending to it.

      journalWriter = new StreamWriter(journalFile.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
    }

    private void ReadJournalLine(string line) {
      if (null == line) {
        throw new EndOfStreamException();
      }

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
        var length = secondSpace - keyBegin;
        key = line.Substring(keyBegin, length);
      }

      lruEntries.TryGetValue(key, out var entry);

      if (null == entry) {
        entry = new UnsafeEntry(key, Directory, ValueCount);
        lruEntries[key] = entry;
      }

      if (secondSpace != -1 && firstSpace == CleanFlag.Length && line.StartsWith(CleanFlag)) {
        var parts = line.Substring(secondSpace + 1).Split(' ');

        entry.UnsafeReadable = true;
        entry.UnsafeCurrentEditor = null;
        entry.SetLengths(parts);
      } else if (secondSpace == -1 && firstSpace == DirtyFlag.Length && line.StartsWith(DirtyFlag)) {
        entry.UnsafeCurrentEditor = new Editor(entry, this);
      } else if (secondSpace == -1 && firstSpace == ReadFlag.Length && line.StartsWith(ReadFlag)) {
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
        if (null == entry.UnsafeCurrentEditor) {
          for (var i = 0; i < ValueCount; i++) {
            size += entry.UnsafeFileLengths[i];
          }
        } else {
          entry.UnsafeCurrentEditor = null;

          for (var i = 0; i < ValueCount; i++) {
            DeleteIfExists(entry.CleanFileAt(i));
            DeleteIfExists(entry.DirtyFileAt(i));
          }

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

      using (var writer = new StreamWriter(journalFileTmp.Open(FileMode.Create, FileAccess.Write, FileShare.ReadWrite))) {
        writer.WriteLine(Magic);
        writer.WriteLine(Version1);
        writer.WriteLine(appVersion.ToString());
        writer.WriteLine(ValueCount.ToString());
        writer.WriteLine();

        foreach (var entry in lruEntries.Values) {
          writer.WriteLine(null != entry.UnsafeCurrentEditor
            ? $"{DirtyFlag} {entry.Key}"
            : $"{CleanFlag} {entry.Key}{entry.GetLengths()}");
        }
      }

      if (journalFile.Exists) {
        RenameTo(journalFile, journalFileBackup, true);
      }

      RenameTo(journalFileTmp, journalFile, false);
      journalFileBackup.Delete();

      journalWriter = new StreamWriter(journalFile.Open(FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
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

      if (!entry.UnsafeReadable) {
        return null;
      }

      // Open all streams eagerly to guarantee that we see a single published
      // snapshot. If we opened streams lazily then the streams could come
      // from different edits.
      var ins = new Stream[ValueCount];

      try {
        for (var i = 0; i < ValueCount; i++) {
          ins[i] = entry.CleanFileAt(i).OpenRead();
        }
      } catch (FileNotFoundException) {
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
      journalWriter.Flush();

      if (JournalRebuildRequired()) {
        Evict();
      }

      return new Snapshot(key, entry.UnsafeSequenceNumber, ins, entry.UnsafeFileLengths, this);
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
        && (null == entry || entry.UnsafeSequenceNumber != expectedSequenceNumber)) {
        return null; // Snapshot is stale.
      }

      if (null == entry) {
        entry = new UnsafeEntry(key, Directory, ValueCount);
        lruEntries[key] = entry;
      } else if (null != entry.UnsafeCurrentEditor) {
        return null; // Another edit is in progress.
      }

      var editor = new Editor(entry, this);
      entry.UnsafeCurrentEditor = editor;

      // Flush the journal before creating files to prevent file leaks.
      journalWriter.WriteLine($"{DirtyFlag} {key}");
      journalWriter.Flush();

      return editor;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void UnsafeCompleteEdit(Editor editor, bool success) {
      var entry = editor.Entry;

      if (entry.UnsafeCurrentEditor != editor) {
        throw new InvalidOperationException();
      }

      // If this edit is creating the entry for the first
      // time, every index must have a value.
      if (success && !entry.UnsafeReadable) {
        for (var i = 0; i < ValueCount; i++) {
          if (!editor.WrittenAt(i)) {
            editor.Abort();
            throw new InvalidOperationException($"newly created entry didn't create value for index {i}");
          }

          if (!entry.DirtyFileAt(i).Exists) {
            editor.Abort();
            return;
          }
        }
      }

      for (var i = 0; i < ValueCount; i++) {
        var dirty = entry.DirtyFileAt(i);

        if (success) {
          if (dirty.Exists) {
            var clean = entry.CleanFileAt(i);

            if (File.Exists(clean.FullName)) {
              File.Delete(clean.FullName);
            }

            File.Move(dirty.FullName, clean.FullName);

            var oldLength = entry.UnsafeFileLengths[i];
            var newLength = clean.Length;

            entry.UnsafeFileLengths[i] = newLength;
            size = size - oldLength + newLength;
          }
        } else {
          DeleteIfExists(dirty);
        }
      }

      redundantOpCount++;
      entry.UnsafeCurrentEditor = null;

      if (entry.UnsafeReadable || success) {
        entry.UnsafeReadable = true;
        journalWriter.WriteLine($"{CleanFlag} {entry.Key}{entry.GetLengths()}");

        if (success) {
          entry.UnsafeSequenceNumber = nextSequenceNumber++;
        }
      } else {
        lruEntries.Remove(entry.Key);
        journalWriter.WriteLine($"{RemoveFlag} {entry.Key}");
      }

      journalWriter.Flush();

      if (size > maxSize || JournalRebuildRequired()) {
        Evict();
      }
    }

    /// <summary>
    /// Drops the entry for key if it exists and can be removed. Entries
    /// actively being edited cannot be removed. Returns true if an entry was removed.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool Remove(string key) {
      CheckNotClosed();
      ValidateKey(key);

      lruEntries.TryGetValue(key, out var entry);

      if (null == entry || null != entry.UnsafeCurrentEditor) {
        return false;
      }

      for (var i = 0; i < ValueCount; i++) {
        var file = entry.CleanFileAt(i);

        if (file.Exists) {
          try {
            file.Delete();
          } catch (Exception) {
            throw new IOException($"failed to delete {file}");
          }
        }

        size -= entry.UnsafeFileLengths[i];
        entry.UnsafeFileLengths[i] = 0;
      }

      redundantOpCount++;
      journalWriter.WriteLine($"{RemoveFlag} {key}");
      lruEntries.Remove(key);

      if (JournalRebuildRequired()) {
        Evict();
      }

      return true;
    }

    /// <summary>
    /// Force buffered operations to the filesystem.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Flush() {
      CheckNotClosed();
      TrimToSize();
      journalWriter.Flush();
    }

    /// <summary>
    /// Closes this cache. Stored values will remain on the filesystem.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Dispose() {
      if (null == journalWriter) {
        return; // Already closed.
      }

      var entries = lruEntries.Values.ToList();

      foreach (var entry in entries) {
        entry.UnsafeCurrentEditor?.Abort();
      }

      TrimToSize();

      journalWriter.Close();
      journalWriter = null;
    }

    /// <summary>
    /// Closes the cache and deletes all of its stored values. This will delete
    /// all files in the cache directory including files that weren't created by
    /// the cache.
    /// </summary>
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete() {
      Dispose();
      Directory.Delete(true);
    }

    /// <summary>
    /// Returns true if this cache has been closed.
    /// </summary>
    public bool Closed {
      [MethodImpl(MethodImplOptions.Synchronized)]
      get => null != journalWriter;
    }

    public int ValueCount { get; }

    /// <summary>
    /// Returns the directory where this cache stores its data.
    /// </summary>
    public DirectoryInfo Directory { get; }

    public delegate void EvictCompleteHandler();

    public event EvictCompleteHandler OnEvictComplete;

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
        Evict();
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

    /// <summary>
    /// Original cache implementation uses a single background thread to evict entries.
    /// Here, we simply do this synchronously to keep things clean and concise for now.
    /// </summary>
    public void Evict() { // TODO: Only signal evict required???
      lock (evictLock) {
        if (null == journalWriter) {
          return;
        }

        TrimToSize();

        if (!JournalRebuildRequired()) {
          return;
        }

        RebuildJournal();
        redundantOpCount = 0;
        OnEvictComplete?.Invoke();
      }
    }

    private void CheckNotClosed() {
      if (null == journalWriter) {
        throw new InvalidOperationException("cache is closed");
      }
    }

    private void ValidateKey(string key) {
      try {
        var match = legalKeyPattern.Match(key);

        if (!match.Success) {
          throw new ArgumentException($"keys must match regex {legalKeyPattern}: {key}");
        }
      } catch (Exception e) {
        throw new ArgumentException($"invalid key: {e.Message}");
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

    private void TrimToSize() {
      while (size > maxSize) {
        Remove(lruEntries.First().Key);
      }
    }

    private static void DeleteIfExists(FileSystemInfo file) {
      if (file.Exists) {
        file.Delete();
      }
    }

    private static void RenameTo(FileSystemInfo from, FileSystemInfo to, bool deleteDestination) {
      if (deleteDestination) {
        DeleteIfExists(to);
      }

      // TODO: XXX: Always forced to remove dst?
      if (File.Exists(to.FullName)) {
        File.Delete(to.FullName);
      }

      File.Move(from.FullName, to.FullName);
    }
  }
}
