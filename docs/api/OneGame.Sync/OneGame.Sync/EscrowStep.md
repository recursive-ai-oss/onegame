[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# EscrowStep enum

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public enum EscrowStep : byte
```

What a participant's step did.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-withdraw"></a>`Withdraw` | `1` | Taking the giver's value and keeping a receipt. |
| <a id="m-abort"></a>`Abort` | `2` | Making sure a withdraw that has not happened never will. |
| <a id="m-credit"></a>`Credit` | `3` | Crediting the receiver. |
