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
  /// A snapshot of the values for an entry.
  /// </summary>
  public class Snapshot : IDisposable {
    private readonly string key;
    private readonly long sequenceNumber;
    private readonly Stream[] ins;
    private readonly long[] lengths;

    public Snapshot(string key, long sequenceNumber, Stream[] ins, long[] lengths) {
      this.key = key;
      this.sequenceNumber = sequenceNumber;
      this.ins = ins;
      this.lengths = lengths;
    }

    /// <summary>
    /// Returns an editor for this snapshot's entry, or null if either the
    /// entry has changed since this snapshot was created or if another edit
    /// is in progress.
    /// </summary>
    public Editor Edit() {
      throw new NotImplementedException();
      // return DiskLruCache.this.edit(key, sequenceNumber);
      return null;
    }

    /// <summary>
    /// Returns the unbuffered stream with the value for index.
    /// </summary>
    public Stream GetInputStream(int index) {
      return ins[index];
    }

    /// <summary>
    /// Returns the string value for index
    /// </summary>
    public string GetString(int index) {
      return new StreamReader(GetInputStream(index)).ReadToEnd();
    }

    /// <summary>
    /// Returns the byte length of the value for index
    /// </summary>
    public long GetLength(int index) {
      return lengths[index];
    }

    public void Dispose() {
      foreach (var s in ins) {
        // Util.closeQuietly(in);
        s.Dispose();
      }
    }
  }
}
