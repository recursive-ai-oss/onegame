using System.Threading;

namespace Solitaire.Tests;

/// <summary>One step of a solution: a draw (or recycle) or a move of some cards.</summary>
internal readonly record struct SolverMove(bool IsDraw, int From, int To, int Count)
{
    public override string ToString() => IsDraw ? "draw" : $"{From}->{To} x{Count}";
}

/// <summary>
/// A depth-first search for a winning line from a dealt table. It is used by the tests to find a deal that can be won and a move
/// list that wins it; the rules here are written separately from the game's, and the game itself checks every move when the list is
/// played back, so a mistake in either shows up as a refused move.
/// </summary>
internal sealed class Solver
{
    private readonly int _drawMode;
    private readonly int _maxNodes;
    private readonly int _maxRecycles;
    private readonly HashSet<ulong> _seen = [];
    private readonly List<SolverMove> _path = [];
    private int _nodes;

    private Solver(int drawMode, int maxNodes, int maxRecycles)
    {
        _drawMode = drawMode;
        _maxNodes = maxNodes;
        _maxRecycles = maxRecycles;
    }

    /// <summary>Searches from the table's current piles. Returns the moves of a win, or null when none was found within the node budget.</summary>
    public static List<SolverMove>? Solve(Table table, int maxNodes = 400_000, int maxRecycles = 2)
    {
        var piles = new List<int>[Piles.Count];
        for (var p = 0; p < piles.Length; p++)
        {
            piles[p] = [.. table.Pile(p)];
        }

        var solver = new Solver(table.DrawMode, maxNodes, maxRecycles);
        List<SolverMove>? result = null;
        var thread = new Thread(() => result = solver.Search(piles, 0) ? [.. solver._path] : null, 256 * 1024 * 1024);
        thread.Start();
        thread.Join();
        return result;
    }

    private bool Search(List<int>[] piles, int recycles)
    {
        if (piles[Piles.Foundation].Count + piles[Piles.Foundation + 1].Count + piles[Piles.Foundation + 2].Count + piles[Piles.Foundation + 3].Count == 52)
        {
            return true;
        }

        if (++_nodes > _maxNodes || !_seen.Add(Hash(piles)))
        {
            return false;
        }

        foreach (var move in Moves(piles, recycles))
        {
            var next = Apply(piles, move, recycles, out var nextRecycles);
            _path.Add(move);
            if (Search(next, nextRecycles))
            {
                return true;
            }

            _path.RemoveAt(_path.Count - 1);
            if (_nodes > _maxNodes)
            {
                return false;
            }
        }

        return false;
    }

    private List<SolverMove> Moves(List<int>[] piles, int recycles)
    {
        var scored = new List<(int Score, SolverMove Move)>();
        for (var from = Piles.Waste; from < Piles.Count; from++)
        {
            var source = piles[from];
            if (source.Count == 0)
            {
                continue;
            }

            if (from != Piles.Waste && !Piles.IsTableau(from))
            {
                continue;
            }

            var top = source[^1];
            var found = piles[Piles.Foundation + Cards.Suit(top)];
            if (Cards.Rank(top) == found.Count + 1)
            {
                scored.Add((50, new SolverMove(false, from, Piles.Foundation + Cards.Suit(top), 1)));
            }
        }

        for (var from = Piles.Waste; from < Piles.Count; from++)
        {
            var source = piles[from];
            if (source.Count == 0 || Piles.IsFoundation(from))
            {
                continue;
            }

            var firstUp = source.Count;
            while (firstUp > 0 && Cards.IsFaceUp(source[firstUp - 1]))
            {
                firstUp--;
            }

            var lowest = from == Piles.Waste ? source.Count - 1 : firstUp;
            for (var i = lowest; i < source.Count; i++)
            {
                var card = source[i];
                var count = source.Count - i;
                for (var to = Piles.Tableau; to < Piles.Count; to++)
                {
                    if (to == from)
                    {
                        continue;
                    }

                    var target = piles[to];
                    var fits = target.Count == 0 ? Cards.Rank(card) == 13 : Cards.IsFaceUp(target[^1]) && Cards.Stacks(target[^1], card);
                    if (!fits)
                    {
                        continue;
                    }

                    if (target.Count == 0 && i == 0)
                    {
                        continue; // a king already at the bottom of a column gains nothing from changing columns
                    }

                    var flips = from != Piles.Waste && i == firstUp && firstUp > 0;
                    var score = from == Piles.Waste ? 30 : flips ? 40 : i == firstUp && firstUp == 0 ? 20 : 10;
                    scored.Add((score, new SolverMove(false, from, to, count)));
                }
            }
        }

        if (piles[Piles.Stock].Count > 0 || (piles[Piles.Waste].Count > 0 && recycles < _maxRecycles))
        {
            scored.Add((1, new SolverMove(true, 0, 0, 0)));
        }

        return [.. scored.OrderByDescending(s => s.Score).Select(s => s.Move)];
    }

    private List<int>[] Apply(List<int>[] piles, SolverMove move, int recycles, out int nextRecycles)
    {
        var next = new List<int>[piles.Length];
        for (var p = 0; p < piles.Length; p++)
        {
            next[p] = [.. piles[p]];
        }

        nextRecycles = recycles;
        if (move.IsDraw)
        {
            if (next[Piles.Stock].Count > 0)
            {
                var n = Math.Min(_drawMode, next[Piles.Stock].Count);
                for (var i = 0; i < n; i++)
                {
                    var c = next[Piles.Stock][^1];
                    next[Piles.Stock].RemoveAt(next[Piles.Stock].Count - 1);
                    next[Piles.Waste].Add(c | Cards.FaceUp);
                }
            }
            else
            {
                nextRecycles++;
                while (next[Piles.Waste].Count > 0)
                {
                    var c = next[Piles.Waste][^1];
                    next[Piles.Waste].RemoveAt(next[Piles.Waste].Count - 1);
                    next[Piles.Stock].Add(c & ~Cards.FaceUp);
                }
            }

            return next;
        }

        var source = next[move.From];
        var moved = source.GetRange(source.Count - move.Count, move.Count);
        source.RemoveRange(source.Count - move.Count, move.Count);
        next[move.To].AddRange(moved);
        if (Piles.IsTableau(move.From) && source.Count > 0 && !Cards.IsFaceUp(source[^1]))
        {
            source[^1] |= Cards.FaceUp;
        }

        return next;
    }

    private static ulong Hash(List<int>[] piles)
    {
        var h = 14695981039346656037UL;
        foreach (var pile in piles)
        {
            h = (h ^ 0xFFUL) * 1099511628211UL;
            foreach (var c in pile)
            {
                h = (h ^ (ulong)c) * 1099511628211UL;
            }
        }

        return h;
    }
}
