// DataTypes/GameData.cs
using System;
using System.Collections.Generic;
using Godot;

// 玩家数据
[Serializable]
public class PlayerData
{
    public int MaxHealth { get; set; } = 100;  // 初始最大生命值
    public int CurrentHealth { get; set; } = 100; // 初始当前生命值
    public int Attack { get; set; } = 10; // 基础攻击力
    public int Defense { get; set; } = 5; // 基础防御力
    public int ActionPoints { get; set; } = 3; // 每回合行动点数
    public float Speed { get; set; } = 1.0f; // 速度影响行动顺序
    public float Luck { get; set; } = 1.0f; // 幸运值影响掉落等
    public int Gold { get; set; } = 50; // 初始金币数量
    public List<string> Deck { get; set; } = new(); // 卡组中的卡牌ID列表
    public List<string> EquippedCards { get; set; } = new();  // 当前装备的卡牌ID列表
    public Dictionary<string, int> Inventory { get; set; } = new();  // 物品ID及其数量
    public Dictionary<int, CropPlotData> Crops { get; set; } = new(); // 农作物地块数据
    public Dictionary<string, float> PermanentUpgrades { get; set; } = new(); // 例如：攻击力提升百分比等

    public float GetStat(GameEnums.PlayerStatType statType)
    {
        return statType switch
        {
            GameEnums.PlayerStatType.Health => CurrentHealth,
            GameEnums.PlayerStatType.MaxHealth => MaxHealth,
            GameEnums.PlayerStatType.Attack => Attack,
            GameEnums.PlayerStatType.Defense => Defense,
            GameEnums.PlayerStatType.ActionPoints => ActionPoints,
            GameEnums.PlayerStatType.Speed => Speed,
            GameEnums.PlayerStatType.Luck => Luck,
            _ => 0
        };
    }

    public void SetStat(GameEnums.PlayerStatType statType, float value)
    {
        switch (statType)
        {
            case GameEnums.PlayerStatType.Health:
                CurrentHealth = (int)value;
                break;
            case GameEnums.PlayerStatType.MaxHealth:
                MaxHealth = (int)value;
                break;
            case GameEnums.PlayerStatType.Attack:
                Attack = (int)value;
                break;
            case GameEnums.PlayerStatType.Defense:
                Defense = (int)value;
                break;
            case GameEnums.PlayerStatType.ActionPoints:
                ActionPoints = (int)value;
                break;
            case GameEnums.PlayerStatType.Speed:
                Speed = value;
                break;
            case GameEnums.PlayerStatType.Luck:
                Luck = value;
                break;
        }
    }
}

// 作物地块数据
[Serializable]
public class CropPlotData
{
    public string CropId { get; set; }
    public DateTime PlantTime { get; set; }
    public float GrowthProgress { get; set; }
    public bool IsReady { get; set; }
}

// 房间数据
[Serializable]
public class RoomData
{
    public string Id { get; set; }
    public Vector2I Position { get; set; }
    public GameEnums.RoomType Type { get; set; }
    public bool IsVisited { get; set; }
    public bool IsCleared { get; set; }
    public List<string> Enemies { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();
	public List<Vector2I> Connections { get; set; } = new();
}

// 奖励数据
[Serializable]
public class RewardData
{
    public GameEnums.RewardType Type { get; set; }
    public float Value { get; set; }
    public GameEnums.PlayerStatType StatType { get; set; }
    public string CardId { get; set; }
    public string ItemId { get; set; }
}

// 卡牌数据（简化）
[Serializable]
public class CardData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public GameEnums.CardType Type { get; set; }
    public int Cost { get; set; }
    public Dictionary<string, float> Effects { get; set; } = new();
}

// 敌人数据
[Serializable]
public class EnemyData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Health { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public List<string> Abilities { get; set; } = new();

}

// 作物数据
[Serializable]
public class CropData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; } = ""; // 作物描述
    public float GrowthTimeSeconds { get; set; }
    public CropReward Rewards { get; set; }
	public List<string> Stages { get; set; } = new();
    
    // 作物效果相关
    public GameEnums.CropEffectType EffectType { get; set; } = GameEnums.CropEffectType.StatBoost;
    public CropEffectData EffectData { get; set; } // 效果数据
}

// 作物效果数据
[Serializable]
public class CropEffectData
{
    // 属性加成效果
    public Dictionary<string, float> StatBoosts { get; set; } = new(); // 属性名 -> 增加值
    
    // 诅咒权衡效果
    public Dictionary<string, float> CurseModifiers { get; set; } = new(); // 降低的属性
    public Dictionary<string, float> TradeModifiers { get; set; } = new(); // 提升的属性
    
    // 遗忘效果
    public int CardsToRemove { get; set; } = 0; // 删除卡牌数量
    public List<string> StatToRemove { get; set; } = new(); // 要移除的属性（暂时不用）
}

// 作物奖励
[Serializable]
public class CropReward
{
    public int HealthRestore { get; set; }
    public int Gold { get; set; }
    public Dictionary<string, float> StatModifiers { get; set; } = new();
    public List<string> CardsToAdd { get; set; } = new();
    public List<string> CardsToRemove { get; set; } = new();
}

// 游戏配置
[Serializable]
public class GameConfig
{
    public float InitialHealth { get; set; } = 100;
    public float InitialAttack { get; set; } = 10;
    public int InitialGold { get; set; } = 50;
    public List<string> StartingCards { get; set; } = new();
}

