using Godot;

namespace ArknightsDemo;

/// <summary>Built, playable stage. Produced by <see cref="StageBuilder"/>, never hand-assembled.</summary>
public class StageRuntime
{
    public string Id = "";
    public string Name = "";
    public string Description = "";
    public string Hint = "";

    public GridMap Map = new();
    public List<WaveNode> Waves = new();
    public List<SpawnEvent> Spawns = new();
    public float TotalDuration;

    public int WaveCount => Waves.Count;

    /// <summary>Absolute clock time at which each wave begins.</summary>
    public List<float> WaveStartTimes = new();
}
