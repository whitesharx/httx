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

namespace Httx.Caches.Disk {
  /// <summary>
  /// Edits the values for an entry.
  /// </summary>
  public class Editor {
    private readonly Entry entry;
    private readonly bool[] written;
    private readonly DiskLruCache parent;

    private bool hasErrors;
    private bool committed;

    public Editor(Entry entry, DiskLruCache parent) {
      this.entry = entry;
      this.parent = parent;

      written = entry.Readable ? null : new bool[parent.ValueCount];
    }

    /// <summary>
    /// Returns an unbuffered input stream to read the last committed value,
    /// or null if no value has been committed.
    /// </summary>
    public Stream NewInputStream(int index) {
      lock (parent) {
        if (this != entry.CurrentEditor) {
          throw new InvalidOperationException();
        }

        if (!entry.Readable) {
          return null;
        }

        try {
          return entry.GetCleanFile(index).OpenRead();
        } catch (FileNotFoundException) {
          return null;
        }
      }
    }

    /// <summary>
    /// Returns the last committed value as a string, or null if no value
    /// has been committed.
    /// </summary>
    public string GetString(int index) {
      var inputStream = NewInputStream(index);
      return null != inputStream ? new StreamReader(inputStream).ReadToEnd() : null;
    }

    /// <summary>
    /// Returns a new unbuffered output stream to write the value at
    /// index. If the underlying output stream encounters errors
    /// when writing to the filesystem, this edit will be aborted when
    /// commit is called. The returned output stream does not throw
    /// IOExceptions.
    /// </summary>
    public Stream NewOutputStream(int index) {
      if (index < 0 || index >= parent.ValueCount) {
        throw new ArgumentException($"Expected index {index} to "
          + "be greater than 0 and less than the maximum value count "
          + $"of {parent.ValueCount}");
      }

      lock (parent) {
        if (this != entry.CurrentEditor) {
          throw new InvalidOperationException();
        }

        if (!entry.Readable) {
          written[index] = true;
        }

        var dirtyFile = entry.GetDirtyFile(index);
        Stream outputStream;

        try {
          outputStream = dirtyFile.OpenWrite();
        } catch (DirectoryNotFoundException) {
          // Attempt to recreate the cache directory.
          // directory.mkdirs(); parent.Directory...

          try {
            outputStream = dirtyFile.OpenWrite();
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
    public void Set(int index, string value) {
      using (var writer = new StreamWriter(NewOutputStream(index))) {
        writer.Write(value);
      }
    }

    /// <summary>
    /// Commits this edit so it is visible to readers.  This releases the
    /// edit lock so another edit may be started on the same key.
    /// </summary>
    public void Commit() {
      if (hasErrors) {
        parent.CompleteEdit(this, false);
        parent.Remove(entry.Key); // The previous entry is stale.
      } else {
        parent.CompleteEdit(this, true);
      }

      committed = true;
    }

    /// <summary>
    /// Aborts this edit. This releases the edit lock so another edit may be
    /// started on the same key.
    /// </summary>
    public void Abort() {
      parent.CompleteEdit(this, false);
    }

    public void AbortUnlessCommitted() {
      if (!committed) {
        try {
          Abort();
        } catch (IOException) {

        }
      }
    }

    // TODO: FaultHidingOutputStream
  }
}
