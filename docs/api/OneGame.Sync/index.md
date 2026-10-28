[API reference](../index.md) › OneGame.Sync

# OneGame.Sync

Nodes, scopes, loopback transport, saves, command log, replay and drift hashes.

**Package:** `RecursiveAI.OneGame.Sync`

```sh
dotnet add package RecursiveAI.OneGame.Sync
```

## OneGame.Generated.OneGame\_Sync

| Type | Kind | Description |
| --- | --- | --- |
| [`EntityTypes`](OneGame.Generated.OneGame_Sync/EntityTypes.md) | static class | The entity types declared in this assembly. |

## OneGame.Storage

| Type | Kind | Description |
| --- | --- | --- |
| [`SaveSlotOptions`](OneGame.Storage/SaveSlotOptions.md) | class | Options for [`SaveSlots`](OneGame.Storage/SaveSlots.md). |
| [`SaveSlots`](OneGame.Storage/SaveSlots.md) | class | Named save slots over a [`IByteStore`](../OneGame.Core/OneGame.Storage/IByteStore.md): each slot is one file holding a small description and a world save ([`SaveFile`](OneGame.Sync/SaveFile.md), so older saves load through the entity migrations). |
| [`SlotInfo`](OneGame.Storage/SlotInfo.md) | record | The small description of a slot a load screen shows, read without restoring the world. |
| [`SlotKind`](OneGame.Storage/SlotKind.md) | enum | Why a slot was written. |
| [`SlotResult`](OneGame.Storage/SlotResult.md) | record | The outcome of a slot operation. |
| [`SlotStatus`](OneGame.Storage/SlotStatus.md) | enum | How a slot operation ended. |

## OneGame.Sync

| Type | Kind | Description |
| --- | --- | --- |
| [`AuthorityScope`](OneGame.Sync/AuthorityScope.md) | class | A scope whose authority is this node: it owns the simulating [`AuthorityScope.World`](OneGame.Sync/AuthorityScope.md#m-world), accepts commands, and publishes a snapshot then one frame per tick to each subscriber. |
| [`Checkpoint`](OneGame.Sync/Checkpoint.md) | record | A hash of the whole world after a tick, with the per-part digest when it was small enough to keep. |
| [`Cluster`](OneGame.Sync/Cluster.md) | class | A set of nodes in one process, joined by loopback links and stepped together. |
| [`ClusterOptions`](OneGame.Sync/ClusterOptions.md) | class | Cluster-wide behavior switches. |
| [`CommandOutcome`](OneGame.Sync/CommandOutcome.md) | struct | What happened to a command when the authority ran it. |
| [`CommandRefusal`](OneGame.Sync/CommandRefusal.md) | struct | A command or subscription that an authority refused, or that this node was told was refused. |
| [`CommandResult`](OneGame.Sync/CommandResult.md) | struct | The answer to one command a replica sent, matched to the send by [`CommandResult.Sequence`](OneGame.Sync/CommandResult.md#m-sequence). |
| [`CommandVerdict`](OneGame.Sync/CommandVerdict.md) | enum | How a command sent from a replica ended. |
| [`Connection`](OneGame.Sync/Connection.md) | class | The authority's end of a connection: what the peer is trusted to do, and the entry point for commands as that peer. |
| [`DifferenceChange`](OneGame.Sync/DifferenceChange.md) | enum | How a part of the state differs between two digests. |
| [`DigestEntry`](OneGame.Sync/DigestEntry.md) | struct | One line of a [`StateDigest`](OneGame.Sync/StateDigest.md). |
| [`DigestKind`](OneGame.Sync/DigestKind.md) | enum | What a digest entry describes. |
| [`Divergence`](OneGame.Sync/Divergence.md) | record | The first place a replay stopped matching the recording. |
| [`DivergenceReason`](OneGame.Sync/DivergenceReason.md) | enum | Why a replay stopped. |
| [`DriftException`](OneGame.Sync/DriftException.md) | class | Thrown by [`Cluster.Step()`](OneGame.Sync/Cluster.md#m-step-int) when [`ClusterOptions.FatalDrift`](OneGame.Sync/ClusterOptions.md#m-fataldrift) is on and a replica drifted. |
| [`DriftReport`](OneGame.Sync/DriftReport.md) | class | A replica whose state differs from its authority's at the same tick. |
| [`Durability`](OneGame.Sync/Durability.md) | enum | When a scope lets answers and confirmations leave relative to saving its state. |
| [`Escrow`](OneGame.Sync/Escrow.md) | static class | Installing escrow into a game's worlds, and the identity the coordinator acts as. |
| [`EscrowBook`](OneGame.Sync/EscrowBook.md) | class | The escrow records of a scope, kept in its world (so saves, snapshots and replays carry them): on the coordinator, the trades; on every participant, a receipt for each step it took, keyed by transaction and leg. |
| [`EscrowDelivery`](OneGame.Sync/EscrowDelivery.md) | enum | Whether a leg's value has been handed on (to the counterparty after a commit, back to the giver after a cancel). |
| [`EscrowDesk`](OneGame.Sync/EscrowDesk.md) | class | The command surface of an escrow in a scope. |
| [`EscrowDesk.Commands`](OneGame.Sync/EscrowDesk.Commands.md) | struct | Sends commands to this entity. |
| [`EscrowDesk.ReadOnlyView`](OneGame.Sync/EscrowDesk.ReadOnlyView.md) | struct | A read-only window onto the synced state, for views. |
| [`EscrowDriver`](OneGame.Sync/EscrowDriver.md) | class | Host code on the coordinator's node that carries out what the coordinator's [`EscrowBook`](OneGame.Sync/EscrowBook.md) says is still to do: it sends the withdraw, abort and credit steps to the participants' desks as commands and feeds their answers back to the coordinator's desk. |
| [`EscrowHold`](OneGame.Sync/EscrowHold.md) | enum | Whether a leg's value has been taken from its giver. |
| [`EscrowLeg`](OneGame.Sync/EscrowLeg.md) | class | One direction of a trade: [`EscrowLeg.Giver`](OneGame.Sync/EscrowLeg.md#m-giver) on [`EscrowLeg.Scope`](OneGame.Sync/EscrowLeg.md#m-scope) gives [`EscrowLeg.Amount`](OneGame.Sync/EscrowLeg.md#m-amount) of [`EscrowLeg.Kind`](OneGame.Sync/EscrowLeg.md#m-kind) to [`EscrowLeg.Receiver`](OneGame.Sync/EscrowLeg.md#m-receiver) on the other leg's scope. |
| [`EscrowReply`](OneGame.Sync/EscrowReply.md) | struct | The answer to an escrow step: accepted (first time or repeat), or declined with a short reason. |
| [`EscrowState`](OneGame.Sync/EscrowState.md) | enum | Where one trade stands on the coordinator. |
| [`EscrowStep`](OneGame.Sync/EscrowStep.md) | enum | What a participant's step did. |
| [`EscrowTrade`](OneGame.Sync/EscrowTrade.md) | class | A trade between two scopes, as the coordinator records it. |
| [`FixedStepDriver`](OneGame.Sync/FixedStepDriver.md) | class | Turns variable frame times into a fixed number of simulation ticks. |
| [`FollowTarget`](OneGame.Sync/FollowTarget.md) | record | Where a node should subscribe to follow entities that moved to another scope: a link to the node that holds it and an empty replica world. |
| [`IAuthenticatedChannel`](OneGame.Sync/IAuthenticatedChannel.md) | interface | A channel whose far end proved who it is during the transport handshake. |
| [`IEscrowHandler`](OneGame.Sync/IEscrowHandler.md) | interface | The game's side of an escrow: how value leaves and enters its holdings. |
| [`IMessageChannel`](OneGame.Sync/IMessageChannel.md) | interface | One end of a message pipe between two nodes. |
| [`Link`](OneGame.Sync/Link.md) | class | The client end of a connection to another node: where a node sends subscriptions and commands, and receives snapshots and frames. |
| [`LoadResult`](OneGame.Sync/LoadResult.md) | record | What [`SaveFile.Load()`](OneGame.Sync/SaveFile.md#m-load-readonlyspan-byte-world) found and did. |
| [`LoggedCommand`](OneGame.Sync/LoggedCommand.md) | record | A command that entered an authority from outside its tick. |
| [`Loopback`](OneGame.Sync/Loopback.md) | static class | In-process transport: two channel ends joined by byte queues that frame messages with a length prefix, so receivers see exactly the bytes a socket would deliver. |
| [`Node`](OneGame.Sync/Node.md) | class | A participant: it holds the scopes it is authority for, the replicas it subscribes to, and the connections that carry both. |
| [`PeerIdentity`](OneGame.Sync/PeerIdentity.md) | struct | Who is on the other end of an accepted channel, as far as the transport can prove. |
| [`PlayerRegistry`](OneGame.Sync/PlayerRegistry.md) | class | Gives each authenticated key a stable small player id, so a client that reconnects (a new connection, the same key) is the same player. |
| [`PlayerRegistryFullException`](OneGame.Sync/PlayerRegistryFullException.md) | class | The registry holds as many players as it may and every one of them has been activated, so there is nobody to forget. |
| [`Prediction`](OneGame.Sync/Prediction.md) | class | Client-side prediction for one replica. |
| [`PredictionOptions`](OneGame.Sync/PredictionOptions.md) | class | Settings for a replica's prediction ([`ReplicaScope.EnablePrediction()`](OneGame.Sync/ReplicaScope.md#m-enableprediction-world-predictionoptions)). |
| [`ProtocolError`](OneGame.Sync/ProtocolError.md) | struct | A message from a peer that could not be applied; the replica asked for a fresh snapshot. |
| [`Recorder`](OneGame.Sync/Recorder.md) | class | Captures a run on an [`AuthorityScope`](OneGame.Sync/AuthorityScope.md). |
| [`Recording`](OneGame.Sync/Recording.md) | class | A run that can be replayed: the world at the start, every command that entered from outside, and hash checkpoints to verify against. |
| [`RecordingOptions`](OneGame.Sync/RecordingOptions.md) | class | How a recording samples the run. |
| [`RefusalReason`](OneGame.Sync/RefusalReason.md) | enum | Why a command or subscription was refused. |
| [`ReplayOptions`](OneGame.Sync/ReplayOptions.md) | class | Options for [`Replayer.Replay()`](OneGame.Sync/Replayer.md#m-replay-recording-world-replayoptions). |
| [`ReplayResult`](OneGame.Sync/ReplayResult.md) | class | The outcome of [`Replayer.Replay()`](OneGame.Sync/Replayer.md#m-replay-recording-world-replayoptions). |
| [`Replayer`](OneGame.Sync/Replayer.md) | static class | Re-runs a [`Recording`](OneGame.Sync/Recording.md) and checks the hashes. |
| [`ReplicaScope`](OneGame.Sync/ReplicaScope.md) | class | This node's copy of a scope it does not own. |
| [`ReplicaState`](OneGame.Sync/ReplicaState.md) | enum | Where a replica is in its life. |
| [`SaveFile`](OneGame.Sync/SaveFile.md) | static class | Save files: a header (format, seed, tick, hashes, schema versions) followed by a world snapshot. |
| [`SaveHeader`](OneGame.Sync/SaveHeader.md) | record | The fixed part of a save file. |
| [`SaveStatus`](OneGame.Sync/SaveStatus.md) | enum | How loading a save ended. |
| [`SchemaEntry`](OneGame.Sync/SchemaEntry.md) | struct | The schema version a save holds for one entity type. |
| [`ScopeHash`](OneGame.Sync/ScopeHash.md) | struct | One scope's state on one node, as listed by [`Cluster.Hashes()`](OneGame.Sync/Cluster.md#m-hashes). |
| [`ScopeId`](OneGame.Sync/ScopeId.md) | struct | Names a scope: a unit of state with exactly one authority node. |
| [`ScopeSet`](OneGame.Sync/ScopeSet.md) | struct | A set of scopes a connection is allowed to touch: all of them, none, or a listed few. |
| [`StateDifference`](OneGame.Sync/StateDifference.md) | record | One entity, table or the random streams that differ between expected and actual state. |
| [`StateDigest`](OneGame.Sync/StateDigest.md) | class | A per-part hash list of a world at one tick: the random streams, each table, each entity. |
| [`SubmitResult`](OneGame.Sync/SubmitResult.md) | enum | What `Connection.SubmitCommand` did with a command. |
| [`SyncModule`](OneGame.Sync/SyncModule.md) | static class | Identifies the Sync assembly; a placeholder until its first feature lands. |
| [`Transfer`](OneGame.Sync/Transfer.md) | class | A running or finished move of entities from one scope to a scope on another node ([`AuthorityScope.BeginTransfer()`](OneGame.Sync/AuthorityScope.md#m-begintransfer-link-scopeid-ienumerable-entityid)). |
| [`TransferLimits`](OneGame.Sync/TransferLimits.md) | static class | Limits on what one authority transfer may carry. |
| [`TransferLog`](OneGame.Sync/TransferLog.md) | class | The state of a scope's authority transfers, kept in the world so that a save, a snapshot-based restart and a replay carry it: the bundles that left this scope and are not confirmed yet, and the ids of the transfers this scope has installed. |
| [`TransferOffer`](OneGame.Sync/TransferOffer.md) | record | An offer another node made to hand entities to a scope on this node; [`AuthorityScope.TransferGate`](OneGame.Sync/AuthorityScope.md#m-transfergate) may veto it. |
| [`TransferState`](OneGame.Sync/TransferState.md) | enum | Where an authority transfer stands. |
| [`Trust`](OneGame.Sync/Trust.md) | struct | What a connection may do and who it is. |
