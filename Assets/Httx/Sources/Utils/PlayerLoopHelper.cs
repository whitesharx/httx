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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.LowLevel;
using UnityEngine.Experimental.PlayerLoop;

namespace Httx.Utils {
  public struct HttxRequestFixedUpdateSystem { }



  public class PlayerLoopHelper {

    private static PlayerLoopSystem.UpdateFunction currentFunction;

    public static void Insert(Action<IProgress<float>> onProgressUpdate) {
      var rootSystem = PlayerLoop.GetDefaultPlayerLoop();

      var fixedUpdateSystem = rootSystem.subSystemList.First(s => s.type == typeof(FixedUpdate));
      var fixedUpdateList = new List<PlayerLoopSystem>(fixedUpdateSystem.subSystemList);

      currentFunction = () => onProgressUpdate.Invoke(null);

      var requestSystem = new PlayerLoopSystem {
        type = typeof(HttxRequestFixedUpdateSystem),
        updateDelegate = currentFunction
      };




      fixedUpdateList.Add(requestSystem);
      fixedUpdateSystem.subSystemList = fixedUpdateList.ToArray();


      rootSystem.subSystemList = rootSystem.subSystemList.Select(s => s.type == typeof(FixedUpdate) ? fixedUpdateSystem : s).ToArray();

      // var names = fixedUpdateSystem.subSystemList.Select(system => system.type.Name).ToArray();

      // Debug.Log($"Names: {string.Join(", ", names)}");

      PlayerLoop.SetPlayerLoop(rootSystem);


      var nextNames = rootSystem.subSystemList.First(s => s.type == typeof(FixedUpdate))
        .subSystemList.Select(system => system.type.Name).ToArray();

      Debug.Log($"nextNames: {string.Join(", ", nextNames)}");

    }



    public static void Remove() {
      var rootSystem = PlayerLoop.GetDefaultPlayerLoop();

      var fixedUpdateSystem = rootSystem.subSystemList.First(s => s.type == typeof(FixedUpdate));

      var names = fixedUpdateSystem.subSystemList.Select(system => system.type.Name).ToArray();
      Debug.Log($"Names: {string.Join(", ", names)}");

      // var result = fixedUpdateSystem.subSystemList.First(s => s.type == typeof(HttxRequestFixedUpdateSystem));
      // Debug.Log("result: " + result.type);

    }



  }
}
