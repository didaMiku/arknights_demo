using Godot;

namespace ArknightsDemo;

/// <summary>
/// Builder pattern: assembling a stage is a multi-step, order-sensitive job
/// (map -> paths -> waves -> compiled spawn list). Callers get one readable chain.
/// </summary>
public class StageBuilder
{
    private StageData _data;
    private readonly StageRuntime _stage = new();
    private readonly List<string> _steps = new();

    public StageBuilder Load(StageData data)
    {
        _data = data;
        _stage.Id = data.Id;
        _stage.Name = data.Name;
        _stage.Description = data.Description;
        _stage.Hint = data.Hint;
        return this;
    }

    public StageBuilder BuildMap()
    {
        _stage.Map.Initialize(_data.Cols, _data.Rows);

        for (int r = 0; r < _data.Rows; r++)
        {
            string line = r < _data.Cells.Count ? _data.Cells[r] : "";
            for (int c = 0; c < _data.Cols && c < line.Length; c++)
            {
                var type = line[c] switch
                {
                    'G' => CellType.Ground,
                    'H' => CellType.Highland,
                    'W' => CellType.Wall,
                    'S' => CellType.Spawn,
                    'T' => CellType.Goal,
                    _ => CellType.Empty,
                };
                _stage.Map.Set(c, r, type);
                if (type == CellType.Spawn) _stage.Map.Spawns.Add(new Vector2I(c, r));
                if (type == CellType.Goal) _stage.Map.Goals.Add(new Vector2I(c, r));
            }
        }

        _steps.Add($"map {_data.Cols}x{_data.Rows}");
        return this;
    }

    public StageBuilder BuildPaths()
    {
        foreach (var raw in _data.Paths)
        {
            var nodes = new List<Vector2I>(raw.Count);
            foreach (var p in raw) nodes.Add(new Vector2I(p.Col, p.Row));

            // Waypoints in JSON are corners only. Expand them into every cell the lane
            // crosses, so blocking, lane-aware AI and the walk animation all agree.
            var path = ExpandPath(nodes);
            _stage.Map.Paths.Add(path);

            foreach (var p in path)
            {
                var t = _stage.Map.At(p);
                if (t is CellType.Empty or CellType.Wall)
                    _stage.Map.Set(p.X, p.Y, CellType.Ground);
            }
        }

        _steps.Add($"paths {_stage.Map.Paths.Count}");
        return this;
    }

    private static List<Vector2I> ExpandPath(List<Vector2I> nodes)
    {
        var result = new List<Vector2I>();
        if (nodes.Count == 0) return result;

        result.Add(nodes[0]);
        for (int i = 1; i < nodes.Count; i++)
        {
            var current = nodes[i - 1];
            var target = nodes[i];

            while (current != target)
            {
                // Lanes are authored with axis-aligned segments, so a step is always
                // one cell horizontally or vertically.
                int dx = Math.Sign(target.X - current.X);
                int dy = Math.Sign(target.Y - current.Y);
                current = new Vector2I(current.X + dx, current.Y + dy);
                result.Add(current);
            }
        }

        return result;
    }

    public StageBuilder BuildWaves()
    {
        foreach (var wave in _data.Waves)
        {
            var group = new WaveGroup { Label = wave.Label };
            foreach (var g in wave.Groups)
            {
                group.Add(new WaveLeaf
                {
                    EnemyId = g.Enemy,
                    Count = g.Count,
                    Interval = g.Interval,
                    Start = wave.Time + g.Delay,
                    PathIndex = g.PathIndex,
                    Label = wave.Label,
                });
            }
            _stage.Waves.Add(group);
            _stage.WaveStartTimes.Add(wave.Time);
        }

        _steps.Add($"waves {_stage.Waves.Count}");
        return this;
    }

    public StageRuntime Build()
    {
        var collected = new List<SpawnEvent>();
        foreach (var wave in _stage.Waves) wave.Collect(0f, collected);
        collected.Sort((a, b) => a.Time.CompareTo(b.Time));

        _stage.Spawns = collected;
        _stage.TotalDuration = collected.Count > 0 ? collected[^1].Time : 0f;

        GD.Print($"[StageBuilder] {_stage.Id} => {string.Join(" | ", _steps)} | spawns={_stage.Spawns.Count}");
        return _stage;
    }
}
