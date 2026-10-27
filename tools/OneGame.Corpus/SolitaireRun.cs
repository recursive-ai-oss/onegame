using OneGame.Core;
using OneGame.Sync;
using Solitaire;

namespace OneGame.Corpus;

/// <summary>Solitaire: a bot that prefers foundation moves, then moves that turn a card over, with some noise, draws when it has nothing, and deals again when it wins or gets stuck.</summary>
internal sealed class SolitaireRun : SoloRun
{
    private const int ThinkEvery = 6;
    private const int StuckDraws = 60;

    private int _draws;
    private long _nextAction;

    public static void Run(CorpusOptions options, Sink sink) => new SolitaireRun().Execute(options, sink);

    protected override string Game => "solitaire";

    protected override int SampleTicks => 2000;

    protected override void Register(EntityRegistry registry) => new SolitaireGame().RegisterEntities(registry);

    protected override void Seed(World world) => world.Add(new Table()).NewGame(1);

    protected override void Play(ReplicaScope view, Rng rng, long tick)
    {
        if (view.World.Entities.Count == 0 || view.World.Entities[0] is not Table table || tick < _nextAction)
        {
            return;
        }

        _nextAction = tick + ThinkEvery + rng.Range(0, 4);
        if (table.IsWon || _draws >= StuckDraws)
        {
            table.Send.NewGame(rng.OneIn(2) ? 1 : 3);
            _draws = 0;
            _nextAction = tick + 30;
            return;
        }

        var bestScore = int.MinValue;
        var (bestFrom, bestTo, bestCount) = (-1, -1, 0);
        for (var from = Piles.Waste; from < Piles.Count; from++)
        {
            var pile = table.Pile(from);
            var most = from == Piles.Waste || Piles.IsFoundation(from) ? Math.Min(1, pile.Count) : pile.Count;
            for (var count = 1; count <= most; count++)
            {
                if (Piles.IsTableau(from) && !Cards.IsFaceUp(pile[pile.Count - count]))
                {
                    break;
                }

                for (var to = Piles.Foundation; to < Piles.Count; to++)
                {
                    if (to == from || !table.CanMove(from, to, count))
                    {
                        continue;
                    }

                    var score = rng.Range(0, 30);
                    if (Piles.IsFoundation(to))
                    {
                        score += Piles.IsFoundation(from) ? -50 : 100;
                    }
                    else if (Piles.IsTableau(from) && count == FaceUp(pile) && pile.Count > count)
                    {
                        score += 60;
                    }
                    else if (from == Piles.Waste)
                    {
                        score += 40;
                    }
                    else if (Piles.IsFoundation(from))
                    {
                        score -= 40;
                    }

                    if (score > bestScore)
                    {
                        (bestScore, bestFrom, bestTo, bestCount) = (score, from, to, count);
                    }
                }
            }
        }

        if (bestFrom >= 0 && !rng.OneIn(25))
        {
            table.Send.Move(bestFrom, bestTo, bestCount);
            _draws = 0;
        }
        else
        {
            table.Send.Draw();
            _draws++;
        }
    }

    private static int FaceUp(SyncList<int> pile)
    {
        var n = 0;
        for (var i = pile.Count - 1; i >= 0 && Cards.IsFaceUp(pile[i]); i--)
        {
            n++;
        }

        return n;
    }
}
