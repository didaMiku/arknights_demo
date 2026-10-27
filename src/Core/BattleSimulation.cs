using Godot;

namespace ArknightsDemo;

public enum BattleState { Preparing, Running, Won, Lost }

/// <summary>
/// The whole battle lives here, engine-free. Godot only reads this object to draw,
/// which keeps the design-pattern structure testable and independent of the renderer.
/// </summary>
public class BattleSimulation
{
	public StageRuntime Stage { get; }
	public GridMap Map => Stage.Map;

	public List<OperatorBase> Operators { get; } = new();
	public List<EnemyBase> Enemies { get; } = new();
	public List<Projectile> Projectiles { get; } = new();
	public List<VisualEffect> Effects { get; } = new();

	/// <summary>Squad chosen before the battle, as clones of the prototype table.</summary>
	public List<OperatorTemplate> Squad { get; } = new();

	public float Cost;
	public int Life;
	public float Time;
	public float Speed = 1f;
	public bool Paused;
	public BattleState State = BattleState.Preparing;

	public int SpawnCursor;
	public int Killed;
	public int Leaked;
	public int Deployed;

	/// <summary>Diagnostics: what leaked and when, used by the --auto regression.</summary>
	public List<string> LeakLog { get; } = new();

	public Action<string> OnToast;

	public BattleSimulation(StageRuntime stage)
	{
		Stage = stage;
		Cost = GameConst.StartCost;
		Life = GameConst.StartLife;
	}

	public void Start()
	{
		if (State != BattleState.Preparing) return;
		State = BattleState.Running;
		Toast($"作战开始 · {Stage.Name}");
	}

	public void Toast(string message) => OnToast?.Invoke(message);

	public void Tick(float dt)
	{
		if (State != BattleState.Running || Paused) return;

		float sdt = dt * Speed;
		Time += sdt;

		Cost = Mathf.Min(GameConst.CostCap, Cost + GameConst.CostPerSecond * sdt);

		while (SpawnCursor < Stage.Spawns.Count && Stage.Spawns[SpawnCursor].Time <= Time)
		{
			SpawnEnemy(Stage.Spawns[SpawnCursor]);
			SpawnCursor++;
		}

		foreach (var op in Operators) op.Tick(sdt, this);

		for (int i = Enemies.Count - 1; i >= 0; i--)
		{
			var enemy = Enemies[i];
			enemy.Tick(sdt, this);

			if (enemy.IsDead)
			{
				OnEnemyKilled(enemy);
				Enemies.RemoveAt(i);
				continue;
			}

			if (enemy.ReachedGoal && !enemy.Counted)
			{
				enemy.Counted = true;
				Leaked++;
				Life--;
				LeakLog.Add($"{enemy.Name}@{Time:0.0}s");
				SpawnExplosion(enemy.Position);
				Toast($"漏怪！剩余生命 {Life}");
				Enemies.RemoveAt(i);
			}
		}

		for (int i = Projectiles.Count - 1; i >= 0; i--)
		{
			Projectiles[i].T += sdt;
			if (Projectiles[i].Done) Projectiles.RemoveAt(i);
		}

		for (int i = Effects.Count - 1; i >= 0; i--)
		{
			Effects[i].T += sdt;
			if (Effects[i].Done) Effects.RemoveAt(i);
		}

		if (Life <= 0)
		{
			State = BattleState.Lost;
			Toast("作战失败");
			return;
		}

		if (SpawnCursor >= Stage.Spawns.Count && Enemies.Count == 0 && Stage.Spawns.Count > 0)
		{
			State = BattleState.Won;
			Toast("作战成功");
		}
	}

	private void SpawnEnemy(SpawnEvent evt)
	{
		if (Map.Paths.Count == 0) return;

		var template = ConfigManager.Instance.Registry.CloneEnemy(evt.EnemyId);
		int pathIndex = Mathf.Clamp(evt.PathIndex, 0, Map.Paths.Count - 1);
		var path = Map.Paths[pathIndex];
		var enemy = UnitFactory.CreateEnemy(template, path, pathIndex);
		Enemies.Add(enemy);
	}

	private void OnEnemyKilled(EnemyBase enemy)
	{
		Killed++;
		Cost = Mathf.Min(GameConst.CostCap, Cost + (enemy.Template.IsElite ? 3f : 1f));
		SpawnExplosion(enemy.Position);

		if (enemy.Template.SelfDestructDamage > 0)
		{
			var center = enemy.Position;
			foreach (var op in Operators.ToArray())
			{
				if (op.IsDead || !op.IsDeployed) continue;
				if ((new Vector2(op.Cell.X, op.Cell.Y) - center).Length() <= 2.2f)
					DamageOperator(op, enemy.Template.SelfDestructDamage, DamageType.True, enemy.Name);
			}
		}
	}

	public bool TryDeploy(OperatorTemplate template, Vector2I cell, Facing facing)
	{
		if (State != BattleState.Running) return false;

		if (!Map.IsDeployable(cell, template.Profession))
		{
			Toast("这个格子不能部署该职业");
			return false;
		}

		if (GetOperatorAt(cell) != null)
		{
			Toast("格子已被占用");
			return false;
		}

		if (Cost < template.Cost)
		{
			Toast("费用不足");
			return false;
		}

		var op = UnitFactory.CreateOperator(template.Clone());
		op.Cell = cell;
		op.Facing = facing;
		Operators.Add(op);
		Cost -= template.Cost;
		Deployed++;
		SpawnDeploy(new Vector2(cell.X, cell.Y));
		Toast($"{op.Name} 部署");
		return true;
	}

	public void Retreat(OperatorBase op)
	{
		if (op == null || !op.IsDeployed || op.IsDead) return;

		Cost = Mathf.Min(GameConst.CostCap, Cost + op.Cost * GameConst.RetreatRefund);
		foreach (var enemy in op.Blocking) enemy.Blocker = null;
		op.Blocking.Clear();
		op.Cell = new Vector2I(-1, -1);
		Operators.Remove(op);
		Toast($"{op.Name} 撤退");
	}

	public bool UseSkill(OperatorBase op)
	{
		if (op == null || op.IsDead || op.Skill == null) return false;
		if (!op.Skill.Ready) { Toast("技能未就绪"); return false; }
		op.Skill.Activate(op, this);
		return true;
	}

	public OperatorBase GetOperatorAt(Vector2I cell)
	{
		foreach (var op in Operators)
			if (op.IsDeployed && !op.IsDead && op.Cell == cell) return op;
		return null;
	}

	public OperatorBase FindBlockerAt(Vector2I cell, EnemyBase enemy)
	{
		var op = GetOperatorAt(cell);
		if (op == null) return null;

		// High-ground units stand on plateaus and never body-block ground enemies.
		// Without this guard a ranged unit could "block" a ground enemy it cannot
		// reach, and a zero-attack enemy would deadlock forever.
		if (GridMap.IsHighGroundProfession(op.Profession)) return null;

		int block = op.Stats.Block;
		if (block <= 0) return null;

		int used = 0;
		foreach (var b in op.Blocking)
			if (!b.IsDead) used += b.Template.BlockCost;

		return used + enemy.Template.BlockCost <= block ? op : null;
	}

	public void DamageEnemy(EnemyBase enemy, int amount, DamageType type, string source)
	{
		if (enemy == null || enemy.IsDead) return;
		DamagePipeline.Resolve(new DamageRequest
		{
			RawAmount = amount,
			Type = type,
			Target = enemy,
			Source = source,
		});
	}

	public void DamageOperator(OperatorBase op, int amount, DamageType type, string source)
	{
		if (op == null || op.IsDead) return;
		DamagePipeline.Resolve(new DamageRequest
		{
			RawAmount = amount,
			Type = type,
			Target = op,
			Source = source,
		});
	}

	public void SpawnProjectile(Vector2 from, Vector2 to, Color color, float duration = 0.12f) =>
		Projectiles.Add(new Projectile { From = from, To = to, Color = color, Duration = duration });

	public void SpawnHitEffect(Vector2 pos, Color color) =>
		Effects.Add(new VisualEffect { Position = pos, Color = color, Kind = EffectKind.Hit, Duration = 0.3f });

	public void SpawnHealEffect(Vector2 pos) =>
		Effects.Add(new VisualEffect { Position = pos, Color = Palette.Heal, Kind = EffectKind.Heal, Duration = 0.5f });

	public void SpawnDeploy(Vector2 pos) =>
		Effects.Add(new VisualEffect { Position = pos, Color = Palette.Accent, Kind = EffectKind.Deploy, Duration = 0.5f });

	public void SpawnExplosion(Vector2 pos) =>
		Effects.Add(new VisualEffect { Position = pos, Color = Palette.Danger, Kind = EffectKind.Explosion, Duration = 0.45f });

	public void SpawnBurst(Vector2 pos, Color color) =>
		Effects.Add(new VisualEffect { Position = pos, Color = color, Kind = EffectKind.Burst, Duration = 0.6f });
}
