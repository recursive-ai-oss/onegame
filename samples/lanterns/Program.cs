using Lanterns;
using OneGame.Host;

// Single player by default; with --connect <address> --scope N --also-scopes M,3 the same game is a client of the island servers.
return GameApp.Run(new LanternsGame(), args);
