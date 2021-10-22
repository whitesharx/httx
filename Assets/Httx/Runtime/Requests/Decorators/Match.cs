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
  public enum If {
    Match,
    NoneMatch,
    UnmodifiedSince,
    ModifiedSince
  }

  public class Condition {
    public Condition(If type, [CanBeNull] string value, Action<string> onValueChanged) {
      Type = type;
      Value = value;
      OnValueChanged = onValueChanged;
    }

    public If Type { get; }

    [CanBeNull]
    public string Value { get; }

    public Action<string> OnValueChanged { get; }
  }

  public class Match : BaseRequest {
    private readonly Condition matchCondition;

    public Match(IRequest next, Condition condition) : base(next) {
      matchCondition = condition;
    }

    public override IEnumerable<KeyValuePair<string, object>> Headers {
      get {
        var dictionary = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(matchCondition.Value)) {
          dictionary[TypeAsString(matchCondition.Type)] = matchCondition.Value;
        }

        dictionary[InternalHeaders.ConditionObject] = matchCondition;

        return dictionary;
      }
    }

    private static string TypeAsString(If type) {
      switch (type) {
        case If.Match:
          return "If-Match";
        case If.NoneMatch:
          return "If-None-Match";
        case If.ModifiedSince:
          return "If-Modified-Since";
        case If.UnmodifiedSince:
          return "If-Unmodified-Since";
        default:
          throw new NotImplementedException();
      }
    }
  }
}
