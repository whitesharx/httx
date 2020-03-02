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
  /// A snapshot of the values for an entry.
  /// </summary>
  public class Snapshot : IDisposable {
    private readonly string key;
    private readonly long sequenceNumber;
    private readonly Stream[] readStreams;
    private readonly long[] lengths;
    private readonly WeakReference<DiskLruCache> parentRef;

    public Snapshot(string key, long sequenceNumber, Stream[] readStreams, long[] lengths, DiskLruCache parent) {
      this.key = key;
      this.sequenceNumber = sequenceNumber;
      this.readStreams = readStreams;
      this.lengths = lengths;

      parentRef = new WeakReference<DiskLruCache>(parent);
    }

    /// <summary>
    /// Returns an editor for this snapshot's entry, or null if either the
    /// entry has changed since this snapshot was created or if another edit
    /// is in progress.
    /// </summary>
    public Editor Edit() {
      parentRef.TryGetTarget(out var parent);
      return parent?.Edit(key, sequenceNumber);
    }

    /// <summary>
    /// Returns the unbuffered stream with the value for index.
    /// </summary>
    public Stream ReaderAt(int index) {
      return readStreams[index];
    }

    /// <summary>
    /// Returns the unbuffered stream with the value for 0 index.
    /// </summary>
    public Stream Reader => readStreams[0];

    /// <summary>
    /// Returns the string value for index.
    /// </summary>
    public string StringAt(int index) {
      using (var reader = new StreamReader(ReaderAt(index))) {
        return reader.ReadToEnd();
      }
    }

    /// <summary>
    /// Returns the string value for 0 index.
    /// </summary>
    public string String => StringAt(0);

    /// <summary>
    /// Returns the byte array value for index.
    /// </summary>
    public IEnumerable<byte> BytesAt(int index) {
      byte[] bytes;

      using (var stream = new MemoryStream()) {
        ReaderAt(index).CopyTo(stream);
        bytes = stream.ToArray();
      }

      return bytes;
    }

    /// <summary>
    /// Returns the byte array value for 0 index.
    /// </summary>
    public IEnumerable<byte> Bytes => BytesAt(0);

    /// <summary>
    /// Returns the byte length of the value for index
    /// </summary>
    public long LengthAt(int index) {
      return lengths[index];
    }

    /// <summary>
    /// Returns the byte length of the value for 0 index.
    /// </summary>
    public long Length => LengthAt(0);

    public void Dispose() {
      foreach (var s in readStreams) {
        // XXX: Original: Util.closeQuietly(in);
        s.Dispose();
      }
    }
  }
}
