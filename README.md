## About

![npm (tag)](https://img.shields.io/npm/v/com.whitesharx.httx/latest?color=green&logo=httx)
[![CodeFactor](https://www.codefactor.io/repository/github/whitesharx/httx/badge)](https://www.codefactor.io/repository/github/whitesharx/httx)

X-Force HTTP/REST framework for Unity

 - :warning: Under heavy development
 - Zero dependency, built for Unity
 - Simple, DSL-like API to compose your requests
 - Includes reliable Memory/Disk/Bundle cache support
 - Easily extensible for your custom needs


## Introduction

```c#
// Ok. Let's make simple GET request for text data
var text = await new As<string>(new Get(new Text(url)));

// What if I want to parse JSON instead?
var user = await new As<User>(new Get(new Json(url)));

// What if I want to POST/PUT my JSON data?
var user = new User("John Doe");
var credentials = await new As<Credentials>(new Post(new Json<User>(url, user)));

// What if my request requires basic authorization?
var token = await new As<Token>(new Post(new Basic(new Json<User>(url, user), name, password)));

// And what if it's PUT request with bearer authorization?
var credentials = await new As<Credentials>(new Put(new Bearer(new Json<User>(url, user), token)));

// I want to easily download my asset bundle, local or remote
var bundle = await new As<AssetBundle>(new Get(new Bundle(url)));

// I want this bundle cached on disk
var bundle = await new As<AssetBundle>(new Cache(new Get(new Bundle(url)), Storage.Native));

// I want to download a picture and also cache it on disk
var texture = await new As<Texture2D>(new Cache(new Get(new Texture(url)), Storage.Disk));

// I just want to know size of a texture or content I need to download
var byteSize = await new Length(url);

// And I want to display progress while downloading
var onProgress = new Progress<float>(value => { /* Implementation */ });
var bundle = await new As<AssetBundle>(new Get(new Bundle(url), onProgress));

// I want my "hot" request cached for a few seconds in memory
var ttl = TimeSpan.FromSeconds(4);
var bytes = await new As<byte[]>(new Cache(new Get(new Bytes(url)), Storage.Memory, ttl));

// I just want to know response code
var code = await new As<int>(new Code(new Put(new Json<User>(url, user))));
```

For more information about API, usage scenarios and examples see [detailed documentation]().


## Integration

Httx distributed as standard [Unity Package](https://docs.unity3d.com/Manual/PackagesList.html)
You can install this package using Unity Package Manager, just add the
following to your `Packages/manifest.json`:

1. Add official NPM registry with WhiteSharx scope, or simply add `com.whitesharx` scope
if you already have NPM registry added:

```json
{
  "scopedRegistries": [
    {
      "name": "Official NPM Registry",
      "url": "https://registry.npmjs.org/",
      "scopes": [ "com.whitesharx" ]
    }
  ]
}
```

2. Add httx package to your dependencies:

```json
{
  "dependencies": {
    "com.whitesharx.httx": "0.5.2"
  }
}
```

3. Preserve Httx assembly contents in your `Assets/link.xml`:

```xml
<linker>
  <assembly fullname="Httx" preserve="all"/>
</linker>
```
