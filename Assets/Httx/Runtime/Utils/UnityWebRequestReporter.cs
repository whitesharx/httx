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
using UnityEngine.Networking;

#if UNITY_2019_3_OR_NEWER
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
#else
using UnityEngine.Experimental.LowLevel;
using UnityEngine.Experimental.PlayerLoop;
#endif

namespace Httx.Utils {
  public static class UnityWebRequestReporter {
    public struct HttxUnityWebRequestReporterFixedUpdate { }

    public class ReporterWrapper {
      public ReporterWrapper(WeakReference<IProgress<float>> progressRef, UnityWebRequest request) {
        ProgressRef = progressRef;
        Request = request;
      }

      public WeakReference<IProgress<float>> ProgressRef { get; }
      public UnityWebRequest Request { get; }
    }

    private static bool isInitialized;
    private static readonly Dictionary<string, ReporterWrapper> Reporters =
      new Dictionary<string, ReporterWrapper>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize() {
      if (isInitialized) {
        return;
      }

#if UNITY_2019_3_OR_NEWER
      var rootSystem = PlayerLoop.GetCurrentPlayerLoop();
#else
      var rootSystem = PlayerLoop.GetDefaultPlayerLoop();
#endif

      var fixedUpdateSystem = rootSystem.subSystemList.First(s => s.type == typeof(FixedUpdate));

      var fixedUpdateList = new List<PlayerLoopSystem>(fixedUpdateSystem.subSystemList) {
        new PlayerLoopSystem {
          type = typeof(HttxUnityWebRequestReporterFixedUpdate),
          updateDelegate = UpdateFunctionImpl
        }
      };

      fixedUpdateSystem.subSystemList = fixedUpdateList.ToArray();
      rootSystem.subSystemList = rootSystem.subSystemList.Select(s =>
        s.type == typeof(FixedUpdate) ? fixedUpdateSystem : s).ToArray();

      PlayerLoop.SetPlayerLoop(rootSystem);
      isInitialized = true;
    }

    public static void AddReporterRef(string pRefId, ReporterWrapper wrapper) {
      Reporters[pRefId] = wrapper;
    }

    public static void RemoveReporterRef(string pRefId) {
      if (!Reporters.ContainsKey(pRefId)) {
        return;
      }

      Reporters[pRefId] = null;
      Reporters.Remove(pRefId);
    }

    private static void UpdateFunctionImpl() {
      foreach (var p in Reporters.Where(p => p.Value?.ProgressRef != null)) {
        p.Value.ProgressRef.TryGetTarget(out var progress);

        var request = p.Value?.Request;

        // XXX: Basically, it's not very good strategy. But just
        // for the sake of simplicity, let's try how this approach
        // will be valid for most use cases.
        var progressValue = null != request?.uploadHandler
          ? request.uploadProgress : request?.downloadProgress;

        if (progressValue != null) {
          progress?.Report((float) progressValue);
        }
      }
    }
  }
}
