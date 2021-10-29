
<h1 align="center">Httx</h1>

<h3 align="center">⚡️ X-Force HTTP/REST library for Unity ⚡️</h3>

<p align="center">
  <a aria-label="License" href="https://github.com/whitesharx/httx/blob/develop/LICENSE.md">
    <img alt="" src="https://img.shields.io/static/v1?label=LICENSE&message=MIT&style=for-the-badge&labelColor=000000&color=blue">
  </a>

  <a aria-label="CodeClimate" href="https://codeclimate.com/github/whitesharx/httx/maintainability">
    <img alt="" src="https://img.shields.io/static/v1?label=maintainability&message=A&style=for-the-badge&labelColor=000000&color=green&logo=code-climate">
  </a>

  <a aria-label="NPM" href="https://www.npmjs.com/package/com.whitesharx.httx">
    <img alt="" src="https://img.shields.io/npm/v/com.whitesharx.httx/latest?label=NPM&style=for-the-badge&labelColor=000000&color=CB3837&logo=npm">
  </a>
</p>

<br>

 * Zero dependency, built for **Unity**
 * **Simple**, DSL-like API to compose your requests
 * Includes reliable memory, disk and native AssetBundle **cache** support
 * Easily **extensible** for your custom needs

## Quick Start

You need **Unity 2019.x** or newer

### Install Httx with Package Manger (NPM Package)

Httx distributed as standard [Unity Package](https://docs.unity3d.com/Manual/PackagesList.html)
You can install this package using Unity Package Manager, just add the
following to your `Packages/manifest.json`:

Add official [NPM](https://www.npmjs.com/) registry with WhiteSharx scope, or simply
add `com.whitesharx` scope if you already have NPM registry added:

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

Add `com.whitesharx.httx` package to your dependencies (see repo tags to pick the version):

```json
{
  "dependencies": {
    "com.whitesharx.httx": "x.x.x"
  }
}
```

### Install Httx with Package Manger (Git url)

Or, your can simply install it from git url (see repo tags to pick the version):

```json
{
  "dependencies": {
    "com.whitesharx.httx": "https://github.com/whitesharx/httx.git#x.x.x"
  }
}
```

### Add preserve rule for linker

Preserve `Httx` assembly contents in your `Assets/link.xml`:

```xml
<linker>
  <assembly fullname="Httx" preserve="all"/>
</linker>
```

### Initialize context

First, you need to initaize `Context` to make HTTP requests. If you
do not need any fine-tune customization it's just one line of code:

```csharp
Context.InitializeDefault(1, () => { /* Ready to go callback */ });
```

To give you more detailed example, suppose you want to do it from MonoBehaviour:

```csharp
public class YourMonoBehaviour : MonoBehaviour {
  // XXX: Versioning exists to manage caching.
  private const int Version = 1;

  private void Awake() {
    Context.InitializeDefault(Version, OnContextReady);
  }

  private async void OnContextReady() {
    // Here you can safely make HTTP requests using Httx.
  }
}
```

### Make your first HTTP requests

Now you are ready to make HTTP requests. Suppose you realized that you desperetly need to
download whole Google...as text. Here you go:

```csharp
var text = await new As<string>(new Get(new Text("https://google.com")));
```

Ok, that's was fun. But now, let's take a look at more serious stuff you can do with Httx
starting with simple requests:

```csharp
// Make simple GET request for text data.
var text = await new As<string>(new Get(new Text(url)));

// Make GET request for JSON data and parse it to your model with default JSONUtility.
var user = await new As<User>(new Get(new Json(url)));

// Make POST request with JSON payload and parse JSON model as response.
var credentials = await new As<Credentials>(new Post(new Json<User>(url, new User("John Doe"))));
```

Take a look at some even more complicated stuff:

```csharp
// Make POST request that requires basic authorization.
var token = await new As<Token>(new Post(new Basic(new Json<User>(url, user), name, password)));

// Make POST request that requires bearer authorization.
var credentials = await new As<Credentials>(new Put(new Bearer(new Json<User>(url, user), token)));
```

Highly decorated requests become a bit messy. You can use **Fluent API** to build more complex requests.
You can rewrite two requests above with Fluent API like this:

```csharp
// Make POST request that requires basic authorization.
var token = await new Json<User>(url, user)
    .Post()
    .Basic(name, password)
    .As<Token>();

// Make POST request that requires bearer authorization.
var credentials = await new Json<User>(url, user)
    .Post()
    .Bearer(token)
    .As<Credentials>();
```

Let's examine some caching capabilities. Httx supports **in-memory** and **disk** caching out of
the box. Also it supports native Unity **AssetBundle** caching:

```csharp
// Download a picture and also cache it on disk.
// Next request to this url will be offline.
var texture = await new new Texture(url)
    .Get()
    .Cache(Storage.Disk)
    .As<Texture2D>();

// Download some "hot" data and cache it for a few seconds in memory.
// Requests to this urls made in next 4 seconds will hit only memory in-cache.
var bytes = new Bytes(url)
    .Get()
    .Cache(Storage.Memory, TimeSpan.FromSeconds(4))
    .As<byte[]>();
```

Httx also supports Unity AssetBundles out of the box:

```csharp
// Download asset bundle. Local or remote urls supported.
var bundle = await new As<AssetBundle>(new Get(new Bundle(url)));

// Download asset bundle and cache it on disk. Next request will hit offline file.
var bundle = await new As<AssetBundle>(new Cache(new Get(new Bundle(url)), Storage.Native));

// Download asset bundle and display progress while downloading
var onProgress = new Progress<float>(value => { /* Implementation */ });
var bundle = await new As<AssetBundle>(new Get(new Bundle(url), onProgress));
```

## Documentation

There's more stuff under the hood. Please check detailed documentation and examples to
understand how this librarty could fit your needs.

* [Documentation](https://github.com/whitesharx/httx/wiki)
* [Examples]()

## License

Httx is available under the [MIT](https://en.wikipedia.org/wiki/MIT_License) license.

<p align="center">
  Made with 🖤 at <a aria-label="WhiteSharx" href="https://whitesharx.com">WhiteSharx</a>
</p>
