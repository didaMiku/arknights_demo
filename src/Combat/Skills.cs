using Godot;

namespace ArknightsDemo;

public class AttackUpSkill : SkillBase
{
    protected override void OnActivate(OperatorBase owner, BattleSimulation sim)
    {
        owner.AddBuff(StatBuffs.AttackUp(0.6f, 15f));
        sim.SpawnBurst(new Vector2(owner.Cell.X, owner.Cell.Y), Palette.Gold);
        sim.Toast($"{owner.Name} 攻击力提升");
    }
}

public class AttackSpeedSkill : SkillBase
{
    protected override void OnActivate(OperatorBase owner, BattleSimulation sim)
    {
        owner.AddBuff(StatBuffs.AttackSpeedUp(0.8f, 12f));
        sim.SpawnBurst(new Vector2(owner.Cell.X, owner.Cell.Y), Palette.Accent);
        sim.Toast($"{owner.Name} 攻速提升");
    }
}

public class DefenseUpSkill : SkillBase
{
    protected override void OnActivate(OperatorBase owner, BattleSimulation sim)
    {
        owner.AddBuff(StatBuffs.DefenseUp(1.0f, 20f));
        sim.SpawnBurst(new Vector2(owner.Cell.X, owner.Cell.Y), Palette.ProfessionColor(Profession.Defender));
        sim.Toast($"{owner.Name} 防御力大幅提升");
    }
}

public class HealAllSkill : SkillBase
{
    protected override void OnActivate(OperatorBase owner, BattleSimulation sim)
    {
        int amount = Mathf.RoundToInt(owner.Stats.Attack * 1.5f);
        int healed = 0;
        foreach (var op in sim.Operators)
        {
            if (op.IsDead || !op.IsDeployed) continue;
            if (!owner.IsInRange(op.Cell)) continue;
            op.Heal(amount);
            sim.SpawnHealEffect(new Vector2(op.Cell.X, op.Cell.Y));
            healed++;
        }
        sim.Toast(healed > 0 ? $"{owner.Name} 群体治疗 {healed} 人" : $"{owner.Name} 治疗范围内无人");
    }
}

public class SlowFieldSkill : SkillBase
{
    protected override void OnActivate(OperatorBase owner, BattleSimulation sim)
    {
        int hit = 0;
        var snapshot = sim.Enemies.ToArray();
        foreach (var e in snapshot)
        {
            if (e.IsDead || e.ReachedGoal) continue;
            if (!owner.IsInRange(e.Cell)) continue;
            e.Effects.Add(new StatusEffect { Type = StatusType.Slow, Remaining = 6f, Power = 0.5f });
            sim.SpawnHitEffect(e.Position, Palette.ProfessionColor(Profession.Caster));
            hit++;
        }
        sim.Toast(hit > 0 ? $"{owner.Name} 减速 {hit} 名敌人" : $"{owner.Name} 范围内无敌人");
    }
}

public class BurstSkill : SkillBase
{
    protected override void OnActivate(OperatorBase owner, BattleSimulation sim)
    {
        int damage = Mathf.RoundToInt(owner.Stats.Attack * 2.2f);
        var snapshot = sim.Enemies.ToArray();
        int hit = 0;
        foreach (var e in snapshot)
        {
            if (e.IsDead || e.ReachedGoal) continue;
            if (!owner.IsInRange(e.Cell)) continue;
            sim.DamageEnemy(e, damage, DamageType.Magical, owner.Name);
            sim.SpawnHitEffect(e.Position, Palette.ProfessionColor(Profession.Caster));
            hit++;
        }
        sim.SpawnBurst(new Vector2(owner.Cell.X, owner.Cell.Y), Palette.ProfessionColor(Profession.Caster));
        sim.Toast(hit > 0 ? $"{owner.Name} 爆发 {hit} 个目标" : $"{owner.Name} 范围内无敌人");
    }
}

/// <summary>Factory method for skills: ids in the JSON table map to concrete classes here.</summary>
public static class SkillFactory
{
    public static SkillBase Create(string id, float chargeTime)
    {
        SkillBase skill = id switch
        {
            "attack_up" => new AttackUpSkill(),
            "attack_speed" => new AttackSpeedSkill(),
            "defense_up" => new DefenseUpSkill(),
            "heal_all" => new HealAllSkill(),
            "slow_field" => new SlowFieldSkill(),
            "burst" => new BurstSkill(),
            _ => new AttackUpSkill(),
        };
        skill.Id = id;
        skill.ChargeTime = Mathf.Max(1f, chargeTime);
        return skill;
    }
}
