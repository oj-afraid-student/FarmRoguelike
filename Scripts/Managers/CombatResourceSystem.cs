// CombatResourceSystem.cs - 战斗物资系统
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CombatResourceSystem : Node
{
    // 玩家物资库存
    private Dictionary<string, int> _playerResources = new();
    
    // 管理器引用
    private GameManager _gameManager;
    private DataManager _dataManager;
    private FarmingSystem _farmingSystem;
    private EventBus _eventBus;
    
    // 物资数据缓存
    private Dictionary<string, ResourceData> _resourceDataCache = new();
    
    public IReadOnlyDictionary<string, int> PlayerResources => _playerResources;
    
    public override void _Ready()
    {
        // 获取管理器引用
        _gameManager = GameRoot.Instance?.GameManager;
        _dataManager = GameRoot.Instance?.DataManager;
        _farmingSystem = GameRoot.Instance?.FarmingSystem;
        _eventBus = GameRoot.Instance?.EventBus;
        
        if (_eventBus == null)
        {
            GD.PrintErr("CombatResourceSystem: EventBus 未找到");
            return;
        }
        
        // 初始化物资数据
        InitializeResourceData();
        
        // 订阅事件
        SubscribeToEvents();
        
        GD.Print("CombatResourceSystem 初始化完成");
    }
    
    private void InitializeResourceData()
    {
        // 从DataManager加载物资数据（如果存在）
        // 这里先使用硬编码数据，后续可以从配置文件加载
        var resources = GetDefaultResourceData();
        foreach (var resource in resources)
        {
            _resourceDataCache[resource.Id] = resource;
        }
    }
    
    private List<ResourceData> GetDefaultResourceData()
    {
        return new List<ResourceData>
        {
            new ResourceData
            {
                Id = "resource_fertilizer",
                Name = "普通肥料",
                Description = "加速作物生长50%",
                Type = ResourceType.Fertilizer,
                GrowthAcceleration = 0.5f,
                Rarity = 1
            },
            new ResourceData
            {
                Id = "resource_water",
                Name = "水",
                Description = "加速作物生长30%",
                Type = ResourceType.Water,
                GrowthAcceleration = 0.3f,
                Rarity = 1
            },
            new ResourceData
            {
                Id = "resource_special_fertilizer",
                Name = "特殊肥料",
                Description = "加速作物生长100%并增强效果20%",
                Type = ResourceType.SpecialFertilizer,
                GrowthAcceleration = 1.0f,
                EffectEnhancement = 0.2f,
                Rarity = 3
            },
            new ResourceData
            {
                Id = "resource_growth_boost",
                Name = "生长加速剂",
                Description = "立即完成作物30%的生长进度",
                Type = ResourceType.GrowthBoost,
                InstantProgress = 0.3f,
                Rarity = 2
            },
            new ResourceData
            {
                Id = "resource_quality_boost",
                Name = "品质提升剂",
                Description = "增强作物效果15%",
                Type = ResourceType.QualityBoost,
                EffectEnhancement = 0.15f,
                Rarity = 2
            }
        };
    }
    
    private void SubscribeToEvents()
    {
        _eventBus.EnemyDefeated += OnEnemyDefeated;
        _eventBus.CombatEnded += OnCombatEnded;
    }
    
    /// <summary>
    /// 战斗结束时生成物资
    /// </summary>
    public Dictionary<string, int> GenerateCombatResources(bool isVictory, string enemyId = "")
    {
        if (!isVictory)
        {
            GD.Print("战斗失败，不生成物资");
            return new Dictionary<string, int>();
        }
        
        var generatedResources = new Dictionary<string, int>();
        var random = new Random();
        
        // 基础物资生成（每次战斗胜利必定获得）
        // 70%概率获得普通肥料或水
        if (random.NextDouble() < 0.7)
        {
            string resourceId = random.NextDouble() < 0.5 ? "resource_fertilizer" : "resource_water";
            generatedResources[resourceId] = 1;
        }
        
        // 30%概率获得生长加速剂
        if (random.NextDouble() < 0.3)
        {
            generatedResources["resource_growth_boost"] = 1;
        }
        
        // 10%概率获得品质提升剂
        if (random.NextDouble() < 0.1)
        {
            generatedResources["resource_quality_boost"] = 1;
        }
        
        // 5%概率获得特殊肥料（稀有）
        if (random.NextDouble() < 0.05)
        {
            generatedResources["resource_special_fertilizer"] = 1;
        }
        
        // 将生成的物资添加到玩家库存
        foreach (var resource in generatedResources)
        {
            AddResource(resource.Key, resource.Value);
        }
        
        // 发送事件
        _eventBus?.EmitCombatResourcesGenerated(generatedResources);
        
        GD.Print($"战斗胜利，生成物资: {string.Join(", ", generatedResources.Select(r => $"{r.Key} x{r.Value}"))}");
        
        return generatedResources;
    }
    
    /// <summary>
    /// 添加物资到库存
    /// </summary>
    public void AddResource(string resourceId, int amount = 1)
    {
        if (amount <= 0)
            return;
        
        if (_playerResources.ContainsKey(resourceId))
        {
            _playerResources[resourceId] += amount;
        }
        else
        {
            _playerResources[resourceId] = amount;
        }
        
        GD.Print($"获得物资: {resourceId} x{amount}，当前数量: {_playerResources[resourceId]}");
    }
    
    /// <summary>
    /// 使用物资
    /// </summary>
    public bool UseResource(string resourceId, int plotIndex)
    {
        if (!_playerResources.ContainsKey(resourceId) || _playerResources[resourceId] <= 0)
        {
            GD.PrintErr($"物资不足: {resourceId}");
            return false;
        }
        
        if (_farmingSystem == null)
        {
            GD.PrintErr("FarmingSystem 未找到");
            return false;
        }
        
        var resourceData = GetResourceData(resourceId);
        if (resourceData == null)
        {
            GD.PrintErr($"物资数据不存在: {resourceId}");
            return false;
        }
        
        // 检查地块是否有作物
        var plots = _farmingSystem.Plots;
        if (!plots.ContainsKey(plotIndex))
        {
            GD.PrintErr($"地块不存在: {plotIndex}");
            return false;
        }
        
        var plot = plots[plotIndex];
        if (!plot.IsOccupied || string.IsNullOrEmpty(plot.CropId))
        {
            GD.PrintErr($"地块 {plotIndex} 没有作物");
            return false;
        }
        
        // 应用物资效果
        bool success = ApplyResourceToCrop(plotIndex, resourceData);
        
        if (success)
        {
            // 消耗物资
            _playerResources[resourceId]--;
            if (_playerResources[resourceId] <= 0)
            {
                _playerResources.Remove(resourceId);
            }
            
            _eventBus?.EmitResourceAppliedToCrop(plotIndex, resourceId);
            GD.Print($"成功对地块 {plotIndex} 使用 {resourceData.Name}");
        }
        
        return success;
    }
    
    /// <summary>
    /// 对作物应用物资效果
    /// </summary>
    private bool ApplyResourceToCrop(int plotIndex, ResourceData resourceData)
    {
        if (_farmingSystem == null)
            return false;
        
        var plots = _farmingSystem.Plots;
        if (!plots.ContainsKey(plotIndex))
            return false;
        
        var plot = plots[plotIndex];
        if (!plot.IsOccupied || plot.PlantTime == null)
            return false;
        
        // 根据物资类型应用不同效果
        switch (resourceData.Type)
        {
            case ResourceType.Fertilizer:
            case ResourceType.Water:
            case ResourceType.SpecialFertilizer:
                // 加速生长：通过调整种植时间来模拟时间前进
                ApplyGrowthAcceleration(plot, resourceData.GrowthAcceleration);
                break;
                
            case ResourceType.GrowthBoost:
                // 立即完成一定进度
                ApplyInstantProgress(plot, resourceData.InstantProgress);
                break;
                
            case ResourceType.QualityBoost:
                // 增强效果（这个需要在收获时应用，这里先记录）
                // TODO: 实现效果增强机制
                GD.Print($"品质提升剂效果将在收获时应用");
                break;
        }
        
        return true;
    }
    
    /// <summary>
    /// 应用生长加速
    /// </summary>
    private void ApplyGrowthAcceleration(CropPlot plot, float accelerationFactor)
    {
        if (plot.PlantTime == null)
            return;
        
        // 通过将种植时间提前来模拟加速
        var cropData = _dataManager?.GetCrop(plot.CropId);
        if (cropData == null)
            return;
        
        // 计算应该前进的时间
        float timeToAdvance = cropData.GrowthTimeSeconds * accelerationFactor;
        plot.PlantTime = plot.PlantTime.Value.AddSeconds(-timeToAdvance);
        
        GD.Print($"加速生长: 前进 {timeToAdvance} 秒 ({accelerationFactor * 100}%)");
    }
    
    /// <summary>
    /// 应用立即进度
    /// </summary>
    private void ApplyInstantProgress(CropPlot plot, float progressAmount)
    {
        if (plot.PlantTime == null)
            return;
        
        var cropData = _dataManager?.GetCrop(plot.CropId);
        if (cropData == null)
            return;
        
        // 计算应该前进的时间来达到指定进度
        float timeToAdvance = cropData.GrowthTimeSeconds * progressAmount;
        plot.PlantTime = plot.PlantTime.Value.AddSeconds(-timeToAdvance);
        
        GD.Print($"立即完成进度: {progressAmount * 100}%");
    }
    
    /// <summary>
    /// 获取物资数据
    /// </summary>
    public ResourceData GetResourceData(string resourceId)
    {
        return _resourceDataCache.GetValueOrDefault(resourceId);
    }
    
    /// <summary>
    /// 获取玩家拥有的物资数量
    /// </summary>
    public int GetResourceCount(string resourceId)
    {
        return _playerResources.GetValueOrDefault(resourceId, 0);
    }
    
    /// <summary>
    /// 获取所有玩家物资
    /// </summary>
    public Dictionary<string, int> GetPlayerResources()
    {
        return new Dictionary<string, int>(_playerResources);
    }
    
    // 事件处理
    private void OnEnemyDefeated(string enemyId)
    {
        // 战斗胜利时生成物资
        GenerateCombatResources(true, enemyId);
    }
    
    private void OnCombatEnded(bool playerWon)
    {
        // 如果战斗系统没有触发EnemyDefeated事件，这里作为备用
        // 实际上EnemyDefeated已经处理了，这里可以留空或做其他处理
    }
}
