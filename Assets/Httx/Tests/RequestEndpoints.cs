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
using UnityEngine;

namespace Httx.Tests {
  public static class RequestEndpoints {
    public const string TextUrl = "https://run.mocky.io/v3/372251d8-2760-4d42-b61c-569da2534962";
    public const string TextResponse = "success-respose";

    public const string JsonUrl = "https://run.mocky.io/v3/cf731d9a-35ca-4a57-bf6b-6b0c9147449f";
    public const string JsonResponse = "{ \"text\": \"some-string\", \"number\": 7 }";

    public const string TextureUrl = "https://homepages.cae.wisc.edu/~ece533/images/zelda.png";
    public const int TextureSize = 512;
    public const int TextureBytes = 1048576;

    public const string BundleUrl = "https://whitesharx.app/temporary/input-bundle.osx-bundle";
    public const string ManifestUrl = "https://whitesharx.app/temporary/input-bundle.osx-bundle.manifest";

    public const string FakeUrl = "http://fakehost";
    public const string NotFoundUrl = "https://whitesharx.app/not-exeistent-path";
  }

  [Serializable]
  public class JsonResponseModel {
    [SerializeField]
    private string text;

    [SerializeField]
    private int number;

    public JsonResponseModel(string text, int number) {
      this.text = text;
      this.number = number;
    }

    public string Text => text;
    public int Number => number;
  }
}
