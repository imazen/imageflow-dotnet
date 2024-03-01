## Changelog


InputWatermark.Source is now IMemorySource instead of IBytesSource

## v0.11 (2024-01-29)

New features: 
* Now multi-targets both .NET 8 and .NET Standard 2.0
* Trimming and AOT are now supported on .NET 8
* Switched to using System.Text.Json instead of Newtonsoft.Json
* Added support for RecyclableMemoryStream 3.x
* Dropped 2 dependencies: Microsoft.CSharp and Newtonsoft.Json

Breaking changes:

Check your code for usage of deprecated methods and fix them. 
The next release will involve cleanup of all deprecated methods (both the ones deprecated for years and the ones deprecated in this release).

Removed the following APIs (not frequently used)
```
public static dynamic? DeserializeDynamic(this IJsonResponseProvider p)

public static T? Deserialize<T>(this IJsonResponseProvider p) where T : class
```

Deprecated lots of APIs, including the following:
```
* All ToImageflowDynamic() methods on objects. Use ToJsonNode() instead.
* JobContext.Execute<T>
* JobContext.SendMessage<T> 

```