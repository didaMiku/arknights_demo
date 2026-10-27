using Godot;

namespace ArknightsDemo;

public class SpawnEvent
{
    public string EnemyId = "";
    public float Time;
    public int PathIndex;
    public string WaveLabel = "";
}

/// <summary>
/// Composite pattern: a wave is a tree. Leaves are individual spawn templates,
/// groups aggregate children, and the root exposes the same Collect() call.
/// </summary>
public abstract class WaveNode
{
    public string Label = "";
    public abstract void Collect(float elapsed, List<SpawnEvent> output);
}

public class WaveLeaf : WaveNode
{
    public string EnemyId = "";
    public int Count = 1;
    public float Interval = 1.5f;
    public float Start;
    public int PathIndex;

    public override void Collect(float elapsed, List<SpawnEvent> output)
    {
        for (int i = 0; i < Count; i++)
        {
            output.Add(new SpawnEvent
            {
                EnemyId = EnemyId,
                Time = Start + i * Interval,
                PathIndex = PathIndex,
                WaveLabel = Label,
            });
        }
    }
}

public class WaveGroup : WaveNode
{
    private readonly List<WaveNode> _children = new();

    public IReadOnlyList<WaveNode> Children => _children;
    public int Count => _children.Count;

    public WaveGroup Add(WaveNode node)
    {
        _children.Add(node);
        return this;
    }

    public override void Collect(float elapsed, List<SpawnEvent> output)
    {
        foreach (var child in _children) child.Collect(elapsed, output);
    }
}
