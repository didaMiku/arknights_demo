using Godot;

namespace ArknightsDemo;

/// <summary>Entry scene. Loads config singletons, then hands the screen to BattleView.</summary>
public partial class MainScene : Node2D
{
    public override void _Ready()
    {
        ConfigManager.Instance.LoadAll();
        GameManager.Instance.BuildDefaultSquad();

        var view = new BattleView();
        AddChild(view);
    }
}
