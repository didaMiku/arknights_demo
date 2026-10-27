namespace ArknightsDemo;

/// <summary>Mutable snapshot of an operator's combat numbers.</summary>
public class OperatorStats
{
    public int MaxHp;
    public int Attack;
    public int Defense;
    public int Resist;
    public int Block;
    public float AttackInterval = 1f;

    public OperatorStats Clone() => (OperatorStats)MemberwiseClone();
}

/// <summary>
/// Decorator pattern - component. Everything that can answer "what are this
/// operator's stats right now" implements this, so buffs can nest arbitrarily.
/// </summary>
public interface IStatSource
{
    OperatorStats GetStats();
}

/// <summary>Base component: the operator's immutable blueprint values.</summary>
public sealed class BaseStatSource : IStatSource
{
    private readonly OperatorStats _base;
    public BaseStatSource(OperatorStats baseStats) => _base = baseStats;
    public OperatorStats GetStats() => _base.Clone();
}

/// <summary>Decorator pattern - abstract decorator. Each concrete buff wraps the source before it.</summary>
public abstract class StatDecorator : IStatSource
{
    protected readonly IStatSource Inner;
    protected StatDecorator(IStatSource inner) => Inner = inner;
    public abstract OperatorStats GetStats();
}

/// <summary>
/// A timed stat buff. The <see cref="Wrap"/> factory is what makes the decorator
/// chain rebuildable: on expiry the chain is reconstructed from scratch instead of
/// trying to unlink a node in the middle.
/// </summary>
public class StatBuff
{
    public string Name = "";
    public float Duration;
    public float Remaining;
    public Func<IStatSource, IStatSource> Wrap;
    public bool Permanent => Duration <= 0f;
    public bool Expired => !Permanent && Remaining <= 0f;
}
