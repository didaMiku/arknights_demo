using Godot;

namespace ArknightsDemo;

/// <summary>Concrete decorators. Each one only knows how to transform the stats it receives.</summary>
public class AttackUpDecorator : StatDecorator
{
    private readonly float _mult;
    public AttackUpDecorator(IStatSource inner, float mult) : base(inner) => _mult = mult;
    public override OperatorStats GetStats()
    {
        var s = Inner.GetStats();
        s.Attack = Mathf.RoundToInt(s.Attack * _mult);
        return s;
    }
}

public class AttackSpeedDecorator : StatDecorator
{
    private readonly float _percent;
    public AttackSpeedDecorator(IStatSource inner, float percent) : base(inner) => _percent = percent;
    public override OperatorStats GetStats()
    {
        var s = Inner.GetStats();
        s.AttackInterval /= 1f + _percent;
        return s;
    }
}

public class DefenseUpDecorator : StatDecorator
{
    private readonly float _mult;
    public DefenseUpDecorator(IStatSource inner, float mult) : base(inner) => _mult = mult;
    public override OperatorStats GetStats()
    {
        var s = Inner.GetStats();
        s.Defense = Mathf.RoundToInt(s.Defense * _mult);
        return s;
    }
}

public class BlockUpDecorator : StatDecorator
{
    private readonly int _bonus;
    public BlockUpDecorator(IStatSource inner, int bonus) : base(inner) => _bonus = bonus;
    public override OperatorStats GetStats()
    {
        var s = Inner.GetStats();
        s.Block += _bonus;
        return s;
    }
}

/// <summary>Factory helpers so gameplay code never news up a decorator by hand.</summary>
public static class StatBuffs
{
    public static StatBuff AttackUp(float percent, float duration) => new()
    {
        Name = $"攻击+{percent * 100:0}%",
        Duration = duration,
        Wrap = inner => new AttackUpDecorator(inner, 1f + percent),
    };

    public static StatBuff AttackSpeedUp(float percent, float duration) => new()
    {
        Name = $"攻速+{percent * 100:0}%",
        Duration = duration,
        Wrap = inner => new AttackSpeedDecorator(inner, percent),
    };

    public static StatBuff DefenseUp(float percent, float duration) => new()
    {
        Name = $"防御+{percent * 100:0}%",
        Duration = duration,
        Wrap = inner => new DefenseUpDecorator(inner, 1f + percent),
    };

    public static StatBuff BlockUp(int bonus, float duration) => new()
    {
        Name = $"阻挡+{bonus}",
        Duration = duration,
        Wrap = inner => new BlockUpDecorator(inner, bonus),
    };
}
