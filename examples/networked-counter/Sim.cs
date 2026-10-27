using OneGame.Core;

namespace NetworkedCounterExample;

/// <summary>The shared simulation: the client and the server both compile this file. A command from any client is validated and
/// run by the server, and every replica receives the result.</summary>
[Entity]
public partial class Counter : Entity
{
    [Synced] public partial int Count { get; private set; }

    [Command]
    public void Increment(int by) => Count += by;
}
