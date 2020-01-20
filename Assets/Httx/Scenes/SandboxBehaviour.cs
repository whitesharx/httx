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
using Httx.Requests.Aux;
using Httx.Requests.Mappers;
using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using UnityEngine;

public class SandboxBehaviour : MonoBehaviour {
  [UsedImplicitly]
  private async void Start() {
    // Must be conf: Context, IProgress
    // var result = await new Get(new Text("http://time.jsontest.com")).Map<string>();
    //
    // Debug.Log("result: " + result);

    // var result = await new Get(new Text("http://time.jsontest.com"));

    //
    // var t2 = typeof(Utf8JsonUtilityMapper<,>);
    // // var gt2 = t2.MakeGenericType(typeof(object), typeof(byte[]));
    // // var r2 = (IResultMapper<byte[]>) Activator.CreateInstance(gt2);
    //
    //
    // Debug.Log(t2.GetGenericArguments().Length);


    var result = await new As<string>(new Get(new Text("http://time.jsontest.com")));
    Debug.Log($"Result: {result}");

  }
}
