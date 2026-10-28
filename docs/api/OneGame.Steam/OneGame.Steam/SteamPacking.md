[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# SteamPacking enum

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public enum SteamPacking : int
```

How the Steam library packs the callback structs it hands out. The C++ headers pack them at 4 bytes on Linux and macOS and at 8 bytes on Windows, which moves any 64-bit field that follows an odd number of 32-bit ones.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-small"></a>`Small` | `4` | 4-byte packing (Linux, macOS). |
| <a id="m-large"></a>`Large` | `8` | 8-byte packing (Windows). |
