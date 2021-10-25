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
using System.Collections.Generic;
using Httx.Requests.Extensions;
using JetBrains.Annotations;

namespace Httx.Requests.Decorators {
  public class Callback<TRequest, TResponse> {
    [CanBeNull]
    public Action<TRequest> OnBeforeRequestSent { get; set; }

    [CanBeNull]
    public Action<TResponse> OnResponseReceived { get; set; }
  }

  public class Callback<TObject> : Callback<TObject, TObject> { }

  public class Hook<TRequest, TResponse> : BaseRequest {
    private readonly Callback<TRequest, TResponse> callbackObject;

    public Hook(IRequest next, Callback<TRequest, TResponse> callback) : base(next) {
      callbackObject = callback;
    }

    public override IEnumerable<KeyValuePair<string, object>> Headers =>
        new Dictionary<string, object> { [InternalHeaders.HookObject] = callbackObject };
  }

  public class Hook<TObject> : Hook<TObject, TObject> {
    public Hook(IRequest next, Callback<TObject, TObject> callback) : base(next, callback) { }
  }
}
