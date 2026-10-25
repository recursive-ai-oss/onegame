using OneGame.Host;
using Solitaire;

var services = Launch.CreateServices(args);
try
{
    return GameApp.Run(new SolitaireGame(services), Launch.WithRandomSeed(args));
}
finally
{
    (services.Sounds as IDisposable)?.Dispose();
    services.Store?.Dispose();
}
