using Godot;

namespace ArknightsDemo;

/// <summary>Payload travelling through the damage chain.</summary>
public class DamageRequest
{
    public int RawAmount;
    public DamageType Type = DamageType.Physical;
    public IDamageable Target;
    public string Source = "";
    public float Amplify = 1f;

    public bool Cancelled;
    public bool Dodged;
    public int FinalAmount;
}

/// <summary>
/// Chain of responsibility: each handler may transform or veto the request, then
/// passes it on. Adding a rule means adding a node, not editing an if-else tower.
/// </summary>
public abstract class DamageHandler
{
    protected DamageHandler Next;

    public DamageHandler SetNext(DamageHandler next)
    {
        Next = next;
        return next;
    }

    public void Handle(DamageRequest req)
    {
        Process(req);
        if (!req.Cancelled) Next?.Handle(req);
    }

    protected abstract void Process(DamageRequest req);
}

/// <summary>Rule 1: evasion.</summary>
public class DodgeHandler : DamageHandler
{
    protected override void Process(DamageRequest req)
    {
        if (req.Target == null) { req.Cancelled = true; return; }
        if (req.Target.DodgeChance <= 0f) return;
        if (GD.Randf() < req.Target.DodgeChance)
        {
            req.Cancelled = true;
            req.Dodged = true;
        }
    }
}

/// <summary>Rule 2: the physical / magical / true mitigation formulas.</summary>
public class DefenseHandler : DamageHandler
{
    protected override void Process(DamageRequest req)
    {
        var t = req.Target;
        switch (req.Type)
        {
            case DamageType.Physical:
                req.FinalAmount = Mathf.Max(req.RawAmount - t.Defense, Mathf.CeilToInt(req.RawAmount * 0.05f));
                break;

            case DamageType.Magical:
                int res = Mathf.Clamp(t.Resist, 0, 100);
                req.FinalAmount = Mathf.Max(
                    Mathf.RoundToInt(req.RawAmount * (1f - res / 100f)),
                    Mathf.CeilToInt(req.RawAmount * 0.05f));
                break;

            default:
                req.FinalAmount = req.RawAmount;
                break;
        }
    }
}

/// <summary>Rule 3: vulnerability / amplification.</summary>
public class AmplifyHandler : DamageHandler
{
    protected override void Process(DamageRequest req)
    {
        if (Mathf.IsEqualApprox(req.Amplify, 1f)) return;
        req.FinalAmount = Mathf.Max(1, Mathf.RoundToInt(req.FinalAmount * req.Amplify));
    }
}

/// <summary>Rule 4: commit the number to the target's HP.</summary>
public class ApplyHandler : DamageHandler
{
    protected override void Process(DamageRequest req)
    {
        if (req.FinalAmount <= 0) return;
        req.Target.ApplyDamage(req.FinalAmount);
    }
}

/// <summary>Assembles the chain once and exposes a single entry point.</summary>
public static class DamagePipeline
{
    private static readonly DamageHandler Root;

    static DamagePipeline()
    {
        var dodge = new DodgeHandler();
        var defense = new DefenseHandler();
        var amplify = new AmplifyHandler();
        var apply = new ApplyHandler();
        dodge.SetNext(defense).SetNext(amplify).SetNext(apply);
        Root = dodge;
    }

    public static void Resolve(DamageRequest req) => Root.Handle(req);
}
