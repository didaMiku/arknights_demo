using Godot;

namespace ArknightsDemo;

/// <summary>Ground melee: hits the enemy it is body-blocking first, then the nearest one in range.</summary>
public class MeleeOperator : OperatorBase
{
    protected override AttackTarget? AcquireTarget(BattleSimulation sim)
    {
        EnemyBase best = null;
        float bestScore = float.MaxValue;
        var self = new Vector2(Cell.X, Cell.Y);

        foreach (var e in sim.Enemies)
        {
            if (e.IsDead || e.ReachedGoal) continue;
            if (e.Template.Flying) continue;

            // An enemy this operator is body-blocking shares its cell, which is not
            // in the "front 1" offset set - it must always be attackable.
            if (ReferenceEquals(e.Blocker, this))
            {
                best = e;
                break;
            }

            if (!IsInRange(e.Cell)) continue;

            float score = (e.Position - self).LengthSquared();

            if (score < bestScore)
            {
                bestScore = score;
                best = e;
            }
        }

        return best == null ? null : new AttackTarget { Enemy = best };
    }

    protected override void PerformAttack(AttackTarget target, BattleSimulation sim)
    {
        sim.DamageEnemy(target.Enemy, Stats.Attack, DamageType.Physical, Name);
        sim.SpawnSlash(new Vector2(Cell.X, Cell.Y), Facing.Angle(), Palette.ProfessionColor(Profession));
    }
}

/// <summary>High-ground ranged: prioritises the enemy closest to the goal. Snipers also
/// prefer air targets - that is their job, and without it drones slip past while the
/// sniper is busy with ground units.</summary>
public class RangedOperator : OperatorBase
{
    protected override AttackTarget? AcquireTarget(BattleSimulation sim)
    {
        bool antiAir = Profession == Profession.Sniper;

        EnemyBase bestAir = null;
        float bestAirProgress = -1f;
        EnemyBase bestElite = null;
        float bestEliteProgress = -1f;
        EnemyBase bestGround = null;
        float bestGroundProgress = -1f;

        foreach (var e in sim.Enemies)
        {
            if (e.IsDead || e.ReachedGoal) continue;
            if (!IsInRange(e.Cell)) continue;

            if (e.Template.Flying)
            {
                if (!antiAir) continue;
                if (e.Progress > bestAirProgress)
                {
                    bestAirProgress = e.Progress;
                    bestAir = e;
                }
                continue;
            }

            // Elites are slow, so "closest to goal" alone would let casters ignore
            // a boss while they plink at fast trash in front of it.
            if (e.Template.IsElite)
            {
                if (e.Progress > bestEliteProgress)
                {
                    bestEliteProgress = e.Progress;
                    bestElite = e;
                }
                continue;
            }

            if (e.Progress > bestGroundProgress)
            {
                bestGroundProgress = e.Progress;
                bestGround = e;
            }
        }

        var chosen = bestAir ?? bestElite ?? bestGround;
        return chosen == null ? null : new AttackTarget { Enemy = chosen };
    }

    protected override void PerformAttack(AttackTarget target, BattleSimulation sim)
    {
        var from = new Vector2(Cell.X, Cell.Y);
        var kind = Profession == Profession.Caster ? ProjectileKind.Orb : ProjectileKind.Bolt;
        sim.SpawnProjectile(from, target.Enemy.Position, Palette.ProfessionColor(Profession), 0.14f, kind);
        sim.DamageEnemy(target.Enemy, Stats.Attack, Profession == Profession.Caster ? DamageType.Magical : DamageType.Physical, Name);
    }
}

/// <summary>Healer: targets the ally with the lowest HP ratio inside the range pyramid.</summary>
public class MedicOperator : OperatorBase
{
    protected override AttackTarget? AcquireTarget(BattleSimulation sim)
    {
        OperatorBase best = null;
        float bestRatio = 0.999f;

        foreach (var op in sim.Operators)
        {
            if (op.IsDead || !op.IsDeployed) continue;
            if (!IsInRange(op.Cell)) continue;

            float ratio = (float)op.Hp / op.MaxHp;
            if (ratio >= bestRatio) continue;

            bestRatio = ratio;
            best = op;
        }

        return best == null ? null : new AttackTarget { Ally = best };
    }

    protected override void PerformAttack(AttackTarget target, BattleSimulation sim)
    {
        var to = new Vector2(target.Ally.Cell.X, target.Ally.Cell.Y);
        sim.SpawnProjectile(new Vector2(Cell.X, Cell.Y), to, Palette.Heal, 0.18f, ProjectileKind.Heal);
        target.Ally.Heal(Stats.Attack);
        sim.SpawnHealEffect(to);
        sim.SpawnDamageNumber(to, Stats.Attack, Palette.Heal);
    }
}
