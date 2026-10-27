# onegame-corpus

A corpus of real onegame payloads for compression work (for example, benchmarking [onearchive](https://github.com/recursive-ai-oss/onearchive)).

It plays three of the engine's own samples with seeded bots and writes the bytes the engine really encodes and sends. Nothing is built by hand: each payload is captured at the point where the sync layer sends it, or comes from the engine's own `Save`, `StartRecording` and subscribe calls.

```
dotnet run --project tools/OneGame.Corpus -- --seed 1 --out corpus
dotnet run --project tools/OneGame.Corpus -- --seed 1 --out sample --sample
```

Options: `--seed N` (default 1), `--out DIR`, `--games lanterns,bricks,solitaire`, `--sample` (a few hundred KB in total), `--ticks N` (run exactly N ticks per game), `--budget kind=SIZE` (byte budget for one kind in every game, for example `--budget frames=4M`), `--piece-bytes N` (the size above which the sync layer sends a message in pieces; default 256 KiB, minimum 1024), `--bots N` (Lanterns players, default 6). It prints a JSON report with the files and bytes per game and kind and the SHA-256 of the manifest.

## Layout

```
<out>/<game>/<kind>/<NNNNNN>.bin     one file per payload
<out>/manifest.tsv                   game, kind, file, size, sha256, schema, tick
```

| kind | what each file is |
|---|---|
| `frames` | one message the authority sent to one subscriber for one tick, as sent: empty and tiny ones included. A message longer than the piece size appears as its pieces, each as sent. |
| `snapshots` | the snapshot message a subscriber receives on subscribing: first joins, and strangers who join and leave during the run (late join and reconnect). |
| `saves` | `AuthorityScope.Save()` output (`OGSV`). |
| `recordings` | `Recording.ToBytes()` (`OGRP`), in segments of a few hundred ticks. |
| `transfer` | the offer and commit messages that hand entities from one island server to another (Lanterns only). |

`schema` is the container format the codec writes plus the highest entity schema version in the game's worlds: `snap2.e1` (view snapshot format 2), `save1.e1`, `rec1.e1`. Frames and transfer bundles have no container version, so they read `frame.e1` and `xfer.e1`. `tick` is the cluster tick at which the payload was taken.

## The games

- **lanterns**: two island servers and a master, as in the sample's servers (peer links with relay trust, relays to players, bridge hand-over, swap escrow, saves under `Durability.AfterSave`), plus six bots that gather lanterns, cross the bridge and swap. Everything runs in one process on the in-memory transport, so a run needs no sockets, threads or clock.
- **bricks**, **solitaire**: one authority and one player node subscribed over a tapped link; the bot plays through the replica with ordinary commands.

## Determinism and size

The same seed on the same commit gives byte-identical files and manifest. Bot choices come from the seed through the tool's own generator, and the worlds' seeds come from it too, so another seed gives different deals, lantern placement and play. Defaults give roughly 20 MB per game across all kinds (frames are most of it and are tiny, so a default run writes hundreds of thousands of files; lower `--budget frames=` for fewer). Transfer bundles are small and few, and never fill their budget.

Tests: `tests/OneGame.Corpus.Tests` runs the tool in `--sample` mode and checks the manifest.

Licensed under MIT OR Apache-2.0.
