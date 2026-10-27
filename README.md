# Arknights Lite

一个模仿《明日方舟》核心玩法的简易塔防 Demo，用 **Godot 4.6 + C#** 实现。
重点不是美术复刻，而是**设计模式在真实游戏结构中的落地**：逻辑层完全不依赖 Godot，
渲染、输入、场景只是它的一层薄壳。

## 运行

### 编辑器
用 Godot 4.6 (mono) 打开 `project.godot`，按 F5。

### 命令行
```powershell
$env:DOTNET_ROOT='D:\dev\env\dotnet'
$env:PATH="D:\dev\env\dotnet;$env:PATH"

# 编译
dotnet build arknights_demo.csproj -p:GodotSharp='D:\dev\ide\godot\Godot_v4.6-stable_mono_win64\GodotSharp'

# 运行
& 'D:\dev\ide\godot\Godot_v4.6-stable_mono_win64\Godot_v4.6-stable_mono_win64.exe' --path .
```

### 调试参数（写在 `--` 之后）
| 参数 | 作用 |
|---|---|
| `--shot` | 自动布阵，4.5 秒后截图到 `shot.png` 并退出 |
| `--shot --stage 4` | 截取指定关卡（0–4） |
| `--auto --stage N` | headless 跑完整关，打印胜负 / 击杀 / 漏怪 / 用时 |

示例：
```powershell
& $godot --headless --path . -- --auto --stage 4
# [auto] stage=stage_05 state=Won killed=22 leaked=2 deployed=12 gameTime=203.8s
```

## 操作

| 输入 | 行为 |
|---|---|
| 按住底部干员卡拖到地图 | 部署；拖动方向决定朝向 |
| 点击已部署干员 | 选中，显示技能 / 撤退面板 |
| 点击「释放技能」或按 Q | 释放选中干员的技能 |
| 点击「撤退」 | 撤退并返还 50% 费用 |
| 空格 | 暂停 / 继续 |
| Tab | 1x / 2x 倍速 |
| R | 重开本关 |
| 1–5 | 切换关卡 |
| Esc | 取消选择 / 取消拖拽 |

## 设计模式落点

| 模式 | 落点 | 关键类 |
|---|---|---|
| **单例** | 全局配置与会话状态 | `ConfigManager`、`GameManager` |
| **工厂方法** | 由模板创建运行时单位 / 技能 | `UnitFactory`、`SkillFactory` |
| **模板方法** | 干员行为骨架（计时 → 硬直 → 冷却 → 索敌 → 攻击） | `OperatorBase.Tick` + `MeleeOperator`/`RangedOperator`/`MedicOperator` |
| **装饰器** | 属性 buff 链，可任意叠加 | `IStatSource`、`StatDecorator`、`AttackUpDecorator` 等 |
| **责任链** | 伤害结算管线 | `DamagePipeline`：闪避 → 防御/法抗 → 增伤 → 扣血 |
| **组合** | 波次树 | `WaveNode` / `WaveGroup` / `WaveLeaf` |
| **原型** | 单位模板克隆，避免重复解析 JSON | `OperatorTemplate.Clone()`、`PrototypeRegistry` |
| **建造者** | 分步构建关卡（地图 → 路径 → 波次 → 编译 spawn） | `StageBuilder` |

### 为什么逻辑层不继承 Node
`src/` 下所有游戏类都是纯 C#。`BattleView` 每帧只做两件事：调用 `BattleSimulation.Tick()`，
然后按状态绘制。这样模式结构不会被引擎的组件模型污染，也方便写 `--auto` 这种无渲染回归。

## 目录

```
arknights_demo/
├── project.godot
├── arknights_demo.csproj
├── scenes/Main.tscn          # 只有一个根节点，其余全部代码构建
├── src/
│   ├── Core/                 # GameEnums、Palette、BattleSimulation、ConfigManager、GameManager、Visuals
│   ├── Data/                 # 模板、原型注册表、JSON DTO
│   ├── Entities/             # OperatorBase + 三种子类、EnemyBase、状态
│   ├── Combat/               # 伤害链、技能、属性装饰器（Buffs/）
│   ├── Stage/                # GridMap、波次组合树、StageBuilder、StageRuntime、攻击范围
│   ├── Factory/              # UnitFactory
│   └── UI/                   # MainScene（入口）、BattleView（全部绘制与输入）
└── assets/data/
    ├── operators.json        # 12 名干员
    ├── enemies.json          # 8 种敌人
    └── stages.json           # 5 个关卡
```

## 数据速查

**职业**：先锋 / 近卫 / 重装（地面，阻挡 2–3），狙击 / 术师 / 医疗（高台）。
**伤害**：物理 `max(攻-防, 攻×5%)`，法术 `max(攻×(1-法抗/100), 攻×5%)`，真实 `攻`。
**资源**：初始 10 费，自然回 1/s，击杀普通 +1 / 精英 +3，上限 99，撤退返还 50%。
**胜负**：漏 3 个失败，清完波次胜利。

**关卡**
1. 训练场 — 单路径，练习部署与阻挡
2. 分岔口 — 双路径，练习分兵
3. 高空 — 高台稀少 + 无人机（只有狙击能对空）
4. 回廊 — 自爆虫（死亡 AOE 真伤）+ 术师敌
5. 源石巨像 — 双路径 + 6 波 + Boss

## 已知限制

- 技能只有时间充能，没有攻击回复 / 受击回复。
- 没有地形机关（毒雾、可破坏障碍、传送门）。
- 没有编队界面，默认按职业各取 2 人。
- 美术全部是 `_Draw()` 程序化绘制，没有外部素材。

## 回归基线

`--auto` 的当前结果（5 关全部 0 漏）：

| 关卡 | 结果 | 击杀 | 漏怪 | 用时 |
|---|---|---|---|---|
| 训练场 | Won | 20 | 0 | 37.8s |
| 分岔口 | Won | 23 | 0 | 88.5s |
| 高空 | Won | 17 | 0 | 39.6s |
| 回廊 | Won | 24 | 0 | 50.0s |
| 源石巨像 | Won | 23 | 0 | 94.2s |

## 设计模式

答辩材料见 [docs/design-patterns.md](docs/design-patterns.md)：8 个模式的类图、代码位置、以及"不用会怎样"的对比。

## 扩展建议

新增干员只需改 `operators.json`；新增技能在 `SkillFactory` 注册一个 id；
新增关卡在 `stages.json` 加一条。三者都不需要碰核心战斗代码——这正是这套模式组合想要的效果。
