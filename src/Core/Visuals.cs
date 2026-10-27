using Godot;

namespace ArknightsDemo;

public enum EffectKind
{
    Hit,
    Heal,
    Deploy,
    Explosion,
    Burst,
    Slash,
    DamageNumber,
    Shockwave,
    Death,
}

public enum ProjectileKind
{
    Bolt,
    Orb,
    Heal,
}

public class VisualEffect
{
    public Vector2 Position;
    public float T;
    public float Duration = 0.4f;
    public Color Color;
    public EffectKind Kind;

    /// <summary>Damage number payload, or 0 for a miss.</summary>
    public int Value;

    /// <summary>Facing angle in radians, used by slash arcs.</summary>
    public float Angle;

    public bool Done => T >= Duration;
    public float Alpha => 1f - Mathf.Clamp(T / Duration, 0f, 1f);
    public float Progress => Duration <= 0f ? 1f : Mathf.Clamp(T / Duration, 0f, 1f);
}

public class Projectile
{
    public Vector2 From;
    public Vector2 To;
    public float T;
    public float Duration = 0.12f;
    public Color Color;
    public ProjectileKind Kind = ProjectileKind.Bolt;

    public bool Done => T >= Duration;
    public float Progress => Duration <= 0f ? 1f : Mathf.Clamp(T / Duration, 0f, 1f);
    public Vector2 Position => From.Lerp(To, Progress);
}
