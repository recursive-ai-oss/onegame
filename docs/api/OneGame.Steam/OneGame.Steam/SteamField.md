[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# SteamField enum

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public enum SteamField : byte
```

Primitive kinds a callback struct is made of.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-u8"></a>`U8` | `0` | One byte (uint8 or bool). |
| <a id="m-i32"></a>`I32` | `1` | Four bytes, signed. |
| <a id="m-u32"></a>`U32` | `2` | Four bytes, unsigned. |
| <a id="m-u64"></a>`U64` | `3` | Eight bytes, unsigned. |
| <a id="m-name128"></a>`Name128` | `4` | A 128-byte character array (stat and achievement names). |
