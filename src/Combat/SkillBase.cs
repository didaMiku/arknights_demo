using Godot;

namespace ArknightsDemo;

/// <summary>Template method: charging and the ready check live here, the actual effect is the subclass hook.</summary>
public abstract class SkillBase
{
    public string Id = "";
    public string Name = "";
    public string Description = "";
    public float ChargeTime = 20f;
    public float Charge;

    public bool Ready => Charge >= ChargeTime;
    public float Progress => ChargeTime <= 0f ? 1f : Mathf.Clamp(Charge / ChargeTime, 0f, 1f);

    public virtual void Tick(float dt)
    {
        if (Charge < ChargeTime) Charge = Mathf.Min(ChargeTime, Charge + dt);
    }

    public void Activate(OperatorBase owner, BattleSimulation sim)
    {
        if (!Ready || owner.IsDead) return;
        OnActivate(owner, sim);
        Charge = 0f;
    }

    protected abstract void OnActivate(OperatorBase owner, BattleSimulation sim);
}
