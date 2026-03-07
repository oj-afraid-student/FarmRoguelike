// FarmingSystem.cs - 农场管理
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class FarmingSystem : Node
{
    private Dictionary<int, CropPlot> _plots = new();
    private DateTime _lastUpdateTime;
    
    public IReadOnlyDictionary<int, CropPlot> Plots => _plots;

    public override void _Ready()
    {
        InitializePlots(6); // 默认6块地
        _lastUpdateTime = DateTime.Now;
        
        // 每10秒更新一次作物生长
        var timer = new Timer();
        AddChild(timer);
        timer.WaitTime = 10.0;
        timer.Timeout += UpdateAllCrops;
        timer.Start();
        
        GameRoot.Instance.EventBus.CropPlanted += OnCropPlanted;
    }

    private void InitializePlots(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _plots[i] = new CropPlot
            {
                Index = i,
                IsOccupied = false,
                CropId = null,
                PlantTime = null,
                GrowthProgress = 0f,
                IsReady = false
            };
        }
    }

    public bool TryPlantCrop(int plotIndex, string cropId)
    {
        if (!_plots.ContainsKey(plotIndex))
        {
            GD.PrintErr($"地块不存在: {plotIndex}");
            return false;
        }

        var plot = _plots[plotIndex];
        if (plot.IsOccupied)
        {
            GD.Print($"地块 {plotIndex} 已被占用");
            return false;
        }

        var cropData = GameRoot.Instance.DataManager.GetCrop(cropId);
        if (cropData == null)
        {
            GD.PrintErr($"作物不存在: {cropId}");
            return false;
        }

        // 更新地块信息
        plot.IsOccupied = true;
        plot.CropId = cropId;
        plot.PlantTime = DateTime.Now;
        plot.GrowthProgress = 0f;
        plot.IsReady = false;
        plot.Stage = 0;

        GD.Print($"在地块 {plotIndex} 种植了 {cropData.Name}");
        
        // 发送事件
        GameRoot.Instance.EventBus.EmitCropPlanted(cropId, plotIndex);
        
        return true;
    }

    public void UpdateAllCrops()
    {
        var currentTime = DateTime.Now;
        var timeDelta = (float)(currentTime - _lastUpdateTime).TotalSeconds;
        _lastUpdateTime = currentTime;

        foreach (var plot in _plots.Values.Where(p => p.IsOccupied && !p.IsReady))
        {
            UpdateCropGrowth(plot, timeDelta);
        }
    }

    private void UpdateCropGrowth(CropPlot plot, float deltaSeconds)
    {
        if (string.IsNullOrEmpty(plot.CropId) || plot.PlantTime == null)
            return;

        var cropData = GameRoot.Instance.DataManager.GetCrop(plot.CropId);
        if (cropData == null)
            return;

        var elapsedTime = (float)(DateTime.Now - plot.PlantTime.Value).TotalSeconds;
        plot.GrowthProgress = Mathf.Min(1.0f, elapsedTime / cropData.GrowthTimeSeconds);

        // 更新生长阶段
        if (cropData.Stages.Count > 0)
        {
            var stageIndex = (int)(plot.GrowthProgress * cropData.Stages.Count);
            plot.Stage = Mathf.Min(stageIndex, cropData.Stages.Count - 1);
        }

        // 检查是否成熟
        if (plot.GrowthProgress >= 1.0f && !plot.IsReady)
        {
            plot.IsReady = true;
            GD.Print($"作物 {cropData.Name} 已成熟!");
        }

        // 发送生长更新事件
        GameRoot.Instance.EventBus.EmitCropGrowthUpdated(plot.CropId, plot.Index, plot.GrowthProgress);
    }

    public CropReward TryHarvestCrop(int plotIndex)
    {
        if (!_plots.ContainsKey(plotIndex))
        {
            GD.PrintErr($"地块不存在: {plotIndex}");
            return null;
        }

        var plot = _plots[plotIndex];
        if (!plot.IsOccupied)
        {
            GD.Print($"地块 {plotIndex} 没有可收获的作物");
            return null;
        }

        // 尝试立即刷新生长状态（防止定时器尚未触发导致成熟状态未更新）
        if (!plot.IsReady)
        {
            UpdateCropGrowth(plot, 0f);
        }

        if (!plot.IsReady)
        {
            GD.Print($"地块 {plotIndex} 没有可收获的作物");
            return null;
        }

        var cropData = GameRoot.Instance.DataManager.GetCrop(plot.CropId);
        if (cropData == null)
        {
            GD.PrintErr($"作物数据不存在: {plot.CropId}");
            return null;
        }

        // 获取奖励
        var reward = cropData.Rewards;
        
        // 清理地块
        plot.IsOccupied = false;
        plot.CropId = null;
        plot.PlantTime = null;
        plot.GrowthProgress = 0f;
        plot.IsReady = false;
        plot.Stage = 0;

        GD.Print($"从地块 {plotIndex} 收获了 {cropData.Name}");
        
        // 发送事件
        GameRoot.Instance.EventBus.EmitCropHarvested(cropData.Id, plotIndex, reward);
        
        return reward;
    }

    public void AccelerateCropGrowth(int plotIndex, float accelerationFactor)
    {
        if (!_plots.ContainsKey(plotIndex))
            return;

        var plot = _plots[plotIndex];
        if (!plot.IsOccupied || plot.PlantTime == null)
            return;

        // 模拟时间前进
        var cropData = GameRoot.Instance.DataManager.GetCrop(plot.CropId);
        if (cropData == null)
            return;

        var newPlantTime = plot.PlantTime.Value.AddSeconds(-cropData.GrowthTimeSeconds * accelerationFactor);
        plot.PlantTime = newPlantTime;
        
        GD.Print($"加速了地块 {plotIndex} 的作物生长");
    }

    private void OnCropPlanted(string cropId, int plotIndex)
    {
        GD.Print($"作物种植事件: {cropId} 在 {plotIndex}");
    }
    
    /// <summary>
    /// 恢复地块数据（用于加载存档）
    /// </summary>
    public void RestorePlots(Dictionary<int, CropPlotSaveData> plotsData, DateTime lastUpdateTime)
    {
        if (plotsData == null)
            return;
        
        // 计算离线时间
        var offlineTime = (DateTime.Now - lastUpdateTime).TotalSeconds;
        
        foreach (var plotSave in plotsData.Values)
        {
            if (!_plots.ContainsKey(plotSave.Index))
                continue;
            
            var plot = _plots[plotSave.Index];
            
            // 恢复地块状态
            plot.IsOccupied = plotSave.IsOccupied;
            plot.CropId = plotSave.CropId;
            plot.PlantTime = plotSave.PlantTime;
            plot.GrowthProgress = plotSave.GrowthProgress;
            plot.IsReady = plotSave.IsReady;
            plot.Stage = plotSave.Stage;
            
            // 如果有作物，更新离线生长
            if (plot.IsOccupied && plot.PlantTime.HasValue && !plot.IsReady)
            {
                var cropData = GameRoot.Instance?.DataManager?.GetCrop(plot.CropId);
                if (cropData != null)
                {
                    // 计算离线生长进度
                    var elapsedTime = (float)(DateTime.Now - plot.PlantTime.Value).TotalSeconds;
                    plot.GrowthProgress = Mathf.Min(1.0f, elapsedTime / cropData.GrowthTimeSeconds);
                    
                    // 更新生长阶段
                    if (cropData.Stages.Count > 0)
                    {
                        var stageIndex = (int)(plot.GrowthProgress * cropData.Stages.Count);
                        plot.Stage = Mathf.Min(stageIndex, cropData.Stages.Count - 1);
                    }
                    
                    // 检查是否成熟
                    if (plot.GrowthProgress >= 1.0f)
                    {
                        plot.IsReady = true;
                    }
                }
            }
        }
        
        _lastUpdateTime = DateTime.Now;
        GD.Print($"恢复 {plotsData.Count} 个地块数据，离线时间: {offlineTime:F1}秒");
    }
    
    /// <summary>
    /// 获取最后更新时间（用于保存）
    /// </summary>
    public DateTime GetLastUpdateTime()
    {
        return _lastUpdateTime;
    }
}

// 数据类
public class CropPlot
{
    public int Index { get; set; }
    public bool IsOccupied { get; set; }
    public string CropId { get; set; }
    public DateTime? PlantTime { get; set; }
    public float GrowthProgress { get; set; }
    public bool IsReady { get; set; }
    public int Stage { get; set; }
}
