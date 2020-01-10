# What is it?

⚡️ X-force HTTP/REST framework for Unity ⚡️

## Examples

```c#


var resultObject = await new Get(new Json("http://time.jsontest.com"));

var byteArray = await new Get(new Bytes(url), progressImpl);

var statusCode = await new Post(new Bytes(url, inputBytes), progressImpl);


```

Not excited yet? Get this!

```c#

var texture = await new Get(new Texture(url));

var assetBundle = await new Get(new Bundle(url), progressImpl);


```

## Why use?

 - Zero-dependency, made for Unity, optimized for Unity
 - Aims to be simple and concise and implement needs of 80% of developers in core
 - Moderately flexible and configurable for your needs