using Godot;

namespace ArknightsDemo;

/// <summary>Factory pattern: turns a cloned prototype into a live runtime object.</summary>
public static class UnitFactory
{
    public static OperatorBase CreateOperator(OperatorTemplate template)
    {
        OperatorBase op = template.Profession switch
        {
            Profession.Medic => new MedicOperator(),
            Profession.Sniper or Profession.Caster => new RangedOperator(),
            _ => new MeleeOperator(),
        };

        op.Initialize(template);
        op.Skill = SkillFactory.Create(template.SkillId, template.SkillChargeTime);
        op.Skill.Name = template.SkillName;
        op.Skill.Description = template.SkillDesc;
        return op;
    }

    public static EnemyBase CreateEnemy(EnemyTemplate template, List<Vector2I> path, int pathIndex)
    {
        var enemy = new EnemyBase();
        enemy.Initialize(template, path, pathIndex);
        return enemy;
    }
}
