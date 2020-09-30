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
using System.Text.RegularExpressions;
using Httx.Requests.Decorators;
using Httx.Requests.Executors;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Httx.Tests {
  public class DecoratorsTests {
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
    public IEnumerator BasicAuth() {
      const string url = RequestEndpoints.TextUrl;
      const string text = RequestEndpoints.TextResponse;

      var request = new As<string>(new Get(new Basic(new Text(url), "login", "password")));

      return HttxTestUtils.Await(request, response => {
        LogAssert.Expect(LogType.Log, new Regex("Basic bG9naW46cGFzc3dvcmQ="));
        Assert.That(response, Is.EqualTo(text));
      });
    }

    [UnityTest]
    public IEnumerator Bearer() {
      const string url = RequestEndpoints.TextUrl;
      const string text = RequestEndpoints.TextResponse;

      var request = new As<string>(new Get(new Bearer(new Text(url), "token")));

      return HttxTestUtils.Await(request, response => {
        LogAssert.Expect(LogType.Log, new Regex("Bearer token"));
        Assert.That(response, Is.EqualTo(text));
      });
    }

    [UnityTest]
    public IEnumerator Code() {
      const string url = RequestEndpoints.TextUrl;
      var request = new As<int>(new Get(new Code(new Text(url))));

      return HttxTestUtils.Await(request, response => {
        Assert.That(response, Is.EqualTo(200));
      });
    }
  }
}
