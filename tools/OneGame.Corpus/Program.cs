using OneGame.Corpus;

try
{
    Console.WriteLine(CorpusRun.Generate(CorpusOptions.Parse(args)));
    return 0;
}
catch (ArgumentException e)
{
    Console.Error.WriteLine(e.Message);
    return 2;
}
