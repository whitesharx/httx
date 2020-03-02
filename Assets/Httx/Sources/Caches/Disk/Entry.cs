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
using System.Text;

namespace Httx.Caches.Disk {
  // TODO: Immutabiliy, WeekRef to parent.
  public class Entry {
    private readonly int valueCount;
    private readonly DirectoryInfo directory;

    public Entry(string key, DirectoryInfo directory, int valueCount) {
      Key = key;
      this.directory = directory;
      this.valueCount = valueCount;

      Lengths = new long[valueCount];
    }

    public string GetLengths() {
      var result = new StringBuilder();

      foreach (var size in Lengths) {
        result.Append(' ').Append(size);
      }

      return result.ToString();
    }

    public void SetLengths(string[] strings) {
      if (strings.Length != valueCount) {
        throw new IOException($"unexpected journal line: [{string.Join(", ", strings)}]");
      }

      try {
        for (var i = 0; i < strings.Length; i++) {
          Lengths[i] = long.Parse(strings[i]);
        }
      } catch (FormatException) {
        throw new IOException($"unexpected journal line: [{string.Join(", ", strings)}]");
      }
    }

    public FileInfo GetCleanFile(int i) {
      return new FileInfo(Path.Combine(directory.FullName, $"{Key}.{i}"));
    }

    public FileInfo GetDirtyFile(int i) {
      return new FileInfo(Path.Combine(directory.FullName, $"{Key}.{i}.tmp"));
    }

    /** The ongoing edit or null if this entry is not being edited. */
    public Editor CurrentEditor { get; set; }

    public string Key { get; }

    /** Lengths of this entry's files. */
    public long[] Lengths { get; }

    /** True if this entry has ever been published. */
    public bool Readable { get; set; }

    /** The sequence number of the most recently committed edit to this entry. */
    public long SequenceNumber { get; set; }
  }
}
