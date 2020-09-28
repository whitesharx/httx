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
using System.Threading.Tasks;
using Httx.Requests.Awaiters;
using Httx.Requests.Executors;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Httx.Tests {
  public class BaseRequestsTests {
    private const int AppVersion = 1;

    [UnitySetUp]
    [UsedImplicitly]
    public IEnumerator SetUp() {
      var isReady = false;

      Context.InitializeDefault(AppVersion, () => { isReady = true; });

      while (!isReady) { yield return null; }
    }

    [UnityTearDown]
    [UsedImplicitly]
    public void TearDown() {
      Context.ClearDefault();
    }

    [UnityTest]
    public IEnumerator GetText() {
      var url = "https://run.mocky.io/v3/bb9ca31c-0cb4-4640-9bfa-ed3d7a58778f";
      var request = new As<string>(new Get(new Text(url)));

      return Execute(request, result => {
        Assert.That(result, Is.EqualTo("simple-text"));
      });
    }

    private static IEnumerator Execute<T>(IAwaitable<T> awaitable, Action<T> assertions) {
      var result = default(T);
      var isReady = false;

      async void Action() {
        result = await awaitable;
        isReady = true;
      }

      Action();

      while (!isReady) { yield return null; }
      assertions(result);
    }
  }
}
