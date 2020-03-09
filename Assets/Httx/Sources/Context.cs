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

using Httx.Caches;
using Httx.Caches.Memory;
using Httx.Loggers;
using Httx.Sources.Caches;

namespace Httx {
  public partial class Context {
    public static Context Instance { get; private set; }

    private static Context Instantiate(Context builtCtx) {
      Instance = builtCtx;
      return Instance;
    }

    private Context(ILogger logger, MemoryCache memoryCache,
      DiskCache diskCache, NativeCache nativeCache) {

      Logger = logger;
      MemoryCache = memoryCache;
      DiskCache = diskCache;
      NativeCache = nativeCache;
    }

    public ILogger Logger { get; }
    public MemoryCache MemoryCache { get; }
    public DiskCache DiskCache { get; }
    public NativeCache NativeCache { get; }
  }

  public partial class Context {
    public class Builder {
      private ILogger logger;
      private MemoryCache memoryCache;
      private DiskCache diskCache;
      private NativeCache nativeCache;

      public Builder() { }

      public Builder(Context context) {
        logger = context.Logger;
        memoryCache = context.MemoryCache;
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

      public Context Build() {
        return new Context(logger, memoryCache, diskCache, nativeCache);
      }

      public Context Instantiate() {
        return Context.Instantiate(Build());
      }
    }
  }
}
