using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Behaviour of the bounded undo/redo history shared by every tool workspace.</summary>
public class UndoRedoHistoryTests
{
    [Fact]
    public void NewStack_HasNothingToUndoOrRedo()
    {
        var stack = new UndoRedoHistory<string>(10);

        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
        Assert.Null(stack.Current);
    }

    [Fact]
    public void Undo_AndRedo_WalkTheHistory()
    {
        var stack = new UndoRedoHistory<string>(10);
        stack.Push("one");
        stack.Push("two");
        stack.Push("three");

        Assert.Equal("two", stack.Undo());
        Assert.Equal("one", stack.Undo());
        Assert.False(stack.CanUndo);

        Assert.Equal("two", stack.Redo());
        Assert.Equal("three", stack.Redo());
        Assert.False(stack.CanRedo);
    }

    [Fact]
    public void Push_AfterUndo_DiscardsTheRedoBranch()
    {
        var stack = new UndoRedoHistory<string>(10);
        stack.Push("one");
        stack.Push("two");
        stack.Undo();

        stack.Push("branch");

        Assert.False(stack.CanRedo);
        Assert.Equal("branch", stack.Current);
        Assert.Equal("one", stack.Undo());
    }

    [Fact]
    public void Push_IgnoresAValueEqualToTheCurrentOne()
    {
        var stack = new UndoRedoHistory<string>(10);
        stack.Push("same");
        stack.Push("same");

        Assert.Equal(1, stack.Count);
        Assert.False(stack.CanUndo);
    }

    [Fact]
    public void Push_DropsOldestSnapshotsBeyondCapacity()
    {
        var stack = new UndoRedoHistory<int>(3);
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);
        stack.Push(4);

        Assert.Equal(3, stack.Count);
        Assert.Equal(3, stack.Undo());
        Assert.Equal(2, stack.Undo());
        Assert.False(stack.CanUndo);
    }

    [Fact]
    public void Reset_ClearsHistoryAndStartsFromTheGivenState()
    {
        var stack = new UndoRedoHistory<string>(10);
        stack.Push("one");
        stack.Push("two");

        stack.Reset("fresh");

        Assert.Equal("fresh", stack.Current);
        Assert.False(stack.CanUndo);
        Assert.False(stack.CanRedo);
        Assert.Equal(1, stack.Count);
    }

    [Fact]
    public void Constructor_RejectsUselessCapacity()
        => Assert.Throws<ArgumentOutOfRangeException>(() => new UndoRedoHistory<string>(1));
}

