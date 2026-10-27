using Godot;

namespace ArknightsDemo;

/// <summary>Anything a tower or an enemy can hit.</summary>
public interface IDamageable
{
    int Hp { get; }
    int Defense { get; }
    int Resist { get; }
    float DodgeChance { get; }
    bool IsDead { get; }
    void ApplyDamage(int amount);
}

/// <summary>Public surface of a deployed operator.</summary>
public interface IOperator
{
    string Id { get; }
    string Name { get; }
    Profession Profession { get; }
    Vector2I Cell { get; }
    Facing Facing { get; }
    int Hp { get; }
    int MaxHp { get; }
    bool IsDead { get; }
    bool IsDeployed { get; }
    OperatorStats Stats { get; }
    void Tick(float dt, BattleSimulation sim);
}
