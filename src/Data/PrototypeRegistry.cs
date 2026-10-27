namespace ArknightsDemo;

/// <summary>
/// Prototype registry. Holds one canonical instance per id and hands out clones,
/// so callers never pay the JSON parsing cost twice.
/// </summary>
public class PrototypeRegistry
{
    private readonly Dictionary<string, OperatorTemplate> _operators = new();
    private readonly Dictionary<string, EnemyTemplate> _enemies = new();

    public IReadOnlyDictionary<string, OperatorTemplate> Operators => _operators;
    public IReadOnlyDictionary<string, EnemyTemplate> Enemies => _enemies;

    public void RegisterOperator(OperatorTemplate template) => _operators[template.Id] = template;
    public void RegisterEnemy(EnemyTemplate template) => _enemies[template.Id] = template;

    public OperatorTemplate CloneOperator(string id)
    {
        if (!_operators.TryGetValue(id, out var t))
            throw new KeyNotFoundException($"unknown operator id: {id}");
        return t.Clone();
    }

    public EnemyTemplate CloneEnemy(string id)
    {
        if (!_enemies.TryGetValue(id, out var t))
            throw new KeyNotFoundException($"unknown enemy id: {id}");
        return t.Clone();
    }

    public bool HasOperator(string id) => _operators.ContainsKey(id);
    public bool HasEnemy(string id) => _enemies.ContainsKey(id);
}
