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

using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Httx.Caches.Collections {
  /// <summary>
  /// Works as equivalent of LinkedHashMap from JDK if accessOrder = true.
  /// </summary>
  public class LinkedDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
    private readonly Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>> impl;
    private readonly LinkedList<KeyValuePair<TKey, TValue>> policy;

    public LinkedDictionary() {
      impl = new Dictionary<TKey, LinkedListNode<KeyValuePair<TKey, TValue>>>();
      policy = new LinkedList<KeyValuePair<TKey, TValue>>();
    }

    public void Add(TKey key, TValue value) {
      var pair = new KeyValuePair<TKey, TValue>(key, value);
      var node = new LinkedListNode<KeyValuePair<TKey, TValue>>(pair);

      policy.AddLast(node);
      impl.Add(key, node);
    }

    public bool Remove(TKey key) {
      impl.TryGetValue(key, out var node);

      if (null == node) {
        return false;
      }

      policy.Remove(node);
      return impl.Remove(key);
    }

    public bool TryGetValue(TKey key, out TValue value) {
      impl.TryGetValue(key, out var node);

      if (null != node) {
        policy.Remove(node);
        policy.AddLast(node);

        value = node.Value.Value;
        return true;
      }

      value = default;
      return false;
    }

    public TValue this[TKey key] {
      get {
        TryGetValue(key, out var value);

        if (null == value) {
          throw new KeyNotFoundException();
        }

        return value;
      }

      set => Add(key, value);
    }

    public void Clear() {
      impl.Clear();
      policy.Clear();
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => policy.CopyTo(array, arrayIndex);
    public bool Contains(KeyValuePair<TKey, TValue> p) => policy.Contains(p);
    public bool ContainsKey(TKey key) => impl.ContainsKey(key);
    public void Add(KeyValuePair<TKey, TValue> p) => Add(p.Key, p.Value);
    public bool Remove(KeyValuePair<TKey, TValue> p) => Remove(p.Key);
    public int Count => policy.Count;
    public bool IsReadOnly => false;
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => policy.GetEnumerator();
    public ICollection<TKey> Keys => policy.Select(p => p.Key).ToList();
    public ICollection<TValue> Values => policy.Select(p => p.Value).ToList();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  }
}
