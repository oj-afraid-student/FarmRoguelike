// FullGameFlowTest.cs - 全功能流程测试脚本
// 用于测试当前项目所有已实现系统的功能
// 使用方法：将脚本挂到场景节点上，运行游戏后调用 RunAllTests() 或通过按键触发
using Godot;
using System;
using System.Collections.Generic;

public partial class FullGameFlowTest : Node
{
    private GameManager _gameManager;
    private DataManager _dataManager;
    private FarmingSystem _farmingSystem;
    private CropEffectSystem _cropEffectSystem;
    private EnderChestSystem _enderChestSystem;
    private CombatResourceSystem _resourceSystem;
    private CombatSystem _combatSystem;
    private MapSystem _mapSystem;
    private EventBus _eventBus;

    private int _passedCount;
    private int _failedCount;
    private List<string> _failures = new();

    public override void _Ready()
    {
        AcquireReferences();
        GD.Print("FullGameFlowTest 已加载。按 F6 运行完整测试流程。");
    }

    public override void _Process(double delta)
    {
        if (Input.IsKeyPressed(Key.F6))
        {
            RunAllTests();
        }
    }

    private void AcquireReferences()
    {
        _eventBus = EventBus.Instance ?? GetNodeOrNull<EventBus>("/root/EventBus");
        _gameManager = GameRoot.Instance?.GameManager ?? GetNodeOrNull<GameManager>("/root/GameManager");
        _dataManager = GameRoot.Instance?.DataManager ?? GetNodeOrNull<DataManager>("/root/DataManager");

        if (GameRoot.Instance != null)
        {
            _farmingSystem = GameRoot.Instance.FarmingSystem;
            _cropEffectSystem = GameRoot.Instance.CropEffectSystem;
            _enderChestSystem = GameRoot.Instance.EnderChestSystem;
            _resourceSystem = GameRoot.Instance.CombatResourceSystem;
        }

        var gameRootNode = GetNodeOrNull<Node>("/root/Main/GameRoot")
            ?? GetNodeOrNull<Node>("/root/GameRoot")
            ?? GetTree().CurrentScene?.GetNodeOrNull<Node>("GameRoot");
        if (gameRootNode != null)
        {
            if (_farmingSystem == null) _farmingSystem = gameRootNode.GetNodeOrNull<FarmingSystem>("FarmingSystem");
            if (_cropEffectSystem == null) _cropEffectSystem = gameRootNode.GetNodeOrNull<CropEffectSystem>("CropEffectSystem");
            if (_enderChestSystem == null) _enderChestSystem = gameRootNode.GetNodeOrNull<EnderChestSystem>("EnderChestSystem");
            if (_resourceSystem == null) _resourceSystem = gameRootNode.GetNodeOrNull<CombatResourceSystem>("CombatResourceSystem");
            if (_combatSystem == null) _combatSystem = gameRootNode.GetNodeOrNull<CombatSystem>("CombatSystem");
            if (_mapSystem == null) _mapSystem = gameRootNode.GetNodeOrNull<MapSystem>("MapSystem");
        }
    }

    /// <summary>
    /// 运行所有测试
    /// </summary>
    public void RunAllTests()
    {
        _passedCount = 0;
        _failedCount = 0;
        _failures.Clear();

        GD.Print("\n========== 全功能流程测试开始 ==========\n");

        TestManagerReferences();
        TestDataManager();
        TestMapSystem();
        TestFarmingSystem();
        TestCombatSystem();
        TestCombatResourceSystem();
        TestCropEffectSystem();
        TestEnderChestSystem();
        TestGameStateFlow();
        TestSaveManager();

        GD.Print("\n========== 测试完成 ==========");
        GD.Print($"通过: {_passedCount}, 失败: {_failedCount}");
        if (_failures.Count > 0)
        {
            GD.Print("失败项:");
            foreach (var f in _failures)
                GD.PrintErr($"  - {f}");
        }
    }

    private void Pass(string name) { _passedCount++; GD.Print($"[✓] {name}"); }
    private void Fail(string name, string reason)
    {
        _failedCount++;
        _failures.Add($"{name}: {reason}");
        GD.PrintErr($"[✗] {name}: {reason}");
    }

    private void TestManagerReferences()
    {
        GD.Print("--- 1. 管理器引用 ---");
        if (_eventBus != null) Pass("EventBus"); else Fail("EventBus", "未找到");
        if (_gameManager != null) Pass("GameManager"); else Fail("GameManager", "未找到");
        if (_dataManager != null) Pass("DataManager"); else Fail("DataManager", "未找到");
        if (_farmingSystem != null) Pass("FarmingSystem"); else Fail("FarmingSystem", "未找到");
        if (_cropEffectSystem != null) Pass("CropEffectSystem"); else Fail("CropEffectSystem", "未找到");
        if (_enderChestSystem != null) Pass("EnderChestSystem"); else Fail("EnderChestSystem", "未找到");
        if (_resourceSystem != null) Pass("CombatResourceSystem"); else Fail("CombatResourceSystem", "未找到");
        if (_combatSystem != null) Pass("CombatSystem"); else Fail("CombatSystem", "未找到");
        if (_mapSystem != null) Pass("MapSystem"); else Fail("MapSystem", "未找到");
    }

    private void TestDataManager()
    {
        GD.Print("\n--- 2. DataManager ---");
        if (_dataManager == null) return;

        var crops = _dataManager.GetAllCrops();
        if (crops != null && crops.Count > 0)
            Pass($"作物数据: {crops.Count} 种");
        else
            Fail("作物数据", "无数据");

        var cards = _dataManager.GetAllCards();
        if (cards != null && cards.Count > 0)
            Pass($"卡牌数据: {cards.Count} 张");
        else
            Fail("卡牌数据", "无数据");

        var enemy = _dataManager.GetEnemy("enemy_slime");
        if (enemy != null)
            Pass($"敌人数据: {enemy.Name}");
        else
            Fail("敌人数据", "enemy_slime 未找到");
    }

    private void TestMapSystem()
    {
        GD.Print("\n--- 3. MapSystem ---");
        if (_mapSystem == null) return;

        try
        {
            _mapSystem.GenerateFloor(1);
            var room = _mapSystem.CurrentRoom;
            if (room != null)
                Pass($"地图生成: 当前房间 {room.Type}");
            else
                Fail("地图生成", "CurrentRoom 为空");
        }
        catch (Exception e)
        {
            Fail("MapSystem.GenerateFloor", e.Message);
        }
    }

    private void TestFarmingSystem()
    {
        GD.Print("\n--- 4. FarmingSystem ---");
        if (_farmingSystem == null) return;

        var plots = _farmingSystem.Plots;
        if (plots == null) { Fail("FarmingSystem", "Plots 为空"); return; }
        Pass($"地块数量: {plots.Count}");

        var planted = _farmingSystem.TryPlantCrop(0, "crop_wheat");
        if (planted)
            Pass("种植作物(crop_wheat)");
        else
            Fail("种植作物", "TryPlantCrop 返回 false");

        var plot = plots.ContainsKey(0) ? plots[0] : null;
        if (plot != null && plot.IsOccupied && plot.CropId == "crop_wheat")
            Pass("地块状态正确");
        else
            Fail("地块状态", "种植后状态异常");
    }

    private void TestCombatSystem()
    {
        GD.Print("\n--- 5. CombatSystem ---");
        if (_combatSystem == null) return;

        try
        {
            _combatSystem.StartCombat("enemy_slime");
            var hand = _combatSystem.GetPlayerHand();
            var (cur, max) = _combatSystem.GetEnemyHealth();
            if (hand != null && hand.Count > 0)
                Pass($"战斗启动: 手牌 {hand.Count} 张");
            else
                Fail("战斗启动", "手牌为空");
            Pass($"敌人生命: {cur}/{max}");
        }
        catch (Exception e)
        {
            Fail("CombatSystem.StartCombat", e.Message);
        }
    }

    private void TestCombatResourceSystem()
    {
        GD.Print("\n--- 6. CombatResourceSystem ---");
        if (_resourceSystem == null) return;

        var resources = _resourceSystem.GenerateCombatResources(true, "enemy_slime");
        if (resources != null && resources.Count > 0)
        {
            Pass($"战斗物资生成: {resources.Count} 种");
            foreach (var kv in resources)
            {
                var d = _resourceSystem.GetResourceData(kv.Key);
                if (d != null)
                    GD.Print($"    {d.Name} x{kv.Value}");
            }
        }
        else
        {
            Pass("战斗物资: 未生成（概率正常）");
        }
    }

    private void TestCropEffectSystem()
    {
        GD.Print("\n--- 7. CropEffectSystem ---");
        if (_gameManager == null || _cropEffectSystem == null) return;

        bool ok = _gameManager.ApplyCropEffect("crop_health_boost");
        if (ok)
        {
            Pass("应用作物效果(crop_health_boost)");
            var effects = _gameManager.GetActiveCropEffects();
            if (effects.Count > 0)
                Pass($"激活效果数: {effects.Count}");
        }
        else
            Fail("应用作物效果", "ApplyCropEffect 返回 false");
    }

    private void TestEnderChestSystem()
    {
        GD.Print("\n--- 8. EnderChestSystem ---");
        if (_gameManager == null) return;

        var chest = _gameManager.OpenEnderChest(1);
        if (chest != null && chest.AvailableCrops.Count > 0)
        {
            Pass($"末影箱: {chest.AvailableCrops.Count} 个选项");
        }
        else
            Fail("末影箱", "打开失败或无选项");
    }

    private void TestGameStateFlow()
    {
        GD.Print("\n--- 9. GameState 切换 ---");
        if (_gameManager == null) return;

        var before = _gameManager.CurrentState;
        _gameManager.ChangeState(GameEnums.GameState.Farming);
        if (_gameManager.CurrentState == GameEnums.GameState.Farming)
            Pass("切换到 Farming");
        else
            Fail("切换到 Farming", $"当前状态: {_gameManager.CurrentState}");

        _gameManager.ChangeState(GameEnums.GameState.MapExploration);
        if (_gameManager.CurrentState == GameEnums.GameState.MapExploration)
            Pass("切换回 MapExploration");
        else
            Fail("切换回 MapExploration", $"当前状态: {_gameManager.CurrentState}");
    }

    private void TestSaveManager()
    {
        GD.Print("\n--- 10. SaveManager ---");
        var saveManager = GameRoot.Instance?.SaveManager ?? GetNodeOrNull<SaveManager>("/root/Main/GameRoot/SaveManager");
        if (saveManager == null)
        {
            Fail("SaveManager", "未找到");
            return;
        }
        Pass("SaveManager 引用");
    }
}
