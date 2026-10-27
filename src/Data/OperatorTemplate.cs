namespace ArknightsDemo;

/// <summary>
/// Prototype pattern: a fully configured operator blueprint. Runtime units are produced by
/// <see cref="Clone"/>, never by re-parsing the JSON table.
/// </summary>
public class OperatorTemplate
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public Profession Profession { get; set; }
    public AttackKind AttackKind { get; set; }
    public string RangeShape { get; set; } = "front1";

    public int MaxHp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int Resist { get; set; }
    public int Block { get; set; }
    public float AttackInterval { get; set; } = 1f;
    public int Cost { get; set; }
    public float RedeployTime { get; set; } = 10f;

    public string SkillId { get; set; } = "";
    public string SkillName { get; set; } = "";
    public string SkillDesc { get; set; } = "";
    public float SkillChargeTime { get; set; } = 20f;

    public OperatorTemplate Clone() => (OperatorTemplate)MemberwiseClone();
}
