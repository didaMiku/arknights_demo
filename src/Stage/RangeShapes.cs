using Godot;

namespace ArknightsDemo;

/// <summary>
/// Attack range shapes. Offsets are authored pointing right, then rotated to the
/// operator's facing, which is what makes direction matter like in Arknights.
/// </summary>
public static class RangeShapes
{
    public static List<Vector2I> Get(string shape, Facing facing)
    {
        var raw = shape switch
        {
            "front1" => new List<Vector2I> { new(1, 0) },
            "front2" => new() { new(1, 0), new(2, 0) },
            "line4" => new() { new(1, 0), new(2, 0), new(3, 0), new(4, 0) },
            "front3x3" => Front3x3(),
            "around8" => Around8(),
            "cross" => new() { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) },
            "self" => new(),
            _ => new() { new(1, 0) },
        };

        int rotations = facing switch
        {
            Facing.Right => 0,
            Facing.Down => 1,
            Facing.Left => 2,
            _ => 3,
        };

        var result = new List<Vector2I>(raw.Count);
        foreach (var v in raw) result.Add(Rotate(v, rotations));
        return result;
    }

    private static Vector2I Rotate(Vector2I v, int times)
    {
        for (int i = 0; i < times; i++) v = new Vector2I(-v.Y, v.X);
        return v;
    }

    private static List<Vector2I> Front3x3()
    {
        var list = new List<Vector2I>();
        for (int x = 1; x <= 3; x++)
            for (int y = -1; y <= 1; y++)
                list.Add(new Vector2I(x, y));
        return list;
    }

    private static List<Vector2I> Around8()
    {
        var list = new List<Vector2I>();
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (x != 0 || y != 0) list.Add(new Vector2I(x, y));
        return list;
    }
}
