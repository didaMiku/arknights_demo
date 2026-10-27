using Godot;

namespace ArknightsDemo;

public enum Profession { Vanguard, Guard, Defender, Sniper, Caster, Medic }

public enum DamageType { Physical, Magical, True }

public enum Facing { Up, Down, Left, Right }

public enum CellType { Empty, Ground, Highland, Wall, Spawn, Goal }

public enum AttackKind { Melee, Ranged, Heal }

public enum StatusType { Stun, Slow, AttackUp, AttackSpeedUp, DefenseUp }

public static class FacingExtensions
{
    public static Vector2I Dir(this Facing f) => f switch
    {
        Facing.Up => new Vector2I(0, -1),
        Facing.Down => new Vector2I(0, 1),
        Facing.Left => new Vector2I(-1, 0),
        _ => new Vector2I(1, 0),
    };

    /// <summary>Unit vector pointing to the grid centre of the next cell in this direction.</summary>
    public static Vector2 Vector(this Facing f) => f switch
    {
        Facing.Up => new Vector2(0, -1),
        Facing.Down => new Vector2(0, 1),
        Facing.Left => new Vector2(-1, 0),
        _ => new Vector2(1, 0),
    };

    public static Facing FromDelta(Vector2 d)
    {
        if (Mathf.Abs(d.X) >= Mathf.Abs(d.Y))
            return d.X >= 0 ? Facing.Right : Facing.Left;
        return d.Y >= 0 ? Facing.Down : Facing.Up;
    }

    public static float Angle(this Facing f) => f switch
    {
        Facing.Up => -Mathf.Pi / 2f,
        Facing.Down => Mathf.Pi / 2f,
        Facing.Left => Mathf.Pi,
        _ => 0f,
    };
}

public static class GameConst
{
    public const int CellSize = 56;
    public const int MapCols = 11;
    public const int MapRows = 8;
    public const float StartCost = 10f;
    public const float CostPerSecond = 1f;
    public const float CostCap = 99f;
    public const int StartLife = 3;
    public const float RetreatRefund = 0.5f;
}
