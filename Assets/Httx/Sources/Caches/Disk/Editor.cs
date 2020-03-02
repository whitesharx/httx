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

namespace Httx.Caches.Disk {
  /// <summary>
  /// Edits the values for an entry.
  /// </summary>
  public class Editor {
    private readonly bool[] written;
    private readonly DiskLruCache parent;

    public Editor(UnsafeEntry entry, DiskLruCache parent) {
      this.parent = parent;

      Entry = entry;
      written = entry.UnsafeReadable ? null : new bool[parent.ValueCount];
    }

    /// <summary>
    /// Returns an unbuffered input stream to read the last committed value,
    /// or null if no value has been committed.
    /// </summary>
    public Stream ReaderInstanceAt(int index) {
      lock (parent) {
        if (this != Entry.UnsafeCurrentEditor) {
          throw new InvalidOperationException();
        }

        if (!Entry.UnsafeReadable) {
          return null;
        }

        try {
          return Entry.CleanFileAt(index).OpenRead();
        } catch (FileNotFoundException) {
          return null;
        }
      }
    }

    /// <summary>
    /// Returns the last committed value as a string, or null if no value
    /// has been committed.
    /// </summary>
    public string StringAt(int index) {
      var inputStream = ReaderInstanceAt(index);
      return null != inputStream ? new StreamReader(inputStream).ReadToEnd() : null;
    }

    /// <summary>
    /// Returns a new unbuffered output stream to write the value at
    /// index. If the underlying output stream encounters errors
    /// when writing to the filesystem, this edit will be aborted when
    /// commit is called. The returned output stream does not throw
    /// IOExceptions.
    /// </summary>
    public Stream WriterInstanceAt(int index) {
      if (index < 0 || index >= parent.ValueCount) {
        throw new ArgumentException($"Expected index {index} to "
          + "be greater than 0 and less than the maximum value count "
          + $"of {parent.ValueCount}");
      }

      lock (parent) {
        if (this != Entry.UnsafeCurrentEditor) {
          throw new InvalidOperationException();
        }

        if (!Entry.UnsafeReadable) {
          written[index] = true;
        }

        var dirtyFile = Entry.DirtyFileAt(index);
        Stream outputStream;

        try {
          outputStream = dirtyFile.Open(FileMode.Append, FileAccess.Write);
        } catch (DirectoryNotFoundException) {
          Directory.CreateDirectory(parent.Directory.FullName);

          try {
            outputStream = dirtyFile.Open(FileMode.Append, FileAccess.Write);
          } catch (DirectoryNotFoundException) {
            // We are unable to recover. Silently eat the writes.
            // return NULL_OUTPUT_STREAM;
            outputStream = null;
          }

        }

        return outputStream;
      }
    }

    /// <summary>
    /// Sets the value at index to value.
    /// </summary>
    public void SetAt(int index, string value) {
      using (var writer = new StreamWriter(WriterInstanceAt(index))) {
        writer.Write(value);
      }
    }

    /// <summary>
    /// Commits this edit so it is visible to readers.  This releases the
    /// edit lock so another edit may be started on the same key.
    /// </summary>
    public void Commit() {
      try {
        parent.UnsafeCompleteEdit(this, true);
      } catch (Exception) {
        parent.UnsafeCompleteEdit(this, false);
        parent.Remove(Entry.Key); // The previous entry is stale.
      }

      Committed = true;
    }

    /// <summary>
    /// Aborts this edit. This releases the edit lock so another edit may be
    /// started on the same key.
    /// </summary>
    public void Abort() {
      parent.UnsafeCompleteEdit(this, false);
    }

    public bool Committed { get; private set; }

    public UnsafeEntry Entry { get; }

    public IEnumerable<bool> Written => written;

    public bool WrittenAt(int index) => written[index];
  }
}
