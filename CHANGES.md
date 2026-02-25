## Changelog

## v0.15.1 (draft)

* Now targets .NET 10 (LTS), .NET 8 (LTS), and .NET Standard 2.0/2.1
* ARM64 support on Windows, macOS, and Linux
* Minimum System.Text.Json raised to 8.0.6 (on netstandard2.0/2.1 only; inbox on net8.0+)
* Fixed 7 memory safety bugs:
  - Double-pin in `AddInputBytesPinned` (duplicate `AddPinnedData` call)
  - `MemorySource.TakeOwnership` blocked by incorrect guard clause
  - `FinishWithTimeout` disposed its `CancellationTokenSource` immediately, making cancellation inert
  - `FinishJobBuilder` leaked `CancellationTokenSource` on replacement and on disposal
  - `ImageflowUnmanagedReadStream` didn't call `base.Dispose`, leaving stream in "open" state
  - `Utf8JsonWriter` in `SerializeNode` not disposed
  - `BytesDestination` had no-op `Dispose`, leaking underlying `MemoryStream`
* Added 29 memory safety tests including leak detection across all 8 codecs
* `FinishJobBuilder` now implements `IDisposable`

##v0.13.2

Fixes CreateCanvasBgra bug and supports HttpContent.ReadAsStreamAsync streams Latest

## v0.13.1

BufferedStreamSource now works with seekable streams that throw an exception when you access their Position property.

## v0.13

This release makes user-facing changes with deprecation warnings. Please review your build warnings to avoid breakage in the future.

* There are new classes for attaching source image data to jobs; use MemorySource.* over ByteSource and BufferedStreamSource.* instead of StreamSource.
* Microsoft.IO.RecyclableMemoryStream 3.x is now required
* System.Buffers and System.Memory 4.x+ are now required on .NET 4.x / .NET Standard 2.0
* InputWatermark.Source is now IMemorySource instead of IBytesSource

It also makes lots of internal changes to increase performance, eliminate unnecessary allocations/copies, and improve compatibility with AOT and trimming.

It is now possible to provide ReadOnlyMemory<byte> data and IOwnedMemory<byte> data, without copying to a byte[] array.
The new IAsyncMemorySource interface allows for asynchronous data sources, and the new IMemorySource interface allows
for synchronous data sources.

## v0.12 (2024-02-06)

* Fix compatibility with RecyclableMemoryStream 3.x, drop compatibility with 1.x
* Remove default constructor on BuildJobResult and BuildEncodeResult; these are not user-created types.

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

To accommodate the shift to `System.Text.Json`, interface members `ToJsonNode()` have been added to `IEncoderPreset` and `IWatermarkConstraintBox`. Nobody should be implementing these anyway, other than the Imageflow library itself.

The `object` parameter in `BuildItemBase` protected constructor has been changed to `System.Text.Json.Nodes.JsonNode`.

Deprecated lots of APIs, including the following:
```
* All ToImageflowDynamic() methods on objects. Use ToJsonNode() instead.
* JobContext.Execute<T>
* JobContext.SendMessage<T>

```
