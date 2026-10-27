using Godot;

namespace ArknightsDemo;

public enum EnemyState { Moving, Blocked, Dead, Done }

/// <summary>
/// Enemy runtime. Movement is data-driven (template + path), only the blocking
/// interaction is special-cased.
/// </summary>
public class EnemyBase : IDamageable
{
    public EnemyTemplate Template { get; private set; }
    public string Id => Template.Id;
    public string Name => Template.Name;

    public int Hp { get; private set; }
    public int MaxHp { get; private set; }
    public int Defense => Template.Defense;
    public int Resist => Template.Resist;
    public float DodgeChance => Template.Dodge;
    public bool IsDead => Hp <= 0;

    public bool ReachedGoal;
    public bool Counted;

    public List<Vector2I> Path { get; private set; }
    public int Waypoint;
    public int PathIndex;
    public Vector2 Position;

    /// <summary>Accumulated travel distance, used to decide which enemy is closest to the goal.</summary>
    public float Progress;

    public OperatorBase Blocker;
    public float AttackTimer;
    public EnemyState State = EnemyState.Moving;

    public List<StatusEffect> Effects { get; } = new();

    public Vector2I Cell => new(Mathf.RoundToInt(Position.X), Mathf.RoundToInt(Position.Y));

    public float SlowFactor
    {
        get
        {
            float slow = 0f;
            foreach (var e in Effects)
                if (e.Type == StatusType.Slow) slow = Mathf.Max(slow, e.Power);
            return Mathf.Clamp(1f - slow, 0.2f, 1f);
        }
    }

    public void Initialize(EnemyTemplate template, List<Vector2I> path, int pathIndex)
    {
        Template = template.Clone();
        MaxHp = template.MaxHp;
        Hp = template.MaxHp;
        Path = path;
        PathIndex = pathIndex;
        Waypoint = 1;
        Position = new Vector2(path[0].X, path[0].Y);
    }

    public void ApplyDamage(int amount)
    {
        if (IsDead) return;
        Hp -= amount;
        if (Hp <= 0) Hp = 0;
    }

    public void Tick(float dt, BattleSimulation sim)
    {
        if (IsDead || ReachedGoal) return;

        UpdateEffects(dt);

        if (Blocker != null)
        {
            if (Blocker.IsDead || !Blocker.IsDeployed)
            {
                Blocker.Blocking.Remove(this);
                Blocker = null;
                State = EnemyState.Moving;
                AttackTimer = 0.3f;
            }
            else
            {
                State = EnemyState.Blocked;
                AttackTimer -= dt;
                if (AttackTimer <= 0f)
                {
                    AttackTimer = Template.AttackInterval;
                    sim.DamageOperator(Blocker, Template.Attack, DamageType.Physical, Name);
                }
                return;
            }
        }

        if (!Template.Flying)
        {
            var op = sim.FindBlockerAt(Cell, this);
            if (op != null)
            {
                var dir = new Vector2(op.Cell.X, op.Cell.Y) - Position;
                if (dir.LengthSquared() < 0.85f * 0.85f)
                {
                    Blocker = op;
                    op.Blocking.Add(this);
                    State = EnemyState.Blocked;
                    AttackTimer = 0.4f;
                    return;
                }
            }
        }

        MoveAlongPath(dt, sim);
    }

    private void UpdateEffects(float dt)
    {
        for (int i = Effects.Count - 1; i >= 0; i--)
        {
            Effects[i].Remaining -= dt;
            if (Effects[i].Expired) Effects.RemoveAt(i);
        }
    }

    private void MoveAlongPath(float dt, BattleSimulation sim)
    {
        if (Waypoint >= Path.Count)
        {
            ReachedGoal = true;
            State = EnemyState.Done;
            return;
        }

        var target = new Vector2(Path[Waypoint].X, Path[Waypoint].Y);
        var delta = target - Position;
        float dist = delta.Length();
        float step = Template.Speed * SlowFactor * dt;

        if (dist <= step || dist < 0.001f)
        {
            Position = target;
            Waypoint++;
            if (Waypoint >= Path.Count)
            {
                ReachedGoal = true;
                State = EnemyState.Done;
            }
            return;
        }

        Position += delta / dist * step;
        Progress++;
    }
}
