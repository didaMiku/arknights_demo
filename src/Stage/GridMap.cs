using Godot;

namespace ArknightsDemo;

/// <summary>Static level geometry: cells, spawns, goals and the lane paths enemies walk.</summary>
public class GridMap
{
    public int Cols { get; private set; }
    public int Rows { get; private set; }
    public CellType[,] Cells { get; private set; }

    public List<Vector2I> Spawns { get; } = new();
    public List<Vector2I> Goals { get; } = new();
    public List<List<Vector2I>> Paths { get; } = new();

    public void Initialize(int cols, int rows)
    {
        Cols = cols;
        Rows = rows;
        Cells = new CellType[cols, rows];
        for (int c = 0; c < cols; c++)
            for (int r = 0; r < rows; r++)
                Cells[c, r] = CellType.Empty;
    }

    public bool InBounds(int c, int r) => c >= 0 && r >= 0 && c < Cols && r < Rows;
    public bool InBounds(Vector2I p) => InBounds(p.X, p.Y);

    public CellType At(int c, int r) => InBounds(c, r) ? Cells[c, r] : CellType.Wall;
    public CellType At(Vector2I p) => At(p.X, p.Y);

    public void Set(int c, int r, CellType t)
    {
        if (InBounds(c, r)) Cells[c, r] = t;
    }

    public static bool IsHighGroundProfession(Profession p) =>
        p is Profession.Sniper or Profession.Caster or Profession.Medic;

    /// <summary>Ground professions stand on walkable ground, high-ground ones on plateaus.</summary>
    public bool IsDeployable(Vector2I p, Profession prof)
    {
        var cell = At(p);
        if (IsHighGroundProfession(prof)) return cell == CellType.Highland;
        return cell is CellType.Ground or CellType.Spawn or CellType.Goal;
    }

    public bool IsWalkable(Vector2I p) => At(p) is CellType.Ground or CellType.Spawn or CellType.Goal;
}
