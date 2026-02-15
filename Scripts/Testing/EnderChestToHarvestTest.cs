// EnderChestToHarvestTest.cs
// 测试流程：末影箱选作物 → 物资作用于作物 → 生长 → 收获
using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class EnderChestToHarvestTest : Node
{
    private GameManager _gameManager;
    private FarmingSystem _farmingSystem;
    private EnderChestSystem _enderChestSystem;
    private CombatResourceSystem _resourceSystem;

    // 记录从末影箱选的作物种在了哪块地
    private int? _enderChestPlotIndex = null;
    private string _enderChestCropId = null;

    public override void _Ready()
    {
        _gameManager = GameRoot.Instance?.GameManager;
        _farmingSystem = GameRoot.Instance?.FarmingSystem;
        _enderChestSystem = GameRoot.Instance?.EnderChestSystem;
        _resourceSystem = GameRoot.Instance?.CombatResourceSystem;

        if (_gameManager == null || _farmingSystem == null || _enderChestSystem == null || _resourceSystem == null)
        {
            GD.PrintErr("EnderChestToHarvestTest: 缺少管理器引用");
            return;
        }

        GD.Print("========== 末影箱→物资→收获 流程测试 ==========");
        GD.Print("  F1 = 步骤1：打开末影箱并选择第一个作物（自动种植）");
        GD.Print("  F2 = 步骤2：添加测试物资");
        GD.Print("  F3 = 步骤3：对末影箱作物地块使用物资（加速生长）");
        GD.Print("  F4 = 步骤4：继续使用物资直到成熟");
        GD.Print("  F5 = 步骤5：收获该地块作物");
        GD.Print("  F6 = 一键跑完全流程（1→2→3→4→5）");
        GD.Print("==============================================");
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            switch (keyEvent.Keycode)
            {
                case Key.F1: Step1_OpenEnderChestAndSelectCrop(); break;
                case Key.F2: Step2_AddTestResources(); break;
                case Key.F3: Step3_UseResourceOnEnderCrop(); break;
                case Key.F4: Step4_UseResourcesUntilReady(); break;
                case Key.F5: Step5_HarvestEnderCrop(); break;
                case Key.F6: RunFullFlow(); break;
            }
        }
    }

    /// <summary> 步骤1：打开末影箱，由玩家在界面中自行选择作物 </summary>
    private void Step1_OpenEnderChestAndSelectCrop()
    {
        // 确保有足够金币支付代价（末影箱多数是金币代价）
        var player = _gameManager.PlayerData;
        if (player.Gold < 100)
        {
            player.Gold = 500;
            GD.Print("[步骤1] 已补充金币至 500，便于支付末影箱代价");
        }

        // 打开末影箱（第1层），玩家在界面中手动选择作物
        var chest = _enderChestSystem.OpenEnderChest(1);
        if (chest == null)
        {
            GD.PrintErr("[步骤1] 末影箱打开失败");
            return;
        }
        GD.Print("[步骤1] 末影箱已打开，请在界面中选择作物");
    }

    /// <summary> 步骤2：添加测试用物资 </summary>
    private void Step2_AddTestResources()
    {
        _resourceSystem.AddResource("resource_fertilizer", 2);
        _resourceSystem.AddResource("resource_water", 2);
        _resourceSystem.AddResource("resource_growth_boost", 2);
        GD.Print("[步骤2] 已添加测试物资：肥料x2, 水x2, 生长加速剂x2");
        PrintResourceCount();
    }

    /// <summary> 步骤3：对“末影箱作物”所在的地块使用一次物资 </summary>
    private void Step3_UseResourceOnEnderCrop()
    {
        int plotIndex = GetEnderCropPlotIndex();
        if (plotIndex < 0)
        {
            GD.PrintErr("[步骤3] 请先按 F1 从末影箱选择作物");
            return;
        }

        var plot = _farmingSystem.Plots[plotIndex];
        if (plot.IsReady)
        {
            GD.Print("[步骤3] 该作物已成熟，可直接按 F5 收获");
            return;
        }

        string resourceId = GetFirstAvailableResourceId();
        if (string.IsNullOrEmpty(resourceId))
        {
            GD.PrintErr("[步骤3] 没有可用物资，请先按 F2 添加测试物资");
            return;
        }

        float before = plot.GrowthProgress;
        bool ok = _gameManager.UseResourceOnCrop(resourceId, plotIndex);
        _farmingSystem.UpdateAllCrops();
        float after = _farmingSystem.Plots[plotIndex].GrowthProgress;

        if (ok)
            GD.Print($"[步骤3] 已对地块{plotIndex}使用 {resourceId}，进度 {before:P1} → {after:P1}");
        else
            GD.PrintErr("[步骤3] 使用物资失败");
    }

    /// <summary> 步骤4：反复对末影箱作物使用物资直到成熟 </summary>
    private void Step4_UseResourcesUntilReady()
    {
        int plotIndex = GetEnderCropPlotIndex();
        if (plotIndex < 0)
        {
            GD.PrintErr("[步骤4] 请先按 F1 从末影箱选择作物");
            return;
        }

        var plot = _farmingSystem.Plots[plotIndex];
        if (plot.IsReady)
        {
            GD.Print("[步骤4] 作物已成熟，可直接按 F5 收获");
            return;
        }

        int used = 0;
        while (!_farmingSystem.Plots[plotIndex].IsReady)
        {
            string resourceId = GetFirstAvailableResourceId();
            if (string.IsNullOrEmpty(resourceId))
            {
                GD.Print("[步骤4] 物资用尽，当前进度: " + _farmingSystem.Plots[plotIndex].GrowthProgress.ToString("P1"));
                break;
            }
            bool ok = _gameManager.UseResourceOnCrop(resourceId, plotIndex);
            if (!ok) break;
            used++;
            _farmingSystem.UpdateAllCrops();
        }

        bool nowReady = _farmingSystem.Plots[plotIndex].IsReady;
        GD.Print(nowReady
            ? $"[步骤4] 已用 {used} 次物资，作物已成熟"
            : $"[步骤4] 已用 {used} 次物资，仍未成熟，可再按 F2 加物资后重复 F4 或等待生长");
    }

    /// <summary> 步骤5：收获末影箱作物所在的地块 </summary>
    private void Step5_HarvestEnderCrop()
    {
        int plotIndex = GetEnderCropPlotIndex();
        if (plotIndex < 0)
        {
            GD.PrintErr("[步骤5] 请先按 F1 从末影箱选择作物");
            return;
        }

        var plot = _farmingSystem.Plots[plotIndex];
        if (!plot.IsReady)
        {
            GD.PrintErr("[步骤5] 该地块作物未成熟，请先按 F4 使用物资加速或等待");
            return;
        }

        string cropIdToApply = _enderChestCropId;
        var reward = _farmingSystem.TryHarvestCrop(plotIndex);
        if (reward != null)
        {
            GD.Print($"[步骤5] 收获成功！地块{plotIndex} 奖励: 生命+{reward.HealthRestore}, 金币+{reward.Gold}");
            _enderChestPlotIndex = null;
            _enderChestCropId = null;

            // 收获后主动“应用效果”，才会出现在“激活效果”里（收获本身不会自动应用）
            if (!string.IsNullOrEmpty(cropIdToApply))
            {
                bool effectApplied = _gameManager.ApplyCropEffect(cropIdToApply);
                if (effectApplied)
                    GD.Print($"[步骤5] 已应用作物效果: {cropIdToApply}，请在「激活效果」中查看");
                else
                    GD.Print($"[步骤5] 该作物无效果或效果已激活过，未重复应用");
            }
        }
        else
        {
            GD.PrintErr("[步骤5] 收获失败");
        }
    }

    /// <summary> 一键执行：1→2→3→4→5（末影箱选作物→加物资→用物资直到成熟→收获） </summary>
    private void RunFullFlow()
    {
        GD.Print("---------- 开始一键全流程 ----------");
        Step1_OpenEnderChestAndSelectCrop();
        Step2_AddTestResources();
        Step3_UseResourceOnEnderCrop();
        Step4_UseResourcesUntilReady();
        Step5_HarvestEnderCrop();
        GD.Print("---------- 一键全流程结束 ----------");
    }

    private int GetEnderCropPlotIndex()
    {
        if (_enderChestPlotIndex.HasValue)
            return _enderChestPlotIndex.Value;

        if (string.IsNullOrEmpty(_enderChestCropId))
            return -1;

        foreach (var plot in _farmingSystem.Plots.Values)
        {
            if (plot.IsOccupied && plot.CropId == _enderChestCropId)
            {
                _enderChestPlotIndex = plot.Index;
                return plot.Index;
            }
        }
        return -1;
    }

    private string GetFirstAvailableResourceId()
    {
        foreach (var kv in _resourceSystem.GetPlayerResources())
        {
            if (kv.Value > 0)
                return kv.Key;
        }
        return null;
    }

    private void PrintResourceCount()
    {
        var res = _resourceSystem.GetPlayerResources();
        if (res.Count == 0) return;
        foreach (var kv in res)
        {
            var data = _resourceSystem.GetResourceData(kv.Key);
            GD.Print($"    当前物资: {data?.Name} x{kv.Value}");
        }
    }
}
