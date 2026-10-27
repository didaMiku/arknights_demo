using Godot;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArknightsDemo;

/// <summary>Singleton: one authoritative in-memory copy of all JSON tables.</summary>
public sealed class ConfigManager
{
    public static ConfigManager Instance { get; } = new();
    private ConfigManager() { }

    public PrototypeRegistry Registry { get; } = new();
    public List<StageData> Stages { get; } = new();
    public bool Loaded { get; private set; }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public void LoadAll()
    {
        if (Loaded) return;

        var operators = ReadJson<OperatorTable>("res://assets/data/operators.json");
        foreach (var op in operators.Operators) Registry.RegisterOperator(op);

        var enemies = ReadJson<EnemyTable>("res://assets/data/enemies.json");
        foreach (var enemy in enemies.Enemies) Registry.RegisterEnemy(enemy);

        var stages = ReadJson<StageTable>("res://assets/data/stages.json");
        Stages.AddRange(stages.Stages);

        Loaded = true;
        GD.Print($"[Config] operators={Registry.Operators.Count} enemies={Registry.Enemies.Count} stages={Stages.Count}");
    }

    private static T ReadJson<T>(string path)
    {
        string text = Godot.FileAccess.GetFileAsString(path);
        if (string.IsNullOrEmpty(text))
            throw new InvalidOperationException($"cannot read config: {path}");
        return JsonSerializer.Deserialize<T>(text, JsonOpts);
    }
}
