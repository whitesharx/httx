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
using Httx.Requests.Executors;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Httx.Tests {
  public class TypesTests {
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

    public void Bundle() {
      // TODO:
    }

    [UnityTest]
    public IEnumerator Bytes() {
      const string url = RequestEndpoints.TextUrl;
      const string text = RequestEndpoints.TextResponse;

      var responseBytes = System.Text.Encoding.UTF8.GetBytes(text);
      var request = new As<byte[]>(new Get(new Bytes(url)));

      return HttxTestUtils.Execute(request, bytes => {
        Assert.That(bytes, Is.EqualTo(responseBytes));
      });
    }

    [UnityTest]
    public IEnumerator Complete() {
      const string text = RequestEndpoints.TextResponse;
      var request = new As<string>(new Complete<string>(text));

      return HttxTestUtils.Execute(request, result => {
        Assert.That(result, Is.EqualTo(text));
      });
    }

    public void File() {
      // TODO:
    }

    public void Json() {

    }
  }
}
