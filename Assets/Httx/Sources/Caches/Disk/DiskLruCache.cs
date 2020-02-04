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
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

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
        


      }
    }










    public int ValueCount { get; }
    public DirectoryInfo Directory { get; }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public bool Remove(string key) {
      return true;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void CompleteEdit(Editor editor, bool success) {

    }
  }
}
