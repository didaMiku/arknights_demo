namespace ArknightsDemo;

/// <summary>Prototype pattern: enemy blueprint cloned into live instances.</summary>
public class EnemyTemplate
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";

    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Resist { get; set; }

    /// <summary>Grid cells per second.</summary>
    public float Speed { get; set; } = 1f;
    public float AttackInterval { get; set; } = 1.5f;

    /// <summary>How many block slots this enemy consumes.</summary>
    public int BlockCost { get; set; } = 1;

    public bool Flying { get; set; }
    public float Dodge { get; set; }
    public int SelfDestructDamage { get; set; }
    public bool IsElite { get; set; }
    public float Scale { get; set; } = 1f;
    public string Shape { get; set; } = "circle";

    public EnemyTemplate Clone() => (EnemyTemplate)MemberwiseClone();
}
