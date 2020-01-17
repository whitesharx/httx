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

using Httx.Requests.Types;
using Httx.Requests.Verbs;
using JetBrains.Annotations;
using UnityEngine;

public class SandboxBehaviour : MonoBehaviour {
  [UsedImplicitly]
  private async void Start() {

    // var games = await new Get<Games>(new Json<Player, Games>(url, player));
    // var games = await new Get<Games>(new Json<Games>(url));


    // var games = await new Post(new Json<Player, Games>(url, player));
    // var games = await new Get(new Json<Games>(url));


    // var games = await new Get(new Json(url)) as List<Games>;


    // var r = await Cache(Get(Json<Games>(url))
    // var r = await Cache(Authorize(Get(Json<Games>(url))))
    // var response = await Cache(Authorize(Get(Json(url))))



    // var response = await Post(Bearer(Json<Player>(url, player)))


    // var response = await Post<int>(Bearer(File(url, bytes), token), progress))


    // var assetBundle = await Get<AssetBundle>(Bundle(url), progress))
    // AssetBundle assetBundle = Get(Bundle(url), progress)

    // var texture = await Cache(Get(Texture(url))).Map<Texture2D>();
    // var texture = await As<Texture2D>(Cache(Get(Texture(url))));


    // var texture = await As<Texture2D>Cache(Get(Texture(url))))



    // Must be conf: Context, IProgress
    // var result = await new Get(new Text("http://time.jsontest.com")).Map<string>();
    //
    // Debug.Log("result: " + result);

    var result = await new Get(new Text("http://time.jsontest.com"));
  }
}
