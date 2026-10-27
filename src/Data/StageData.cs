namespace ArknightsDemo;

/// <summary>Plain DTOs mirroring assets/data/*.json. Kept free of Godot types so
/// System.Text.Json can deserialize them without custom converters.</summary>
public class StageData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Hint { get; set; } = "";
    public int Cols { get; set; }
    public int Rows { get; set; }

    /// <summary>One string per row. Characters: . empty, G ground, H highland, W wall, S spawn, T goal.</summary>
    public List<string> Cells { get; set; } = new();
    public List<PointData> Spawns { get; set; } = new();
    public List<PointData> Goals { get; set; } = new();
    public List<List<PointData>> Paths { get; set; } = new();
    public List<WaveData> Waves { get; set; } = new();
}

public class PointData
{
    public int Col { get; set; }
    public int Row { get; set; }
}

public class WaveData
{
    public float Time { get; set; }
    public string Label { get; set; } = "";
    public List<GroupData> Groups { get; set; } = new();
}

public class GroupData
{
    public string Enemy { get; set; } = "";
    public int Count { get; set; }
    public float Interval { get; set; } = 1.5f;
    public float Delay { get; set; }
    public int PathIndex { get; set; }
}

public class OperatorTable
{
    public List<OperatorTemplate> Operators { get; set; } = new();
}

public class EnemyTable
{
    public List<EnemyTemplate> Enemies { get; set; } = new();
}

public class StageTable
{
    public List<StageData> Stages { get; set; } = new();
}
