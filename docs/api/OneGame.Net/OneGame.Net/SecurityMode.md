[API reference](../../index.md) › [OneGame.Net](../index.md) › OneGame.Net

# SecurityMode enum

**Namespace:** `OneGame.Net`  
**Package:** [`RecursiveAI.OneGame.Net`](../index.md)

```csharp
public enum SecurityMode : int
```

Whether a transport encrypts and authenticates its links.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-required"></a>`Required` | `0` | Every link runs a Noise IK handshake and is encrypted and authenticated. The default, and the only mode for a game that is played over a network. |
| <a id="m-insecure"></a>`Insecure` | `1` | Plaintext, unauthenticated links as in earlier releases. For tests, loopback tools and local development. A peer in [`SecurityMode.Required`](SecurityMode.md#m-required) mode refuses to talk to it, and the other way round: there is no negotiation and no fallback. |
