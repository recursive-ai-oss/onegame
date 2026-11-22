using System;
using System.Linq;
using ClusterFixture;
using OneGame.Host;

// --follow-later=N is the fixture's own flag: the game subscribes to scope N at run time.
var follow = args.FirstOrDefault(a => a.StartsWith("--follow-later=", StringComparison.Ordinal));
return GameApp.Run(new ArenaGame { FollowLater = follow is null ? 0u : uint.Parse(follow["--follow-later=".Length..]) }, args.Where(a => a != follow).ToArray());
