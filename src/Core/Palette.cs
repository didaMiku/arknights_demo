using Godot;

namespace ArknightsDemo;

/// <summary>Bright "Arknights-like" palette: light surfaces, saturated accents, dark ink text.</summary>
public static class Palette
{
    public static readonly Color Bg = Color.FromHtml("#eef3f8");
    public static readonly Color Panel = Color.FromHtml("#ffffff");
    public static readonly Color PanelLight = Color.FromHtml("#f5f8fb");
    public static readonly Color EmptyCell = Color.FromHtml("#e6ecf2");
    public static readonly Color GridLine = Color.FromHtml("#cbd6e1");
    public static readonly Color Ground = Color.FromHtml("#f9fcfd");
    public static readonly Color Highland = Color.FromHtml("#e7eef6");
    public static readonly Color Wall = Color.FromHtml("#d2dae3");
    public static readonly Color LaneFill = Color.FromHtml("#d9ebf6");
    public static readonly Color LaneArrow = Color.FromHtml("#1a9fb5");
    public static readonly Color HighlandDot = Color.FromHtml("#b4c6d8");
    public static readonly Color WallCross = Color.FromHtml("#c1ccd7");
    public static readonly Color Accent = Color.FromHtml("#128fa5");
    public static readonly Color AccentDim = Color.FromHtml("#7fc3cf");
    public static readonly Color Danger = Color.FromHtml("#d94a4a");
    public static readonly Color Heal = Color.FromHtml("#2a9e63");
    public static readonly Color Gold = Color.FromHtml("#c78a12");
    public static readonly Color Text = Color.FromHtml("#243040");
    public static readonly Color TextDim = Color.FromHtml("#67788b");
    public static readonly Color TextFaint = Color.FromHtml("#9dabb9");
    public static readonly Color DamageNormal = Color.FromHtml("#243040");
    public static readonly Color DamageBig = Color.FromHtml("#c78a12");
    public static readonly Color DamageHeal = Color.FromHtml("#2a9e63");
    public static readonly Color Skin = Color.FromHtml("#f6d8c3");
    public static readonly Color SkinShade = Color.FromHtml("#e3b99e");

    public static Color ProfessionColor(Profession p) => p switch
    {
        Profession.Vanguard => Color.FromHtml("#e8862e"),
        Profession.Guard => Color.FromHtml("#d94a4a"),
        Profession.Defender => Color.FromHtml("#3d82d9"),
        Profession.Sniper => Color.FromHtml("#c79a00"),
        Profession.Caster => Color.FromHtml("#8e4fd9"),
        Profession.Medic => Color.FromHtml("#2a9e63"),
        _ => Colors.White,
    };

    public static string ProfessionLabel(Profession p) => p switch
    {
        Profession.Vanguard => "先锋",
        Profession.Guard => "近卫",
        Profession.Defender => "重装",
        Profession.Sniper => "狙击",
        Profession.Caster => "术师",
        Profession.Medic => "医疗",
        _ => "?",
    };

    public static Color EnemyColor(string shape) => shape switch
    {
        "diamond" => Color.FromHtml("#d9663d"),
        "triangle" => Color.FromHtml("#7f9e14"),
        "hexagon" => Color.FromHtml("#c0394b"),
        "square" => Color.FromHtml("#5d7a94"),
        _ => Color.FromHtml("#c94a5c"),
    };
}
