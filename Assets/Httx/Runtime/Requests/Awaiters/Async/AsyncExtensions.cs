// Copyright (c) 2021 Sergey Ivonchik
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
using JetBrains.Annotations;

namespace Httx.Requests.Awaiters.Async {
  public static class AsyncExtensions {
    [CanBeNull]
    public static T UnsafeResult<T>(this IAsyncOperation operation) where T : class => operation?.Result as T;

    public static T SafeResult<T>(this IAsyncOperation operation) where T : class {
      try {
        return (T)operation.Result;
      } catch (InvalidCastException e) {
        var operationType = operation.GetType().Name;
        var resultType = null == operation.Result ? "null" : operation.Result.GetType().Name;
        var castType = typeof(T).Name;
        var message = $"can't cast operation {operationType} result of type {resultType} to {castType}";

        throw new InvalidCastException(message, e);
      }
    }
  }
}
