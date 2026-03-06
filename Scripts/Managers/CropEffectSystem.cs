// CropEffectSystem.cs - 作物效果管理系统
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class CropEffectSystem : Node
{
    // 已激活的作物效果
    private Dictionary<string, ActiveCropEffect> _activeEffects = new();
    
    // 玩家数据引用
    private GameManager _gameManager;
    private EventBus _eventBus;
    
    public IReadOnlyDictionary<string, ActiveCropEffect> ActiveEffects => _activeEffects;
    
    public override void _Ready()
    {
        // 获取管理器引用
        _gameManager = GameRoot.Instance?.GameManager;
        _eventBus = GameRoot.Instance?.EventBus;
        
        if (_eventBus == null)
        {
            GD.PrintErr("CropEffectSystem: EventBus 未找到");
            return;
        }
        
        // 订阅事件
        SubscribeToEvents();
        
        GD.Print("CropEffectSystem 初始化完成");
    }
    
    private void SubscribeToEvents()
    {
        _eventBus.CropHarvested += OnCropHarvested;
        _eventBus.GameEnded += OnGameEnded;
    }
    
    /// <summary>
    /// 应用作物效果
    /// </summary>
    /// <param name="cropId">作物ID</param>
    /// <returns>是否成功应用</returns>
    public bool ApplyCropEffect(string cropId)
    {
        if (_gameManager == null || _gameManager.PlayerData == null)
        {
            GD.PrintErr("CropEffectSystem: GameManager 或 PlayerData 未找到");
            return false;
        }
        
        var dataManager = GameRoot.Instance?.DataManager;
        if (dataManager == null)
        {
            GD.PrintErr("CropEffectSystem: DataManager 未找到");
            return false;
        }
        
        var cropData = dataManager.GetCrop(cropId);
        if (cropData == null)
        {
            GD.PrintErr($"CropEffectSystem: 作物不存在: {cropId}");
            return false;
        }
        
        // 如果效果已激活，不重复应用
        if (_activeEffects.ContainsKey(cropId))
        {
            GD.Print($"作物效果已激活: {cropId}");
            return false;
        }
        
        // 根据效果类型应用效果
        bool success = false;
        var playerData = _gameManager.PlayerData;
        
        switch (cropData.EffectType)
        {
            case GameEnums.CropEffectType.StatBoost:
                success = ApplyStatBoost(cropId, cropData, playerData);
                break;
                
            case GameEnums.CropEffectType.CurseTrade:
                success = ApplyCurseTrade(cropId, cropData, playerData);
                break;
                
            case GameEnums.CropEffectType.Forget:
                success = ApplyForget(cropId, cropData, playerData);
                break;
                
            default:
                GD.PrintErr($"未知的作物效果类型: {cropData.EffectType}");
                return false;
        }
        
        if (success)
        {
            // 记录激活的效果
            _activeEffects[cropId] = new ActiveCropEffect
            {
                CropId = cropId,
                CropName = cropData.Name,
                EffectType = cropData.EffectType,
                AppliedTime = DateTime.Now
            };
            
            _eventBus?.EmitCropEffectApplied(cropId);
            GD.Print($"成功应用作物效果: {cropData.Name} ({cropId})");
        }
        
        return success;
    }
    
    /// <summary>
    /// 应用属性加成效果
    /// </summary>
    private bool ApplyStatBoost(string cropId, CropData cropData, PlayerData playerData)
    {
        if (cropData.EffectData == null || cropData.EffectData.StatBoosts == null)
        {
            GD.PrintErr($"作物 {cropId} 缺少效果数据");
            return false;
        }
        
        foreach (var boost in cropData.EffectData.StatBoosts)
        {
            // 优先按玩家属性应用；若不是基础属性则落入 PermanentUpgrades
            if (TryResolveStatType(boost.Key, out var statType))
            {
                float currentValue = playerData.GetStat(statType);
                float delta = boost.Value;
                // 约定：绝对值小于1的值按比例加成处理（例如 0.10 => +10%）
                float newValue = Mathf.Abs(delta) < 1.0f
                    ? currentValue * (1.0f + delta)
                    : currentValue + delta;
                
                playerData.SetStat(statType, newValue);
                _gameManager.UpdateStat(statType, newValue);
                
                GD.Print($"属性加成: {statType} {currentValue} -> {newValue} (cfg={boost.Value})");
            }
            else
            {
                if (playerData.PermanentUpgrades == null)
                {
                    playerData.PermanentUpgrades = new Dictionary<string, float>();
                }

                float oldValue = playerData.PermanentUpgrades.GetValueOrDefault(boost.Key, 0f);
                float newValue = oldValue + boost.Value;
                playerData.PermanentUpgrades[boost.Key] = newValue;
                GD.Print($"记录扩展属性加成: {boost.Key} {oldValue} -> {newValue}");
            }
        }

        // 记录“下次战斗生效”类效果，交给 CombatSystem 在 StartCombat 时读取
        if (cropData.EffectData.NextBattleModifiers != null && cropData.EffectData.NextBattleModifiers.Count > 0)
        {
            if (playerData.PermanentUpgrades == null)
            {
                playerData.PermanentUpgrades = new Dictionary<string, float>();
            }

            foreach (var item in cropData.EffectData.NextBattleModifiers)
            {
                float oldValue = playerData.PermanentUpgrades.GetValueOrDefault(item.Key, 0f);
                float newValue = oldValue + item.Value;
                playerData.PermanentUpgrades[item.Key] = newValue;
                GD.Print($"记录下场战斗增益: {item.Key} {oldValue} -> {newValue}");
            }
        }
        
        return true;
    }

    private bool TryResolveStatType(string key, out GameEnums.PlayerStatType statType)
    {
        // 兼容策划表常见写法与中英文别名
        switch (key)
        {
            case "HP":
            case "Health":
            case "生命":
                statType = GameEnums.PlayerStatType.Health;
                return true;
            case "MaxHP":
            case "MaxHealth":
            case "最大生命":
                statType = GameEnums.PlayerStatType.MaxHealth;
                return true;
            case "Attack":
            case "攻击":
                statType = GameEnums.PlayerStatType.Attack;
                return true;
            case "Defense":
            case "防御":
                statType = GameEnums.PlayerStatType.Defense;
                return true;
            case "Energy":
            case "ActionPoint":
            case "行动力":
                statType = GameEnums.PlayerStatType.Energy;
                return true;
            case "Speed":
            case "速度":
                statType = GameEnums.PlayerStatType.Speed;
                return true;
            case "Luck":
            case "幸运":
                statType = GameEnums.PlayerStatType.Luck;
                return true;
            default:
                return Enum.TryParse<GameEnums.PlayerStatType>(key, out statType);
        }
    }
    
    /// <summary>
    /// 应用诅咒权衡效果（降低速度，提高攻击）
    /// </summary>
    private bool ApplyCurseTrade(string cropId, CropData cropData, PlayerData playerData)
    {
        if (cropData.EffectData == null)
        {
            GD.PrintErr($"作物 {cropId} 缺少效果数据");
            return false;
        }
        
        // 应用降低的属性（诅咒）
        if (cropData.EffectData.CurseModifiers != null)
        {
            foreach (var curse in cropData.EffectData.CurseModifiers)
            {
                if (Enum.TryParse<GameEnums.PlayerStatType>(curse.Key, out var statType))
                {
                    float currentValue = playerData.GetStat(statType);
                    // 百分比降低（负数表示降低）
                    float newValue = currentValue * (1.0f + curse.Value);
                    
                    playerData.SetStat(statType, newValue);
                    _gameManager.UpdateStat(statType, newValue);
                    
                    GD.Print($"诅咒效果: {statType} {currentValue} -> {newValue} ({curse.Value * 100}%)");
                }
            }
        }
        
        // 应用提升的属性（权衡）
        if (cropData.EffectData.TradeModifiers != null)
        {
            foreach (var trade in cropData.EffectData.TradeModifiers)
            {
                if (Enum.TryParse<GameEnums.PlayerStatType>(trade.Key, out var statType))
                {
                    float currentValue = playerData.GetStat(statType);
                    // 百分比提升
                    float newValue = currentValue * (1.0f + trade.Value);
                    
                    playerData.SetStat(statType, newValue);
                    _gameManager.UpdateStat(statType, newValue);
                    
                    GD.Print($"权衡效果: {statType} {currentValue} -> {newValue} (+{trade.Value * 100}%)");
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 应用遗忘效果（删除卡牌）
    /// </summary>
    private bool ApplyForget(string cropId, CropData cropData, PlayerData playerData)
    {
        if (cropData.EffectData == null)
        {
            GD.PrintErr($"作物 {cropId} 缺少效果数据");
            return false;
        }
        
        int cardsToRemove = cropData.EffectData.CardsToRemove;
        
        if (cardsToRemove <= 0)
        {
            GD.Print($"作物 {cropId} 的遗忘效果未指定删除卡牌数量");
            return false;
        }
        
        if (playerData.Deck == null || playerData.Deck.Count == 0)
        {
            GD.Print("玩家卡组为空，无法删除卡牌");
            return false;
        }
        
        // 随机删除指定数量的卡牌
        var random = new Random();
        int actualRemoveCount = Math.Min(cardsToRemove, playerData.Deck.Count);
        
        for (int i = 0; i < actualRemoveCount; i++)
        {
            if (playerData.Deck.Count > 0)
            {
                int randomIndex = random.Next(playerData.Deck.Count);
                string removedCard = playerData.Deck[randomIndex];
                playerData.Deck.RemoveAt(randomIndex);
                
                GD.Print($"遗忘效果: 删除卡牌 {removedCard}");
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 移除作物效果
    /// </summary>
    public bool RemoveCropEffect(string cropId)
    {
        if (!_activeEffects.ContainsKey(cropId))
        {
            GD.Print($"作物效果未激活: {cropId}");
            return false;
        }
        
        // 注意：对于已应用的效果，移除可能需要恢复原始值
        // 这里简化处理，只从列表中移除
        // 实际游戏中可能需要保存原始值以便恢复
        
        _activeEffects.Remove(cropId);
        _eventBus?.EmitCropEffectRemoved(cropId);
        
        GD.Print($"移除作物效果: {cropId}");
        return true;
    }
    
    /// <summary>
    /// 获取所有激活的效果
    /// </summary>
    public List<ActiveCropEffect> GetActiveEffects()
    {
        return new List<ActiveCropEffect>(_activeEffects.Values);
    }
    
    /// <summary>
    /// 检查效果是否已激活
    /// </summary>
    public bool IsEffectActive(string cropId)
    {
        return _activeEffects.ContainsKey(cropId);
    }
    
    // 事件处理
    private void OnCropHarvested(string cropId, int plotIndex, CropReward reward)
    {
        // 收获作物时，可以选择是否应用效果
        // 这里暂时不自动应用，需要玩家主动选择
        GD.Print($"作物收获: {cropId}，可以选择应用效果");
    }
    
    private void OnGameEnded(bool isVictory)
    {
        // 游戏结束时，可以选择清除所有效果
        // 这里暂时保留，实际可以根据需求调整
        GD.Print("游戏结束，保留作物效果");
    }
    
    /// <summary>
    /// 清除所有效果（用于加载存档前）
    /// </summary>
    public void ClearAllEffects()
    {
        _activeEffects.Clear();
        GD.Print("清除所有作物效果");
    }
}

// 激活的作物效果数据
[Serializable]
public class ActiveCropEffect
{
    public string CropId { get; set; }
    public string CropName { get; set; }
    public GameEnums.CropEffectType EffectType { get; set; }
    public DateTime AppliedTime { get; set; }
}
