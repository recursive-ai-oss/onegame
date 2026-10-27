namespace OneGame.Corpus;

/// <summary>Runs the chosen games and writes the corpus and its manifest.</summary>
internal static class CorpusRun
{
    /// <summary>Generates the corpus under <see cref="CorpusOptions.Out"/> and returns the JSON report.</summary>
    public static string Generate(CorpusOptions options)
    {
        var root = Path.GetFullPath(options.Out);
        var sink = new Sink(root, options.Budgets(), options.Strides());
        foreach (var game in options.Games)
        {
            switch (game)
            {
                case "lanterns":
                    LanternsRun.Run(options, sink);
                    break;
                case "bricks":
                    BricksRun.Run(options, sink);
                    break;
                default:
                    SolitaireRun.Run(options, sink);
                    break;
            }
        }

        return CorpusOptions.Report(sink, options.Games, sink.WriteManifest());
    }
}
