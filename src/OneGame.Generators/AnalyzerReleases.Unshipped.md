; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
OG0001 | OneGame | Error | Floating-point type in synced state
OG0002 | OneGame | Error | Floating-point arithmetic in sim code
OG0003 | OneGame | Error | Iteration order is not defined
OG0004 | OneGame | Error | Clock in sim code
OG0005 | OneGame | Error | Threads or tasks in sim code
OG0006 | OneGame | Error | I/O in sim code
OG0007 | OneGame | Error | Random source is not a named stream
OG0008 | OneGame | Error | View code changes simulation state
OG0009 | OneGame | Error | Synced type has no codec
OG0010 | OneGame | Error | Entity declaration is not supported
OG0011 | OneGame | Error | Too many synced properties
OG0012 | OneGame | Error | Synced member is declared wrongly
OG0013 | OneGame | Error | Command is declared wrongly
OG0014 | OneGame | Error | Migration is declared wrongly
