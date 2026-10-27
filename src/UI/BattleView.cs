using Godot;

namespace ArknightsDemo;

/// <summary>
/// The only Godot-facing class. It owns no game rules: every frame it asks the
/// engine-free <see cref="BattleSimulation"/> for state and paints it.
/// </summary>
public partial class BattleView : Node2D
{
    private const int Cell = 56;
    private const int Cols = 11;
    private const int Rows = 8;

    private static readonly Vector2 MapOrigin = new(332, 70);
    private static readonly Vector2 CardOrigin = new(0, 600);
    private const float CardW = 88f;
    private const float CardH = 94f;
    private const float CardGap = 6f;

    private static readonly Rect2 SelectionPanel = new(984, 400, 270, 176);

    private BattleSimulation _sim;
    private Font _font;

    private bool _dragging;
    private OperatorTemplate _dragTemplate;
    private Vector2I _hoverCell = new(-1, -1);
    private Facing _hoverFacing = Facing.Right;
    private OperatorBase _selected;
    private readonly HashSet<string> _used = new();

    private string _toastText = "";
    private float _toastTimer;
    private string _banner = "";
    private float _bannerTimer;

    private float _time;

    private bool _shotMode;
    private bool _autoMode;
    private float _shotClock;
    private bool _shotDeployed;
    private bool _shotSaved;

    public override void _Ready()
    {
        var sf = new SystemFont();
        sf.FontNames = new[] { "Microsoft YaHei", "SimHei", "Noto Sans CJK SC", "sans-serif" };
        _font = sf;

        _shotMode = OS.GetCmdlineUserArgs().Contains("--shot");
        _autoMode = OS.GetCmdlineUserArgs().Contains("--auto");

        int startStage = GameManager.Instance.StageIndex;
        var args = OS.GetCmdlineUserArgs();
        int stageFlag = Array.IndexOf(args, "--stage");
        if (stageFlag >= 0 && stageFlag + 1 < args.Length && int.TryParse(args[stageFlag + 1], out int parsed))
            startStage = parsed;

        LoadStage(startStage);

        if (_autoMode) RunAutoTest();
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        _time += dt;
        _sim?.Tick(dt);
        _toastTimer = Mathf.Max(0f, _toastTimer - dt);
        _bannerTimer = Mathf.Max(0f, _bannerTimer - dt);

        if (_shotMode) UpdateAutoShot(dt);

        QueueRedraw();
    }

    private void LoadStage(int index)
    {
        var stages = ConfigManager.Instance.Stages;
        if (stages.Count == 0)
        {
            GD.PushError("no stages loaded");
            return;
        }

        index = Mathf.Clamp(index, 0, stages.Count - 1);
        GameManager.Instance.StageIndex = index;

        var stage = new StageBuilder()
            .Load(stages[index])
            .BuildMap()
            .BuildPaths()
            .BuildWaves()
            .Build();

        _sim = new BattleSimulation(stage);
        _sim.OnToast = message =>
        {
            _toastText = message;
            _toastTimer = 2.8f;
        };

        _sim.Squad.Clear();
        foreach (var id in GameManager.Instance.Squad)
        {
            if (ConfigManager.Instance.Registry.HasOperator(id))
                _sim.Squad.Add(ConfigManager.Instance.Registry.CloneOperator(id));
        }

        _used.Clear();
        _selected = null;
        _dragging = false;
        _dragTemplate = null;

        _banner = stage.Name;
        _bannerTimer = 2.4f;

        _sim.Start();
    }

    // ---------------------------------------------------------------- input

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_sim == null) return;

        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            HandleKey(key.Keycode);
            return;
        }

        if (@event is InputEventMouseButton mouse && mouse.ButtonIndex == MouseButton.Left)
        {
            if (mouse.Pressed) OnPress(mouse.Position);
            else OnRelease(mouse.Position);
            return;
        }

        if (@event is InputEventMouseMotion motion && _dragging)
            UpdateHover(motion.Position);
    }

    private void HandleKey(Key key)
    {
        switch (key)
        {
            case Key.Space: _sim.Paused = !_sim.Paused; break;
            case Key.Tab: _sim.Speed = _sim.Speed > 1f ? 1f : 2f; break;
            case Key.R: LoadStage(GameManager.Instance.StageIndex); break;
            case Key.Key1: LoadStage(0); break;
            case Key.Key2: LoadStage(1); break;
            case Key.Key3: LoadStage(2); break;
            case Key.Key4: LoadStage(3); break;
            case Key.Key5: LoadStage(4); break;
            case Key.Escape:
                _selected = null;
                _dragging = false;
                _dragTemplate = null;
                break;
            case Key.Q:
                if (_selected != null) _sim.UseSkill(_selected);
                break;
        }
    }

    private void OnPress(Vector2 pos)
    {
        if (_sim.State is BattleState.Won or BattleState.Lost) return;

        int card = CardAt(pos);
        if (card >= 0)
        {
            var template = _sim.Squad[card];
            if (_used.Contains(template.Id)) { _sim.Toast("该干员已部署"); return; }
            if (_sim.Cost < template.Cost) { _sim.Toast("费用不足"); return; }

            _dragging = true;
            _dragTemplate = template;
            _selected = null;
            UpdateHover(pos);
            return;
        }

        if (_selected != null && _selected.IsDeployed && !_selected.IsDead)
        {
            if (SkillButtonRect().HasPoint(pos)) { _sim.UseSkill(_selected); return; }
            if (RetreatButtonRect().HasPoint(pos))
            {
                _used.Remove(_selected.Id);
                _sim.Retreat(_selected);
                _selected = null;
                return;
            }
        }

        var cell = PixelToCell(pos);
        if (_sim.Map.InBounds(cell))
        {
            _selected = _sim.GetOperatorAt(cell);
            return;
        }

        _selected = null;
    }

    private void OnRelease(Vector2 pos)
    {
        if (!_dragging || _dragTemplate == null) return;

        UpdateHover(pos);
        if (ValidCell(_hoverCell) && _sim.TryDeploy(_dragTemplate, _hoverCell, _hoverFacing))
            _used.Add(_dragTemplate.Id);

        _dragging = false;
        _dragTemplate = null;
    }

    private void UpdateHover(Vector2 pos)
    {
        _hoverCell = PixelToCell(pos);
        if (!_sim.Map.InBounds(_hoverCell)) return;

        var delta = pos - CellCenter(_hoverCell);
        _hoverFacing = delta.LengthSquared() < 64f ? Facing.Right : FacingExtensions.FromDelta(delta);
    }

    private bool ValidCell(Vector2I cell)
    {
        if (_dragTemplate == null) return false;
        if (!_sim.Map.InBounds(cell)) return false;
        if (!_sim.Map.IsDeployable(cell, _dragTemplate.Profession)) return false;
        return _sim.GetOperatorAt(cell) == null;
    }

    // ------------------------------------------------------------- drawing

    public override void _Draw()
    {
        if (_sim == null) return;

        // Background stays still; the battlefield shakes under it.
        DrawBackground();

        var shake = Vector2.Zero;
        if (_sim.ScreenShake > 0.001f)
            shake = new Vector2(GD.Randf() - 0.5f, GD.Randf() - 0.5f) * _sim.ScreenShake * 14f;

        DrawSetTransform(shake, 0f, Vector2.One);
        DrawMap();
        DrawDeployPreview();
        DrawEnemies();
        DrawOperators();
        DrawProjectiles();
        DrawEffects();

        DrawSetTransform(Vector2.Zero, 0f, Vector2.One);
        DrawTopBar();
        DrawCards();
        DrawSelection();
        DrawToast();
        DrawBanner();
        DrawResult();
    }

    private void DrawBackground()
    {
        DrawRect(new Rect2(0, 0, 1280, 720), Palette.Bg);

        // Sparse dot grid, keeps the empty area from reading as a flat void.
        for (int x = 0; x < 1280; x += 32)
            for (int y = 0; y < 720; y += 32)
                DrawRect(new Rect2(x, y, 1, 1), new Color(0.10f, 0.13f, 0.17f, 0.8f));
    }

    private void DrawMap()
    {
        var mapArea = new Rect2(MapOrigin, new Vector2(Cols * Cell, Rows * Cell));
        DrawRect(mapArea, Palette.Bg);

        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                var cell = new Vector2I(c, r);
                var type = _sim.Map.At(cell);
                var rect = CellRect(cell);

                switch (type)
                {
                    case CellType.Ground:
                    case CellType.Spawn:
                    case CellType.Goal:
                        DrawRect(rect, Palette.Ground);
                        DrawRect(rect.Grow(-1f), Palette.GridLine, false, 1f);
                        DrawCornerTicks(rect, new Color(Palette.AccentDim, 0.7f));
                        break;

                    case CellType.Highland:
                        DrawRect(rect, Palette.Highland);
                        DrawPlateauDots(rect);
                        DrawRect(rect, new Color(Palette.AccentDim, 0.9f), false, 1f);
                        break;

                    case CellType.Wall:
                        DrawRect(rect, Palette.Wall);
                        DrawWallCross(rect);
                        break;

                    default:
                        DrawRect(rect, Palette.EmptyCell);
                        DrawRect(rect.Grow(-1f), new Color(Palette.GridLine, 0.18f), false, 1f);
                        break;
                }
            }
        }

        foreach (var path in _sim.Map.Paths) DrawLane(path);
        foreach (var spawn in _sim.Map.Spawns) DrawSpawnMarker(spawn);
        foreach (var goal in _sim.Map.Goals) DrawGoalMarker(goal);

        DrawRect(mapArea.Grow(2f), Palette.AccentDim, false, 1f);
        DrawRect(mapArea.Grow(7f), new Color(Palette.Accent, 0.05f), false, 3f);
    }

    private void DrawCornerTicks(Rect2 rect, Color color)
    {
        const float len = 6f;
        var p = rect.Position;
        var s = rect.Size;

        DrawLine(p + new Vector2(3, 3), p + new Vector2(3 + len, 3), color, 1f);
        DrawLine(p + new Vector2(3, 3), p + new Vector2(3, 3 + len), color, 1f);
        DrawLine(p + new Vector2(s.X - 3, 3), p + new Vector2(s.X - 3 - len, 3), color, 1f);
        DrawLine(p + new Vector2(s.X - 3, 3), p + new Vector2(s.X - 3, 3 + len), color, 1f);
    }

    private void DrawPlateauDots(Rect2 rect)
    {
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if ((x + y) % 2 != 0) continue;
                var dot = rect.Position + new Vector2(10 + x * 12, 10 + y * 12);
                DrawRect(new Rect2(dot, new Vector2(3, 3)), Palette.HighlandDot);
            }
        }
    }

    private void DrawWallCross(Rect2 rect)
    {
        var p = rect.Position;
        var s = rect.Size;
        DrawLine(p + new Vector2(8, 8), p + new Vector2(s.X - 8, s.Y - 8), Palette.WallCross, 1f);
        DrawLine(p + new Vector2(s.X - 8, 8), p + new Vector2(8, s.Y - 8), Palette.WallCross, 1f);
    }

    private void DrawLane(List<Vector2I> path)
    {
        foreach (var cell in path)
        {
            if (!_sim.Map.InBounds(cell)) continue;
            var r = CellRect(cell).Grow(-3f);
            DrawRect(r, Palette.LaneFill);
            DrawRect(r, new Color(Palette.Accent, 0.18f), false, 1f);
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            if (i % 2 != 0) continue;

            var a = CellCenter(path[i]);
            var b = CellCenter(path[i + 1]);
            var dir = (b - a).Normalized();
            if (dir.LengthSquared() < 0.01f) continue;

            float phase = Mathf.PosMod(_time * 0.8f + i * 0.33f, 1f);
            var p = a.Lerp(b, phase);
            float alpha = 0.10f + 0.30f * Mathf.Sin(phase * Mathf.Pi);
            DrawArrow(p, dir, new Color(Palette.LaneArrow, alpha), 6f);
        }
    }

    private void DrawArrow(Vector2 pos, Vector2 dir, Color color, float size)
    {
        var perp = new Vector2(-dir.Y, dir.X);
        var tip = pos + dir * size * 0.5f;
        var left = pos - dir * size * 0.35f + perp * size * 0.4f;
        var right = pos - dir * size * 0.35f - perp * size * 0.4f;
        DrawLine(left, tip, color, 2f);
        DrawLine(right, tip, color, 2f);
    }

    private void DrawSpawnMarker(Vector2I spawn)
    {
        var center = CellCenter(spawn);
        float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 4f);
        DrawCircle(center, 11f + pulse * 5f, new Color(Palette.Danger, 0.10f));
        DrawCircle(center, 7f, new Color(Palette.Danger, 0.85f));
        DrawArc(center, 14f + pulse * 4f, 0, Mathf.Tau, 24, new Color(Palette.Danger, 0.45f), 1.5f);
    }

    private void DrawGoalMarker(Vector2I goal)
    {
        var center = CellCenter(goal);
        float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 3f);
        DrawRect(new Rect2(center - new Vector2(12, 30), new Vector2(24, 30)),
            new Color(Palette.Accent, 0.08f + pulse * 0.05f));
        DrawCircle(center, 7f + pulse * 2f, new Color(Palette.Accent, 0.85f));
        DrawArc(center, 13f, 0, Mathf.Tau, 24, new Color(Palette.Accent, 0.45f), 1.5f);
    }

    private void DrawDeployPreview()
    {
        if (!_dragging || _dragTemplate == null || !ValidCell(_hoverCell)) return;

        var color = Palette.ProfessionColor(_dragTemplate.Profession);
        foreach (var offset in RangeShapes.Get(_dragTemplate.RangeShape, _hoverFacing))
        {
            var cell = _hoverCell + offset;
            if (!_sim.Map.InBounds(cell)) continue;
            DrawRect(CellRect(cell).Grow(-2f), new Color(color, 0.22f));
            DrawRect(CellRect(cell).Grow(-2f), new Color(color, 0.5f), false, 1f);
        }

        DrawRect(CellRect(_hoverCell), new Color(color, 0.45f));
        DrawRect(CellRect(_hoverCell), color, false, 2f);
    }

    private void DrawEnemies()
    {
        foreach (var enemy in _sim.Enemies)
        {
            // A blocked enemy shares its cell with the operator; nudge it sideways so
            // the two silhouettes stay readable.
            var visual = enemy.Position;
            if (enemy.Blocker != null) visual += new Vector2(-0.24f, 0.24f);

            var center = GridToPixel(visual);
            var color = Palette.EnemyColor(enemy.Template.Shape);
            float radius = 12f * enemy.Template.Scale;

            if (enemy.SlowFactor < 0.99f)
                DrawCircle(center, radius + 5f, new Color(0.45f, 0.8f, 1f, 0.30f));

            if (enemy.Template.IsElite)
                DrawCircle(center, radius + 7f, new Color(Palette.Gold, 0.10f));

            // Dark rim keeps enemy silhouettes separate from warm-coloured operators.
            DrawCircle(center, radius + 2.5f, new Color(0.02f, 0.03f, 0.05f, 0.85f));
            DrawShape(center, radius, enemy.Template.Shape, color);

            if (enemy.HitFlash > 0f)
            {
                float flash = Mathf.Clamp(enemy.HitFlash / 0.14f, 0f, 1f);
                DrawShape(center, radius * 1.05f, enemy.Template.Shape, new Color(1f, 1f, 1f, flash * 0.75f));
            }

            if (enemy.Template.IsElite)
                DrawArc(center, radius + 3f, 0, Mathf.Tau, 20, Palette.Gold, 1.5f);

            DrawBar(center + new Vector2(-radius, radius + 5f), radius * 2f, 3f,
                (float)enemy.Hp / enemy.MaxHp, Palette.Danger);
        }
    }

    private void DrawOperators()
    {
        foreach (var op in _sim.Operators)
        {
            if (!op.IsDeployed) continue;

            var center = CellCenter(op.Cell);
            var color = Palette.ProfessionColor(op.Profession);

            if (op.IsDead)
            {
                DrawRect(CellRect(op.Cell).Grow(-12f), new Color(0.18f, 0.19f, 0.22f, 0.45f));
                DrawLine(center + new Vector2(-6, -6), center + new Vector2(6, 6), Palette.TextFaint, 2f);
                DrawLine(center + new Vector2(6, -6), center + new Vector2(-6, 6), Palette.TextFaint, 2f);
                continue;
            }

            float scale = Mathf.Lerp(1.35f, 1f, op.DeployAnim);

            DrawFacingWedge(center, op.Facing, color);
            DrawCircle(center, 19f * scale, new Color(color, 0.10f));
            DrawCircle(center, 15f * scale, color.Darkened(0.55f));
            DrawCircle(center, 12.5f * scale, color);
            DrawArc(center, 13.5f * scale, 0, Mathf.Tau, 24, new Color(1f, 1f, 1f, 0.35f), 1f);
            DrawProfessionGlyph(center, op.Profession, color.Darkened(0.2f), scale);

            if (ReferenceEquals(op, _selected))
                DrawArc(center, 22f, 0, Mathf.Tau, 32, Palette.Accent, 2f);

            if (op.Skill != null && op.Skill.Ready)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 5f);
                DrawArc(center, 22f + pulse * 3f, 0, Mathf.Tau, 28,
                    new Color(Palette.Accent, 0.65f - pulse * 0.35f), 1.5f);
                DrawCircle(center + new Vector2(17, -17), 3.5f, Palette.Accent);
            }

            if (op.HitFlash > 0f)
            {
                float flash = Mathf.Clamp(op.HitFlash / 0.14f, 0f, 1f);
                DrawCircle(center, 13.5f, new Color(1f, 1f, 1f, flash * 0.7f));
            }

            var hpColor = (float)op.Hp / op.MaxHp > 0.5f ? Palette.Heal : Palette.Gold;
            DrawBar(center + new Vector2(-16, 21f), 32f, 4f, (float)op.Hp / op.MaxHp, hpColor);
        }
    }

    private void DrawFacingWedge(Vector2 center, Facing facing, Color color)
    {
        var dir = facing.Vector();
        float angle = facing.Angle();

        var points = new[]
        {
            center + new Vector2(Mathf.Cos(angle - 0.45f), Mathf.Sin(angle - 0.45f)) * 34f,
            center + new Vector2(Mathf.Cos(angle + 0.45f), Mathf.Sin(angle + 0.45f)) * 34f,
            center + dir * 18f,
        };
        DrawColoredPolygon(points, new Color(color, 0.10f));
        DrawLine(center + dir * 20f, center + dir * 33f, new Color(color, 0.55f), 2f);
    }

    private void DrawProfessionGlyph(Vector2 center, Profession profession, Color color, float scale)
    {
        float s = 7f * scale;

        switch (profession)
        {
            case Profession.Vanguard:
                DrawColoredPolygon(new[]
                {
                    center + new Vector2(0, -s),
                    center + new Vector2(s, s * 0.8f),
                    center + new Vector2(-s, s * 0.8f),
                }, color);
                break;

            case Profession.Guard:
                DrawColoredPolygon(new[]
                {
                    center + new Vector2(0, -s),
                    center + new Vector2(s, 0),
                    center + new Vector2(0, s),
                    center + new Vector2(-s, 0),
                }, color);
                break;

            case Profession.Defender:
                DrawRect(new Rect2(center - new Vector2(s * 0.8f, s * 0.8f), new Vector2(s * 1.6f, s * 1.6f)), color);
                break;

            case Profession.Sniper:
                DrawArc(center, s, 0, Mathf.Tau, 16, color, 1.5f);
                DrawLine(center + new Vector2(-s * 1.3f, 0), center + new Vector2(s * 1.3f, 0), color, 1.5f);
                DrawLine(center + new Vector2(0, -s * 1.3f), center + new Vector2(0, s * 1.3f), color, 1.5f);
                break;

            case Profession.Caster:
                var hex = new Vector2[6];
                for (int i = 0; i < 6; i++)
                {
                    float a = Mathf.Pi / 3f * i - Mathf.Pi / 2f;
                    hex[i] = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s;
                }
                DrawColoredPolygon(hex, color);
                break;

            default: // Medic
                DrawRect(new Rect2(center - new Vector2(s * 0.32f, s), new Vector2(s * 0.64f, s * 2f)), color);
                DrawRect(new Rect2(center - new Vector2(s, s * 0.32f), new Vector2(s * 2f, s * 0.64f)), color);
                break;
        }
    }

    private void DrawProjectiles()
    {
        foreach (var projectile in _sim.Projectiles)
        {
            var pos = GridToPixel(projectile.Position);

            switch (projectile.Kind)
            {
                case ProjectileKind.Bolt:
                    var dir = (projectile.To - projectile.From).Normalized();
                    var tail = GridToPixel(projectile.Position - dir * 0.35f);
                    DrawLine(tail, pos, new Color(projectile.Color, 0.45f), 2f);
                    DrawCircle(pos, 3f, projectile.Color);
                    break;

                case ProjectileKind.Orb:
                    DrawCircle(pos, 6f, new Color(projectile.Color, 0.22f));
                    DrawCircle(pos, 3.5f, projectile.Color);
                    break;

                default:
                    DrawCircle(pos, 3f, projectile.Color);
                    DrawLine(pos + new Vector2(-3, 0), pos + new Vector2(3, 0), Colors.White, 1f);
                    DrawLine(pos + new Vector2(0, -3), pos + new Vector2(0, 3), Colors.White, 1f);
                    break;
            }
        }
    }

    private void DrawEffects()
    {
        foreach (var fx in _sim.Effects)
        {
            var pos = GridToPixel(fx.Position);
            float t = fx.Progress;
            float radius = Mathf.Lerp(4f, 30f, t);

            switch (fx.Kind)
            {
                case EffectKind.Hit:
                    DrawCircle(pos, radius * 0.4f, new Color(fx.Color, fx.Alpha * 0.8f));
                    for (int i = 0; i < 4; i++)
                    {
                        float a = Mathf.Pi * 0.5f * i + t * 2f;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        DrawLine(pos + dir * radius * 0.3f, pos + dir * radius, new Color(fx.Color, fx.Alpha), 1.5f);
                    }
                    break;

                case EffectKind.Slash:
                    float spread = Mathf.Lerp(0.25f, 0.9f, t);
                    DrawArc(pos, 26f, fx.Angle - spread, fx.Angle + spread, 14, new Color(fx.Color, fx.Alpha), 3f);
                    break;

                case EffectKind.DamageNumber:
                    var textPos = pos + new Vector2(0, -18f - t * 26f);
                    string text = fx.Value > 0 ? fx.Value.ToString() : "MISS";
                    int size = fx.Value >= 400 ? 19 : 14;
                    DrawString(_font, textPos - new Vector2(40, 0), text, HorizontalAlignment.Center, 80, size,
                        new Color(fx.Color, fx.Alpha));
                    break;

                case EffectKind.Shockwave:
                    DrawArc(pos, radius * 1.6f, 0, Mathf.Tau, 28, new Color(fx.Color, fx.Alpha), 2f);
                    break;

                case EffectKind.Death:
                    DrawCircle(pos, radius, new Color(fx.Color, fx.Alpha * 0.5f));
                    for (int i = 0; i < 6; i++)
                    {
                        float a = Mathf.Tau / 6f * i;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        DrawLine(pos + dir * radius * 0.4f, pos + dir * radius, new Color(fx.Color, fx.Alpha), 2f);
                    }
                    break;

                case EffectKind.Heal:
                    DrawArc(pos, radius, 0, Mathf.Tau, 24, new Color(Palette.Heal, fx.Alpha), 2f);
                    break;

                case EffectKind.Deploy:
                    DrawArc(pos, radius, 0, Mathf.Tau, 24, new Color(Palette.Accent, fx.Alpha), 2f);
                    break;

                case EffectKind.Explosion:
                    DrawCircle(pos, radius, new Color(Palette.Danger, fx.Alpha * 0.6f));
                    break;

                case EffectKind.Burst:
                    DrawArc(pos, radius, 0, Mathf.Tau, 24, new Color(fx.Color, fx.Alpha), 2f);
                    break;
            }
        }
    }

    private void DrawTopBar()
    {
        DrawRect(new Rect2(0, 0, 1280, 56), Palette.Panel);
        DrawRect(new Rect2(0, 56, 1280, 1), Palette.GridLine);

        DrawString(_font, new Vector2(56, 34), "COST", HorizontalAlignment.Left, -1, 12, Palette.TextDim);
        DrawString(_font, new Vector2(108, 38), $"{(int)_sim.Cost}", HorizontalAlignment.Left, -1, 24, Palette.Accent);

        DrawString(_font, new Vector2(200, 34), "生命", HorizontalAlignment.Left, -1, 12, Palette.TextDim);
        DrawString(_font, new Vector2(248, 38), $"{_sim.Life} / {GameConst.StartLife}", HorizontalAlignment.Left, -1, 20,
            _sim.Life <= 1 ? Palette.Danger : Palette.Text);

        int wave = 0;
        foreach (var t in _sim.Stage.WaveStartTimes)
            if (_sim.Time >= t) wave++;
        wave = Mathf.Max(1, wave);

        DrawString(_font, new Vector2(410, 34), "波次", HorizontalAlignment.Left, -1, 12, Palette.TextDim);
        DrawString(_font, new Vector2(456, 38), $"{wave} / {_sim.Stage.WaveCount}", HorizontalAlignment.Left, -1, 18, Palette.Text);

        DrawString(_font, new Vector2(570, 34), "时间", HorizontalAlignment.Left, -1, 12, Palette.TextDim);
        DrawString(_font, new Vector2(614, 38), FormatTime(_sim.Time), HorizontalAlignment.Left, -1, 18, Palette.Text);

        string speed = _sim.Paused ? "已暂停" : $"{_sim.Speed:0}x";
        DrawString(_font, new Vector2(760, 38), speed, HorizontalAlignment.Left, -1, 16,
            _sim.Paused ? Palette.Gold : Palette.TextDim);

        DrawString(_font, new Vector2(900, 34), "关卡", HorizontalAlignment.Left, -1, 12, Palette.TextDim);
        DrawString(_font, new Vector2(946, 38), _sim.Stage.Name, HorizontalAlignment.Left, -1, 18, Palette.Accent);
    }

    private void DrawCards()
    {
        var squad = _sim.Squad;
        if (squad.Count == 0) return;

        float total = squad.Count * CardW + (squad.Count - 1) * CardGap;
        float startX = (1280f - total) / 2f;

        for (int i = 0; i < squad.Count; i++)
        {
            var template = squad[i];
            var rect = new Rect2(startX + i * (CardW + CardGap), CardOrigin.Y, CardW, CardH);
            bool used = _used.Contains(template.Id);
            bool affordable = _sim.Cost >= template.Cost;
            var color = Palette.ProfessionColor(template.Profession);

            DrawRect(rect, used ? new Color(0.07f, 0.08f, 0.1f) : Palette.PanelLight);
            DrawRect(rect, used ? Palette.TextFaint : color, false, 1.5f);
            DrawRect(new Rect2(rect.Position, new Vector2(CardW, 4)), used ? Palette.TextFaint : color);

            DrawRect(new Rect2(rect.Position + new Vector2(10, 14), new Vector2(22, 22)), new Color(color, used ? 0.2f : 0.85f));

            if (used)
            {
                DrawString(_font, rect.Position + new Vector2(0, 56), "已部署", HorizontalAlignment.Center, CardW, 13, Palette.TextFaint);
                DrawString(_font, rect.Position + new Vector2(0, 84), $"{template.Cost}", HorizontalAlignment.Center, CardW, 12, Palette.TextFaint);
            }
            else
            {
                DrawString(_font, rect.Position + new Vector2(10, 50), template.Name, HorizontalAlignment.Left, CardW - 14, 15, Palette.Text);
                DrawString(_font, rect.Position + new Vector2(10, 68), Palette.ProfessionLabel(template.Profession),
                    HorizontalAlignment.Left, CardW - 14, 11, Palette.TextDim);
                DrawString(_font, rect.Position + new Vector2(10, 86), $"{template.Cost}", HorizontalAlignment.Left,
                    CardW - 14, 14, affordable ? Palette.Gold : Palette.Danger);
            }
        }
    }

    private void DrawSelection()
    {
        if (_selected == null || _selected.IsDead || !_selected.IsDeployed) return;

        DrawRect(SelectionPanel, Palette.Panel);
        DrawRect(SelectionPanel, Palette.AccentDim, false, 1.5f);

        var origin = SelectionPanel.Position;
        DrawString(_font, origin + new Vector2(14, 26), _selected.Name, HorizontalAlignment.Left, -1, 18, Palette.Text);
        DrawString(_font, origin + new Vector2(14, 46), $"HP {_selected.Hp} / {_selected.MaxHp}", HorizontalAlignment.Left, -1, 12, Palette.TextDim);

        if (_selected.Skill != null)
        {
            var skill = _selected.Skill;
            DrawString(_font, origin + new Vector2(14, 72), skill.Name, HorizontalAlignment.Left, -1, 14, Palette.Accent);

            var bar = new Rect2(origin + new Vector2(14, 82), new Vector2(242, 8));
            DrawRect(bar, new Color(0, 0, 0, 0.5f));
            DrawRect(new Rect2(bar.Position, new Vector2(242 * skill.Progress, 8)),
                skill.Ready ? Palette.Accent : Palette.AccentDim);

            DrawString(_font, origin + new Vector2(14, 108),
                skill.Ready ? "技能就绪" : $"充能 {(int)(skill.Progress * 100)}%",
                HorizontalAlignment.Left, -1, 11, skill.Ready ? Palette.Accent : Palette.TextDim);

            if (skill.Ready)
                DrawButton(SkillButtonRect(), "释放技能 [Q]", Palette.Accent);
        }

        DrawButton(RetreatButtonRect(), "撤退", Palette.Danger);
    }

    private void DrawToast()
    {
        if (_toastTimer <= 0f || string.IsNullOrEmpty(_toastText)) return;

        float alpha = Mathf.Min(1f, _toastTimer * 2f);
        DrawRect(new Rect2(420, 552, 440, 28), new Color(0.04f, 0.05f, 0.07f, 0.85f * alpha));
        DrawString(_font, new Vector2(420, 571), _toastText, HorizontalAlignment.Center, 440, 13, new Color(Palette.Text, alpha));
    }

    private void DrawBanner()
    {
        if (_bannerTimer <= 0f) return;
        float alpha = Mathf.Clamp(_bannerTimer / 1.2f, 0f, 1f);
        DrawString(_font, new Vector2(0, 300), _banner, HorizontalAlignment.Center, 1280, 34, new Color(Palette.Accent, alpha));
    }

    private void DrawResult()
    {
        if (_sim.State is not (BattleState.Won or BattleState.Lost)) return;

        DrawRect(new Rect2(0, 0, 1280, 720), new Color(0, 0, 0, 0.55f));

        bool won = _sim.State == BattleState.Won;
        DrawString(_font, new Vector2(0, 300), won ? "作战成功" : "作战失败",
            HorizontalAlignment.Center, 1280, 46, won ? Palette.Heal : Palette.Danger);
        DrawString(_font, new Vector2(0, 344), $"击杀 {_sim.Killed}   漏怪 {_sim.Leaked}   部署 {_sim.Deployed}",
            HorizontalAlignment.Center, 1280, 15, Palette.Text);
        DrawString(_font, new Vector2(0, 392), "按 R 重新开始   按 1-5 切换关卡",
            HorizontalAlignment.Center, 1280, 14, Palette.TextDim);
    }

    // ------------------------------------------------------------- helpers

    private static Vector2 CellCenter(Vector2I cell) =>
        MapOrigin + new Vector2(cell.X * Cell + Cell / 2f, cell.Y * Cell + Cell / 2f);

    private static Vector2 GridToPixel(Vector2 grid) =>
        MapOrigin + new Vector2(grid.X * Cell + Cell / 2f, grid.Y * Cell + Cell / 2f);

    private static Rect2 CellRect(Vector2I cell) =>
        new(MapOrigin + new Vector2(cell.X * Cell, cell.Y * Cell), new Vector2(Cell, Cell));

    private static Vector2I PixelToCell(Vector2 pixel) =>
        new(Mathf.FloorToInt((pixel.X - MapOrigin.X) / Cell), Mathf.FloorToInt((pixel.Y - MapOrigin.Y) / Cell));

    private static Rect2 SkillButtonRect() => new(SelectionPanel.Position + new Vector2(14, 116), new Vector2(120, 30));

    private static Rect2 RetreatButtonRect() => new(SelectionPanel.Position + new Vector2(146, 116), new Vector2(110, 30));

    private void DrawBar(Vector2 pos, float width, float height, float ratio, Color color)
    {
        ratio = Mathf.Clamp(ratio, 0f, 1f);
        DrawRect(new Rect2(pos, new Vector2(width, height)), new Color(0f, 0f, 0f, 0.7f));
        DrawRect(new Rect2(pos, new Vector2(width * ratio, height)), color);
    }

    private int CardAt(Vector2 pos)
    {
        int count = _sim.Squad.Count;
        if (count == 0) return -1;

        float total = count * CardW + (count - 1) * CardGap;
        float startX = (1280f - total) / 2f;

        if (pos.Y < CardOrigin.Y || pos.Y > CardOrigin.Y + CardH) return -1;

        for (int i = 0; i < count; i++)
        {
            float x = startX + i * (CardW + CardGap);
            if (pos.X >= x && pos.X <= x + CardW) return i;
        }

        return -1;
    }

    private void DrawButton(Rect2 rect, string label, Color color)
    {
        DrawRect(rect, new Color(color, 0.14f));
        DrawRect(rect, color, false, 1.5f);
        DrawString(_font, rect.Position + new Vector2(0, 20), label, HorizontalAlignment.Center, rect.Size.X, 12, color);
    }

    private void DrawShape(Vector2 center, float radius, string shape, Color color)
    {
        switch (shape)
        {
            case "square":
                DrawRect(new Rect2(center - new Vector2(radius, radius), new Vector2(radius * 2, radius * 2)), color);
                break;

            case "diamond":
                DrawColoredPolygon(new[]
                {
                    center + new Vector2(0, -radius),
                    center + new Vector2(radius, 0),
                    center + new Vector2(0, radius),
                    center + new Vector2(-radius, 0),
                }, color);
                break;

            case "triangle":
                DrawColoredPolygon(new[]
                {
                    center + new Vector2(0, -radius),
                    center + new Vector2(radius * 0.95f, radius * 0.7f),
                    center + new Vector2(-radius * 0.95f, radius * 0.7f),
                }, color);
                break;

            case "hexagon":
                var points = new Vector2[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle = Mathf.Pi / 3f * i - Mathf.Pi / 2f;
                    points[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
                DrawColoredPolygon(points, color);
                break;

            default:
                DrawCircle(center, radius, color);
                break;
        }
    }

    private static string FormatTime(float seconds)
    {
        int total = Mathf.FloorToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }

    // -------------------------------------------------------- screenshot aid

    private void UpdateAutoShot(float dt)
    {
        _shotClock += dt;

        if (_shotClock > 1.2f && !_shotDeployed)
        {
            _shotDeployed = true;
            _sim.Time = 8f;
            DeployGenericSquad(6);
        }

        if (_shotClock > 4.5f && !_shotSaved)
        {
            _shotSaved = true;
            SaveShot();
        }
    }

    private void AutoDeploy(string operatorId, Vector2I cell, Facing facing)
    {
        foreach (var template in _sim.Squad)
        {
            if (template.Id != operatorId) continue;
            if (_sim.TryDeploy(template, cell, facing)) _used.Add(template.Id);
            return;
        }
    }

    /// <summary>Headless regression: build a generic line-up from the map, fast-forward, print the outcome.</summary>
    private void RunAutoTest()
    {
        DeployGenericSquad();

        int steps = 0;
        bool dumpedSlow = false;
        while (_sim.State == BattleState.Running && steps < 8000)
        {
            _sim.Tick(0.05f);
            steps++;

            if (!dumpedSlow && _sim.Time > 120f)
            {
                dumpedSlow = true;
                GD.Print($"[auto] slow t={_sim.Time:0.0}s enemies={_sim.Enemies.Count} " +
                         $"spawn={_sim.SpawnCursor}/{_sim.Stage.Spawns.Count}");
                foreach (var e in _sim.Enemies)
                    GD.Print($"  {e.Name} hp={e.Hp}/{e.MaxHp} pos=({e.Position.X:0.00},{e.Position.Y:0.00}) " +
                             $"blocker={e.Blocker?.Name ?? "-"} state={e.State} wp={e.Waypoint}/{e.Path.Count}");
            }

            // Fire every ready skill once per simulated second, exercising the skill path too.
            if (steps % 20 == 0)
            {
                foreach (var op in _sim.Operators)
                    if (op.Skill != null && op.Skill.Ready) _sim.UseSkill(op);
            }
        }

        GD.Print($"[auto] stage={_sim.Stage.Id} state={_sim.State} killed={_sim.Killed} leaked={_sim.Leaked} " +
                 $"deployed={_sim.Deployed} gameTime={_sim.Time:0.0}s steps={steps}");
        if (_sim.LeakLog.Count > 0)
            GD.Print($"[auto] leaks=[{string.Join(", ", _sim.LeakLog)}]");

        if (_sim.State == BattleState.Running)
        {
            GD.Print($"[auto] timeout enemies={_sim.Enemies.Count} spawnCursor={_sim.SpawnCursor}/{_sim.Stage.Spawns.Count}");
            foreach (var e in _sim.Enemies)
                GD.Print($"  {e.Name} hp={e.Hp}/{e.MaxHp} pos=({e.Position.X:0.00},{e.Position.Y:0.00}) " +
                         $"blocker={e.Blocker?.Name ?? "-"} state={e.State} wp={e.Waypoint}/{e.Path.Count}");
        }

        GetTree().Quit();
    }

    /// <summary>Greedy deployment used by both the screenshot mode and the regression run.</summary>
    private void DeployGenericSquad(int maxOperators = 999)
    {
        _sim.Cost = 9999f;
        int count = 0;

        // Walk every lane cell and rely on HasOperatorNear for spacing, so defenders
        // spread along the whole route instead of clustering near the spawn.
        var laneCells = new List<Vector2I>();
        foreach (var path in _sim.Map.Paths) laneCells.AddRange(path);

        foreach (var template in _sim.Squad)
        {
            if (count >= maxOperators) break;
            if (template.Profession is Profession.Sniper or Profession.Caster or Profession.Medic) continue;

            foreach (var cell in laneCells)
            {
                if (_sim.GetOperatorAt(cell) != null) continue;
                if (HasOperatorNear(cell)) continue;
                if (!_sim.Map.IsDeployable(cell, template.Profession)) continue;
                if (_sim.TryDeploy(template, cell, Facing.Right))
                {
                    _used.Add(template.Id);
                    count++;
                    break;
                }
            }
        }

        var highlandCells = new List<Vector2I>();
        for (int r = 0; r < _sim.Map.Rows; r++)
        {
            for (int c = 0; c < _sim.Map.Cols; c++)
            {
                var cell = new Vector2I(c, r);
                if (_sim.Map.At(cell) == CellType.Highland) highlandCells.Add(cell);
            }
        }

        // Prefer plateau tiles that actually overlook a lane, so ranged units cover
        // both paths instead of stacking in the first corner.
        highlandCells.Sort((a, b) => DistToLane(a).CompareTo(DistToLane(b)));

        foreach (var template in _sim.Squad)
        {
            if (count >= maxOperators) break;
            if (template.Profession is not (Profession.Sniper or Profession.Caster or Profession.Medic)) continue;

            foreach (var cell in highlandCells)
            {
                if (_sim.GetOperatorAt(cell) != null) continue;
                if (!_sim.Map.IsDeployable(cell, template.Profession)) continue;
                if (_sim.TryDeploy(template, cell, FacingTowardLane(cell)))
                {
                    _used.Add(template.Id);
                    count++;
                    break;
                }
            }
        }
    }

    private int DistToLane(Vector2I cell)
    {
        int best = int.MaxValue;
        foreach (var path in _sim.Map.Paths)
            foreach (var p in path)
                best = Mathf.Min(best, Mathf.Abs(p.X - cell.X) + Mathf.Abs(p.Y - cell.Y));
        return best;
    }

    private bool HasOperatorNear(Vector2I cell)
    {
        return _sim.GetOperatorAt(cell + new Vector2I(1, 0)) != null
            || _sim.GetOperatorAt(cell + new Vector2I(-1, 0)) != null
            || _sim.GetOperatorAt(cell + new Vector2I(0, 1)) != null
            || _sim.GetOperatorAt(cell + new Vector2I(0, -1)) != null;
    }

    private Facing FacingTowardLane(Vector2I cell)
    {
        Facing best = Facing.Right;
        float bestDist = float.MaxValue;

        foreach (var path in _sim.Map.Paths)
        {
            foreach (var p in path)
            {
                var delta = p - cell;
                if (delta == Vector2I.Zero) continue;
                float dist = delta.LengthSquared();
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = FacingExtensions.FromDelta(new Vector2(delta.X, delta.Y));
            }
        }

        return best;
    }

    private async void SaveShot()
    {
        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        var image = GetViewport().GetTexture().GetImage();
        image.SavePng(ProjectSettings.GlobalizePath("res://shot.png"));
        GD.Print("[shot] saved res://shot.png");
        GetTree().Quit();
    }
}
