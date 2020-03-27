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
using System.IO;
using Httx.Caches;
using Httx.Caches.Memory;
using Httx.Loggers;
using Httx.Requests.Awaiters;
using Httx.Requests.Executors;
using Httx.Requests.Extensions;
using Httx.Requests.Mappers;
using Httx.Requests.Types;
using Httx.Sources.Caches;
using UnityEngine;
using File = Httx.Requests.Types.File;
using ILogger = Httx.Loggers.ILogger;
using Texture = Httx.Requests.Types.Texture;

namespace Httx {
  public partial class Context {
    public static Context Instance { get; private set; }

    private readonly Dictionary<Type, Type> awaiterTypes;
    private readonly Dictionary<Type, Type> mapperTypes;

    private static Context Instantiate(Context builtCtx) {
      Instance = builtCtx;
      return Instance;
    }

    private Context(ILogger logger, MemoryCache memoryCache,
      DiskCache diskCache, NativeCache nativeCache,
      Dictionary<Type, Type> awaiters, Dictionary<Type, Type> mappers) {

      Logger = logger;
      MemoryCache = memoryCache;
      DiskCache = diskCache;
      NativeCache = nativeCache;
      awaiterTypes = awaiters;
      mapperTypes = mappers;
    }

    public ILogger Logger { get; }
    public MemoryCache MemoryCache { get; }
    public DiskCache DiskCache { get; }
    public NativeCache NativeCache { get; }

    public Type ResolveAwaiter(Type requestType) {
      awaiterTypes.TryGetValue(requestType.AsOpen(), out var awaiterType);
      return awaiterType;
    }

    public Type ResolveMapper(Type requestType) {
      mapperTypes.TryGetValue(requestType.AsOpen(), out var mapperType);
      return mapperType;
    }
  }

  public partial class Context {
    public class Builder {
      private ILogger logger;
      private MemoryCache memoryCache;
      private DiskCache diskCache;
      private NativeCache nativeCache;

      private readonly Dictionary<Type, Type> awaiterTypes = new Dictionary<Type, Type>();
      private readonly Dictionary<Type, Type> mapperTypes = new Dictionary<Type, Type>();

      public Builder() { }

      public Builder(Context context) {
        logger = context.Logger;
        memoryCache = context.MemoryCache;
        diskCache = context.DiskCache;
        nativeCache = context.NativeCache;

        foreach (var p in context.awaiterTypes) {
          awaiterTypes[p.Key] = p.Value;
        }

        foreach (var p in context.mapperTypes) {
          mapperTypes[p.Key] = p.Value;
        }
      }

      public Builder WithLogger(ILogger loggerArg) {
        logger = loggerArg;
        return this;
      }

      public Builder WithMemoryCache(MemoryCache memoryCacheArg) {
        memoryCache = memoryCacheArg;
        return this;
      }

      public Builder WithDiskCache(DiskCache diskCacheArg) {
        diskCache = diskCacheArg;
        return this;
      }

      public Builder WithNativeCache(NativeCache nativeCacheArg) {
        nativeCache = nativeCacheArg;
        return this;
      }

      public Builder WithAwaiter(Type requestType, Type awaiterType) {
        awaiterTypes[requestType.AsOpen()] = awaiterType.AsOpen();
        return this;
      }

      public Builder WithMapper(Type requestType, Type mapperType) {
        mapperTypes[requestType.AsOpen()] = mapperType.AsOpen();
        return this;
      }

      public Context Build() {
        return new Context(logger, memoryCache, diskCache,
          nativeCache, awaiterTypes, mapperTypes);
      }

      public Context Instantiate() {
        return Context.Instantiate(Build());
      }

      public void Destroy() {
        memoryCache = null;

        diskCache?.Dispose();
        diskCache = null;

        nativeCache?.Dispose();
        nativeCache = null;

        Instance = null;
      }
    }
  }

  public partial class Context {
    private static readonly string DiskCachePath =
      Path.Combine(Application.persistentDataPath, "Httx", "DiskCache");

    private static readonly string NativeCachePath =
      Path.Combine(Application.persistentDataPath, "Httx", "BundlesCache");

    private static int MemoryMaxSize = 32;
    private static int DiskMaxSize = 64 * 1024 * 1024;
    private static int NativeMaxSize = 128 * 1024 * 1024;
    private static int DiskCollectFrequency = 128;

    public static void InitializeDefault(int version, Action onContextReady,
      Action<Builder> onBeforeInstantiate = null, int inflateMultiplier = 1) {

      var memoryMaxSize = inflateMultiplier * MemoryMaxSize;
      var diskMaxSize = inflateMultiplier * DiskMaxSize;
      var nativeMaxSize = inflateMultiplier * NativeMaxSize;

      var diskCacheArgs = new DiskCacheArgs(DiskCachePath, version, diskMaxSize, DiskCollectFrequency);
      var nativeCacheArgs = new NativeCacheArgs(NativeCachePath, (uint) version, nativeMaxSize);

      var diskCache = new DiskCache(diskCacheArgs);
      var nativeCache = new NativeCache(nativeCacheArgs);
      var memoryCache = new MemoryCache(memoryMaxSize, onEvictValueCallback: (value, isExpired) => {
        if (value is IDisposable disposable) {
          disposable.Dispose();
        }

        if (value is AssetBundle bundle) {
          bundle.UnloadUnsafe(false);
        }
      });

      diskCache.Initialize(() => {
        nativeCache.Initialize(() => {
          var builder = new Builder();
          builder.WithLogger(new UnityDefaultLogger());
          builder.WithMemoryCache(memoryCache);
          builder.WithDiskCache(diskCache);
          builder.WithNativeCache(nativeCache);

          builder.WithAwaiter(typeof(Head), typeof(UnityWebRequestAwaiter<>));
          builder.WithAwaiter(typeof(Bundle), typeof(UnityWebRequestAssetBundleAwaiter<>));
          builder.WithAwaiter(typeof(Bytes), typeof(UnityWebRequestAwaiter<>));
          builder.WithAwaiter(typeof(File), typeof(UnityWebRequestFileAwaiter));
          builder.WithAwaiter(typeof(Json<>), typeof(UnityWebRequestAwaiter<>));
          builder.WithAwaiter(typeof(Json), typeof(UnityWebRequestAwaiter<>));
          builder.WithAwaiter(typeof(Manifest), typeof(UnityWebRequestAssetBundleAwaiter<>));
          builder.WithAwaiter(typeof(Resource), typeof(UnityWebRequestAssetBundleAwaiter<>));
          builder.WithAwaiter(typeof(Text), typeof(UnityWebRequestAwaiter<>));
          builder.WithAwaiter(typeof(Texture), typeof(UnityWebRequestTextureAwaiter));

          // XXX: AOT compatibility. It does not matter for library,
          // open type will be registered anyway, but it does for IL2CPP to
          // register existing value type.

          // We're registering just basic "code/length" types here, if you need
          // to manage custom value types, please refer to docs/discussions.
          builder.WithAwaiter(typeof(Length), typeof(UnityWebRequestAwaiter<int>));
          builder.WithAwaiter(typeof(Length), typeof(UnityWebRequestAwaiter<long>));
          builder.WithAwaiter(typeof(Complete<>), typeof(CompleteAwaiter<int>));
          builder.WithAwaiter(typeof(Complete<>), typeof(CompleteAwaiter<long>));

          builder.WithMapper(typeof(Head), typeof(HeadersMapper));
          builder.WithMapper(typeof(Length), typeof(ContentLengthMapper));
          builder.WithMapper(typeof(Bytes), typeof(NopMapper<,>));
          builder.WithMapper(typeof(Json<>), typeof(Utf8JsonUtilityMapper<,>));
          builder.WithMapper(typeof(Json), typeof(Utf8JsonUtilityMapper<,>));
          builder.WithMapper(typeof(Text), typeof(Utf8TextMapper));

          onBeforeInstantiate?.Invoke(builder);
          builder.Instantiate();
          onContextReady();
        });
      });
    }

    public static void ClearDefault() {
      if (Directory.Exists(DiskCachePath)) {
        Directory.Delete(DiskCachePath, true);
      }

      if (Directory.Exists(NativeCachePath)) {
        Directory.Delete(NativeCachePath, true);
      }
    }
  }
}
