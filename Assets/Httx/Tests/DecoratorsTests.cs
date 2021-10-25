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
using System.Collections;
using System.Text.RegularExpressions;
using System.Threading;
using Httx.Requests.Decorators;
using Httx.Requests.Exceptions;
using Httx.Requests.Executors;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.TestTools;
using Cache = Httx.Requests.Decorators.Cache;
using Match = Httx.Requests.Decorators.Match;

namespace Httx.Tests {
  public class DecoratorsTests {
    private string tagStorage = string.Empty;

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

      return HttxTestUtils.Await(request, response => { Assert.That(response, Is.EqualTo(200)); });
    }

    [UnityTest]
    public IEnumerator CancelException() {
      const string url = RequestEndpoints.TextUrl;

      var tokenSource = new CancellationTokenSource();
      var request = new As<string>(new Get(new Cancel(new Text(url), tokenSource.Token)));

      tokenSource.Cancel();

      return HttxTestUtils.AwaitException(request, e => {
        Assert.That(e, Is.Not.Null);
        Assert.That(e, Is.TypeOf<OperationCanceledException>());
      });
    }

    [UnityTest]
    public IEnumerator CancelContinue() {
      const string url = RequestEndpoints.TextUrl;
      const string text = RequestEndpoints.TextResponse;

      var tokenSource = new CancellationTokenSource();
      var request = new As<string>(new Get(new Cancel(new Text(url), tokenSource.Token)));

      return HttxTestUtils.Await(request, response => { Assert.That(response, Is.EqualTo(text)); });
    }

    [UnityTest]
    public IEnumerator Hook() {
      const string url = RequestEndpoints.TextUrl;
      const string text = RequestEndpoints.TextResponse;

      UnityWebRequest r1 = null;
      UnityWebRequest r2 = null;

      var callbacks = new Callback<UnityWebRequest> {
          OnBeforeRequestSent = requestBeforeSent => {
            r1 = requestBeforeSent;
            Debug.Log("OnBeforeRequestSent::Called()");
          },
          OnResponseReceived = requestAfterReceived => {
            r2 = requestAfterReceived;
            Debug.Log("OnResponseReceived::Called()");
          }
      };

      var request = new As<string>(new Get(new Hook<UnityWebRequest>(new Text(url), callbacks)));

      return HttxTestUtils.Await(request, response => {
        Assert.That(r1, Is.Not.Null);
        Assert.That(r2, Is.Not.Null);

        LogAssert.Expect(LogType.Log, new Regex("OnBeforeRequestSent::Called()"));
        LogAssert.Expect(LogType.Log, new Regex("OnResponseReceived::Called()"));

        Assert.That(response, Is.EqualTo(text));
      });
    }

    [UnityTest]
    [Order(1)]
    public IEnumerator IfNoneMatch1EmptyTag() {
      const string url = RequestEndpoints.ETagUrl;
      const string text = RequestEndpoints.ETagText;

      var condition = new Condition(If.NoneMatch, tagStorage, eTag => { tagStorage = eTag; });
      var request = new As<string>(new Get(new Match(new Cache(new Text(url), Storage.Disk), condition)));

      return HttxTestUtils.Await(request, response => {
        Assert.That(tagStorage, Is.Not.Empty);
        Assert.That(response, Is.EqualTo(text));
      });
    }

    [UnityTest]
    [Order(2)]
    public IEnumerator IfNoneMatch2UpToDateTagException() {
      const string url = RequestEndpoints.ETagUrl;

      Assert.That(tagStorage, Is.Not.Empty);

      var condition = new Condition(If.NoneMatch, tagStorage, null);
      var request = new As<string>(new Get(new Match(new Cache(new Text(url), Storage.Disk), condition)));

      return HttxTestUtils.AwaitException(request, e => {
        Assert.That(e, Is.Not.Null);
        Assert.That(e, Is.TypeOf<NotModifiedException>());
      });
    }

    [UnityTest]
    [Order(3)]
    public IEnumerator IfNoneMatch3UpToDateTagRecover() {
      const string url = RequestEndpoints.ETagUrl;
      const string text = RequestEndpoints.ETagText;

      Assert.That(tagStorage, Is.Not.Empty);

      var request = new As<string>(new Get(new Cache(new Text(url), Storage.Disk)));

      return HttxTestUtils.Await(request, response => {
        Assert.That(tagStorage, Is.Not.Empty);
        Assert.That(response, Is.EqualTo(text));
      });
    }
  }
}
