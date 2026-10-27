using Godot;

namespace ArknightsDemo;

/// <summary>Single source of truth for the flat-geometric visual language.</summary>
public static class Palette
{
    public static readonly Color Bg = Color.FromHtml("#0a0e14");
    public static readonly Color Panel = Color.FromHtml("#121821");
    public static readonly Color PanelLight = Color.FromHtml("#1c2430");
    public static readonly Color EmptyCell = Color.FromHtml("#0e141c");
    public static readonly Color GridLine = Color.FromHtml("#2b3a4c");
    public static readonly Color Ground = Color.FromHtml("#1e2836");
    public static readonly Color Highland = Color.FromHtml("#2c3e56");
    public static readonly Color Wall = Color.FromHtml("#070a0f");
    public static readonly Color PathLine = Color.FromHtml("#2b4356");
    public static readonly Color Accent = Color.FromHtml("#00e5ff");
    public static readonly Color AccentDim = Color.FromHtml("#0a6b78");
    public static readonly Color Danger = Color.FromHtml("#ff4d5e");
    public static readonly Color Heal = Color.FromHtml("#4dff9e");
    public static readonly Color Gold = Color.FromHtml("#ffcf4d");
    public static readonly Color Text = Color.FromHtml("#dfe9f2");
    public static readonly Color TextDim = Color.FromHtml("#7c8a99");
    public static readonly Color TextFaint = Color.FromHtml("#4a5665");

    public static Color ProfessionColor(Profession p) => p switch
    {
        Profession.Vanguard => Color.FromHtml("#ff9f43"),
        Profession.Guard => Color.FromHtml("#ff5e5e"),
        Profession.Defender => Color.FromHtml("#4d9fff"),
        Profession.Sniper => Color.FromHtml("#ffd93d"),
        Profession.Caster => Color.FromHtml("#b06cff"),
        Profession.Medic => Color.FromHtml("#4dff9e"),
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
        "diamond" => Color.FromHtml("#ff7b54"),
        "triangle" => Color.FromHtml("#c0f000"),
        "hexagon" => Color.FromHtml("#ff4d5e"),
        "square" => Color.FromHtml("#7f9bb5"),
        _ => Color.FromHtml("#e05a6d"),
    };
}
