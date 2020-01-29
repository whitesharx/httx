// Copyright (C) 2018 White Sharx (https://whitesharx.com) - All Rights Reserved.
// Unauthorized copying of this file, via any medium is strictly prohibited.
// Proprietary and confidential.
//

using System.IO;
using UnityEngine;

namespace Httx.Cache {
  /*
  public static class LocalCache {
    public static void Expire() {
      if (Directory.Exists(SharedPath)) {
        Directory.Delete(SharedPath, true);
      }

      Directory.CreateDirectory(SharedPath);
    }

    public static void Expire(string key) {
      var innerKey = RichCrypto.Sha256(key);
      var innerPath = Path.Combine(SharedPath, innerKey);

      if (File.Exists(innerPath)) {
        File.Delete(innerPath);
      }
    }

    public static bool Exists(string key) {
      var innerKey = RichCrypto.Sha256(key);
      var innerPath = Path.Combine(SharedPath, innerKey);

      return File.Exists(innerPath);
    }

    public static async UniTask<byte[]> GetAsync(string key) {
      var innerKey = RichCrypto.Sha256(key);
      var innerPath = Path.Combine(SharedPath, innerKey);

      if (File.Exists(innerPath)) {
        return await ReadAsync(innerPath);
      }

      return null;
    }

    public static async UniTask<string> SetAsync(string key, byte[] bytes) {
      if (bytes.IsNullOrEmpty()) {
        return null;
      }

      var innerKey = RichCrypto.Sha256(key);
      var innerPath = Path.Combine(SharedPath, innerKey);

      if (!Directory.Exists(SharedPath)) {
        Directory.CreateDirectory(SharedPath);
      }

      if (File.Exists(innerPath)) {
        File.Delete(innerPath);
      }

      await WriteAsync(innerPath, bytes);

      return innerKey;
    }

    public static string SharedPath => Path.Combine(Application.persistentDataPath, "LocalCache");


    private static async UniTask WriteAsync(string path, byte[] bytes) {
      using (var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.None, 4096, true)) {
        await stream.WriteAsync(bytes, 0, bytes.Length);
      }
    }

    private static async UniTask<byte[]> ReadAsync(string path) {
      using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true)) {
        var buffer = new byte[stream.Length];
        await stream.ReadAsync(buffer, 0, (int) stream.Length);

        return buffer;
      }
    }
  }
  */
}
