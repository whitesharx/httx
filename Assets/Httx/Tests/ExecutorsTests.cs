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
using System.Linq;
using Httx.Requests.Executors;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Httx.Tests {
  public class ExecutorsTests {
    [UnitySetUp]
    [UsedImplicitly]
    public IEnumerator SetUp() {
      return HttxTestUtils.SetUpDefaultContext();
    }

    [UnityTearDown]
    [UsedImplicitly]
    public void TearDown() {
      HttxTestUtils.TearDownDefaultContext();
    }

    [UnityTest]
    public IEnumerator TestHead() {
      const string url = RequestEndpoints.TextUrl;
      var length = RequestEndpoints.TextResponse.Length;

      return HttxTestUtils.Execute(new Head(url), headers => {
        var contentLengthHeader = headers.FirstOrDefault(h => h.Key == "Content-Length");
        var contentLength = contentLengthHeader.Value;

        Assert.That(contentLengthHeader, Is.Not.Null);
        Assert.That(contentLength, Is.Not.Null);
        Assert.That(contentLength, Is.EqualTo($"{length}"));
      });
    }

    [UnityTest]
    public IEnumerator TestLength() {
      const string url = RequestEndpoints.TextUrl;
      var length = (long) RequestEndpoints.TextResponse.Length;

      return HttxTestUtils.Execute(new Length(url), contentLength => {
        Assert.That(contentLength, Is.EqualTo(length));
      });
    }
  }
}
