namespace DevhubWebApp.Utilities;

/// <summary>
/// Bounded undo/redo history for a single value. Tools push a snapshot after each committed
/// change; typing is not a change, so the caller decides what "committed" means.
/// </summary>
/// <typeparam name="T">Snapshot type. Immutable values work best.</typeparam>
public sealed class UndoRedoHistory<T>
{
    private readonly List<T> _states = [];
    private readonly int _capacity;
    private int _index = -1;

    /// <param name="capacity">Maximum number of snapshots retained. Oldest are dropped first.</param>
    public UndoRedoHistory(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 2);
        _capacity = capacity;
    }

    public bool CanUndo => _index > 0;

    public bool CanRedo => _index >= 0 && _index < _states.Count - 1;

    /// <summary>Current snapshot, or <c>default</c> before anything was pushed.</summary>
    public T? Current => _index >= 0 ? _states[_index] : default;

    public int Count => _states.Count;

    /// <summary>
    /// Records a new snapshot. Pushing after an undo discards the redo branch, which is what every
    /// editor does. Pushing a value equal to the current one is ignored.
    /// </summary>
    public void Push(T state)
    {
        if (_index >= 0 && EqualityComparer<T>.Default.Equals(_states[_index], state))
        {
            return;
        }

        if (_index < _states.Count - 1)
        {
            _states.RemoveRange(_index + 1, _states.Count - _index - 1);
        }

        _states.Add(state);

        if (_states.Count > _capacity)
        {
            _states.RemoveAt(0);
        }

        _index = _states.Count - 1;
    }

    /// <summary>Steps back one snapshot and returns it, or <c>default</c> when there is none.</summary>
    public T? Undo() => CanUndo ? _states[--_index] : default;

    /// <summary>Steps forward one snapshot and returns it, or <c>default</c> when there is none.</summary>
    public T? Redo() => CanRedo ? _states[++_index] : default;

    /// <summary>Drops all history and starts again from <paramref name="state"/>.</summary>
    public void Reset(T state)
    {
        _states.Clear();
        _states.Add(state);
        _index = 0;
    }
}

