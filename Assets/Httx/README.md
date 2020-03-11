## Httx

![npm (tag)](https://img.shields.io/npm/v/com.whitesharx.httx/latest?color=green&logo=httx)

X-Force HTTP/REST framework for Unity

 - Zero dependency, based on Unity built-in `UnityWebRequest`
 - Simple, consise DSL-like syntax to compose your queries
 - Includes Memory/Disk/Bundle caches


## Installation

You can install this package using Unity Pacakage Manager, just add the following
to your `Packages/manifest.json`:

1. Add official NPM registry with WhiteSharx scope, or simply add `com.whitesharx` scope
if you already have NPM registry added:

```json
  "scopedRegistries": [
    {
      "name": "Official NPM Registry",
      "url": "https://registry.npmjs.org/",
      "scopes": [ "com.whitesharx" ]
    }
  ]
```

2. Add httx package to your dependencies:

```json
  "dependencies": {
    "com.whitesharx.httx": "0.5.2"
  }
```
