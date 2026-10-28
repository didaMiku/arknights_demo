using Godot;

namespace ArknightsDemo;

public struct AttackTarget
{
    public EnemyBase Enemy;
    public OperatorBase Ally;
    public bool IsEnemy => Enemy != null;
    public bool IsAlly => Ally != null;
}

/// <summary>
/// Template method: <see cref="Tick"/> fixes the order of operations
/// (timers -> stun gate -> attack cooldown -> acquire -> strike -> reset cooldown).
/// Subclasses only answer "who do I hit" and "how do I hit them".
/// </summary>
public abstract class OperatorBase : IOperator, IDamageable
{
    public string Id { get; protected set; } = "";
    public string Name { get; protected set; } = "";
    public Profession Profession { get; protected set; }
    public AttackKind AttackKind { get; protected set; }
    public string RangeShape { get; protected set; } = "front1";
    public int Cost { get; protected set; }
    public string SkillId { get; protected set; } = "";
    public string SkillName { get; protected set; } = "";
    public string SkillDesc { get; protected set; } = "";
    public float SkillChargeTime { get; protected set; } = 20f;

    public Vector2I Cell { get; set; } = new(-1, -1);
    public Facing Facing { get; set; } = Facing.Right;
    public bool IsDeployed => Cell.X >= 0;

    public int Hp { get; protected set; }
    public int MaxHp { get; protected set; }
    public int Defense => Stats.Defense;
    public int Resist => Stats.Resist;
    public float DodgeChance => 0f;
    public bool IsDead => Hp <= 0;

    public SkillBase Skill { get; set; }
    public List<EnemyBase> Blocking { get; } = new();
    public float AttackTimer;

    /// <summary>Set on damage, decays over ~0.14s; the view uses it for the white hit flash.</summary>
    public float HitFlash;

    /// <summary>Counts down after deployment so the view can play a pop-in scale animation.</summary>
    public float DeployTimer;

    /// <summary>Counts down after an attack; the view uses it to drive the swing pose.</summary>
    public float AttackAnim;

    public float DeployAnim => 1f - Mathf.Clamp(DeployTimer / 0.45f, 0f, 1f);

    private readonly OperatorStats _base = new();
    private readonly List<StatBuff> _buffs = new();
    private readonly List<StatusEffect> _effects = new();
    private IStatSource _source;
    private OperatorStats _cached;
    private bool _dirty = true;

    public IReadOnlyList<StatBuff> Buffs => _buffs;
    public IReadOnlyList<StatusEffect> Effects => _effects;
    public bool IsStunned => _effects.Exists(e => e.Type == StatusType.Stun);

    public OperatorStats Stats
    {
        get
        {
            if (_dirty) { _cached = _source.GetStats(); _dirty = false; }
            return _cached;
        }
    }

    public virtual bool CanAct => !IsDead && !IsStunned;

    protected OperatorBase() => RebuildSource();

    public void Initialize(OperatorTemplate t)
    {
        Id = t.Id;
        Name = t.Name;
        Profession = t.Profession;
        AttackKind = t.AttackKind;
        RangeShape = t.RangeShape;
        Cost = t.Cost;
        SkillId = t.SkillId;
        SkillName = t.SkillName;
        SkillDesc = t.SkillDesc;
        SkillChargeTime = t.SkillChargeTime;

        _base.MaxHp = t.MaxHp;
        _base.Attack = t.Attack;
        _base.Defense = t.Defense;
        _base.Resist = t.Resist;
        _base.Block = t.Block;
        _base.AttackInterval = t.AttackInterval;

        MaxHp = t.MaxHp;
        Hp = t.MaxHp;

        RebuildSource();
        AttackTimer = 0.2f;
    }

    public void AddBuff(StatBuff buff)
    {
        buff.Remaining = buff.Duration;
        _buffs.Add(buff);
        RebuildSource();
    }

    public void AddStatus(StatusType type, float duration, float power = 1f)
    {
        _effects.Add(new StatusEffect { Type = type, Remaining = duration, Power = power });
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        Hp = Mathf.Min(MaxHp, Hp + amount);
    }

    public void ApplyDamage(int amount)
    {
        if (IsDead) return;
        Hp -= amount;
        HitFlash = 0.14f;
        if (Hp <= 0)
        {
            Hp = 0;
            OnDeath();
        }
    }

    protected virtual void OnDeath() { }

    private void RebuildSource()
    {
        IStatSource s = new BaseStatSource(_base);
        foreach (var b in _buffs) s = b.Wrap(s);
        _source = s;
        _dirty = true;
    }

    public virtual void Tick(float dt, BattleSimulation sim)
    {
        HitFlash = Mathf.Max(0f, HitFlash - dt);
        DeployTimer = Mathf.Max(0f, DeployTimer - dt);
        AttackAnim = Mathf.Max(0f, AttackAnim - dt);

        if (IsDead) return;

        UpdateTimers(dt);
        if (!CanAct) return;

        AttackTimer -= dt;
        if (AttackTimer > 0f) return;

        var target = AcquireTarget(sim);
        if (target == null) return;

        PerformAttack(target.Value, sim);
        AttackTimer = Mathf.Max(0.15f, Stats.AttackInterval);
        AttackAnim = 0.3f;
    }

    private void UpdateTimers(float dt)
    {
        bool statChanged = false;

        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            var b = _buffs[i];
            if (b.Permanent) continue;
            b.Remaining -= dt;
            if (b.Remaining <= 0f)
            {
                _buffs.RemoveAt(i);
                statChanged = true;
            }
        }

        for (int i = _effects.Count - 1; i >= 0; i--)
        {
            _effects[i].Remaining -= dt;
            if (_effects[i].Expired) _effects.RemoveAt(i);
        }

        if (statChanged) RebuildSource();
        Skill?.Tick(dt);
    }

    public List<Vector2I> RangeOffsets() => RangeShapes.Get(RangeShape, Facing);

    public bool IsInRange(Vector2I target)
    {
        var off = target - Cell;
        foreach (var o in RangeOffsets())
            if (o == off) return true;
        return false;
    }

    protected abstract AttackTarget? AcquireTarget(BattleSimulation sim);

    protected abstract void PerformAttack(AttackTarget target, BattleSimulation sim);
}
