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

namespace Httx.Requests.Awaiters.Async {
  public class AsyncOperationQueue : IAsyncOperation {
    private readonly Queue<Func<IAsyncOperation, IAsyncOperation>> operationsQueue;
    private IAsyncOperation currentOperation;

    public AsyncOperationQueue(params Func<IAsyncOperation, IAsyncOperation>[] operations) {
      operationsQueue = new Queue<Func<IAsyncOperation, IAsyncOperation>>(operations);
      ExecuteNext(null);
    }

    public object Result { get; private set; }

    public bool Done { get; private set; }

    public float Progress => currentOperation?.Progress ?? 1.0f;

    public event Action OnComplete;

    private void ExecuteNext(IAsyncOperation previous) {
      if (0 == operationsQueue.Count) {
        Result = currentOperation?.Result;
        Done = true;
        OnComplete?.Invoke();

        return;
      }

      var nextOperationFunc = operationsQueue.Dequeue();
      var nextOperation = nextOperationFunc(previous);

      if (nextOperation.Done) {
        ExecuteNext(currentOperation);
      } else {
        nextOperation.OnComplete += () => ExecuteNext(currentOperation);
      }

      currentOperation = nextOperation;
    }
  }
}
