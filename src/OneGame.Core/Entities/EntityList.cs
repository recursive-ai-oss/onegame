using System.Collections;

namespace OneGame.Core;

/// <summary>
/// The live entities of a world, in id order. An entity removed during the current tick is already gone from this view, although the world
/// keeps it until the tick ends: code that removes an entity and then looks again (or runs a second command in the same tick) never meets it twice.
/// The view follows the world, so it can be kept.
/// </summary>
internal sealed class EntityList : IReadOnlyList<Entity>
{
    private readonly List<Entity> _all;
    private readonly List<Entity> _pendingRemoval;

    internal EntityList(List<Entity> all, List<Entity> pendingRemoval)
    {
        _all = all;
        _pendingRemoval = pendingRemoval;
    }

    // While entities are waiting for removal, positions are served from a compacted copy built once per change, so an indexed loop
    // after a mid-tick Add or Remove costs O(n) in total rather than O(n) per access. The world calls Invalidate when the list changes.
    private readonly List<Entity> _compact = [];
    private bool _compactValid;

    /// <summary>Drops the compacted copy; the world calls this whenever it adds, removes or detaches an entity.</summary>
    internal void Invalidate() => _compactValid = false;

    private List<Entity> Compact()
    {
        if (!_compactValid)
        {
            _compact.Clear();
            for (var i = 0; i < _all.Count; i++)
            {
                if (!_all[i].IsRemoved)
                {
                    _compact.Add(_all[i]);
                }
            }

            _compactValid = true;
        }

        return _compact;
    }

    /// <summary>The number of live entities.</summary>
    public int Count => _pendingRemoval.Count == 0 ? _all.Count : Compact().Count;

    /// <summary>The live entity at a position in id order.</summary>
    /// <param name="index">A position from 0 to <see cref="Count"/> - 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">The position is outside the list.</exception>
    public Entity this[int index] => _pendingRemoval.Count == 0 ? _all[index] : Compact()[index];

    /// <summary>Walks the live entities.</summary>
    public Enumerator GetEnumerator() => new(_all);

    IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Walks live entities; entities added while walking are visited too, removed ones are skipped.</summary>
    internal struct Enumerator : IEnumerator<Entity>
    {
        private readonly List<Entity> _all;
        private int _next;
        private Entity? _current;

        internal Enumerator(List<Entity> all)
        {
            _all = all;
            _next = 0;
            _current = null;
        }

        /// <summary>The entity the walk stands on.</summary>
        public readonly Entity Current => _current!;

        readonly object IEnumerator.Current => _current!;

        /// <summary>Moves to the next live entity.</summary>
        public bool MoveNext()
        {
            while (_next < _all.Count)
            {
                var e = _all[_next++];
                if (!e.IsRemoved)
                {
                    _current = e;
                    return true;
                }
            }

            _current = null;
            return false;
        }

        /// <summary>Not supported.</summary>
        public readonly void Reset() => throw new NotSupportedException();

        /// <inheritdoc />
        public readonly void Dispose()
        {
        }
    }
}
