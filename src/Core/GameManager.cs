using Godot;

namespace ArknightsDemo;

/// <summary>Singleton: cross-scene session state (which stage, which squad, last result).</summary>
public sealed class GameManager
{
    public static GameManager Instance { get; } = new();
    private GameManager() { }

    public int StageIndex;
    public List<string> Squad { get; } = new();
    public BattleSimulation Battle;
    public int BestStars;

    public StageData CurrentStageData =>
        ConfigManager.Instance.Stages.Count == 0
            ? null
            : ConfigManager.Instance.Stages[Mathf.Clamp(StageIndex, 0, ConfigManager.Instance.Stages.Count - 1)];

    /// <summary>Default squad: two of every profession, so a fresh start is playable.</summary>
    public void BuildDefaultSquad()
    {
        if (Squad.Count > 0) return;

        var byProfession = new Dictionary<Profession, int>();
        foreach (var op in ConfigManager.Instance.Registry.Operators.Values)
        {
            byProfession.TryGetValue(op.Profession, out int count);
            if (count >= 2) continue;
            byProfession[op.Profession] = count + 1;
            Squad.Add(op.Id);
        }
    }
}
