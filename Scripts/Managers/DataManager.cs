using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public partial class DataManager : Node
{
    private Dictionary<string, CardData> _cards = new();
    private Dictionary<string, EnemyData> _enemies = new();
    private Dictionary<string, CropData> _crops = new();
    private Dictionary<string, object> _items = new();
    
    // 简单测试数据
    private void InitializeTestData()
    {
        // 测试卡牌数据
        _cards["card_attack_basic"] = new CardData
        {
            Id = "card_attack_basic",
            Name = "基础攻击",
            Description = "造成基础伤害",
            Type = GameEnums.CardType.Attack,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "damage", 6 } }
        };
        
        _cards["card_defend_basic"] = new CardData
        {
            Id = "card_defend_basic",
            Name = "基础防御",
            Description = "获得防御",
            Type = GameEnums.CardType.Skill,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "defense", 5 } }
        };
        
        // 测试敌人数据
        _enemies["enemy_slime"] = new EnemyData
        {
            Id = "enemy_slime",
            Name = "史莱姆",
            Health = 30,
            Attack = 5,
            Defense = 0
        };
        
        // 测试作物数据 - 基础作物（无效果）
        _crops["crop_wheat"] = new CropData
        {
            Id = "crop_wheat",
            Name = "小麦",
            Description = "基础作物，收获后恢复生命值",
            GrowthTimeSeconds = 300, // 5分钟
            Rewards = new CropReward
            {
                HealthRestore = 20,
                Gold = 10
            },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = null // 无效果
        };
        
        // 属性加成作物 - 生命上限提升
        _crops["crop_health_boost"] = new CropData
        {
            Id = "crop_health_boost",
            Name = "生命之果",
            Description = "永久提升最大生命值20点",
            GrowthTimeSeconds = 600, // 10分钟
            Rewards = new CropReward
            {
                HealthRestore = 30,
                Gold = 15
            },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                StatBoosts = new Dictionary<string, float>
                {
                    { "MaxHealth", 20f }
                }
            }
        };
        
        // 属性加成作物 - 攻击力提升
        _crops["crop_attack_boost"] = new CropData
        {
            Id = "crop_attack_boost",
            Name = "力量之种",
            Description = "永久提升攻击力5点",
            GrowthTimeSeconds = 600, // 10分钟
            Rewards = new CropReward
            {
                Gold = 20
            },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                StatBoosts = new Dictionary<string, float>
                {
                    { "Attack", 5f }
                }
            }
        };
        
        // 诅咒权衡作物 - 降低速度，提高攻击
        _crops["crop_curse_power"] = new CropData
        {
            Id = "crop_curse_power",
            Name = "诅咒之藤",
            Description = "降低速度20%，提高攻击力30%",
            GrowthTimeSeconds = 900, // 15分钟
            Rewards = new CropReward
            {
                Gold = 25
            },
            EffectType = GameEnums.CropEffectType.CurseTrade,
            EffectData = new CropEffectData
            {
                CurseModifiers = new Dictionary<string, float>
                {
                    { "Speed", -0.2f } // 降低20%
                },
                TradeModifiers = new Dictionary<string, float>
                {
                    { "Attack", 0.3f } // 提升30%
                }
            }
        };
        
        // 遗忘作物 - 删除卡牌
        _crops["crop_forget"] = new CropData
        {
            Id = "crop_forget",
            Name = "遗忘之花",
            Description = "删除卡组中的1张随机卡牌",
            GrowthTimeSeconds = 450, // 7.5分钟
            Rewards = new CropReward
            {
                Gold = 30
            },
            EffectType = GameEnums.CropEffectType.Forget,
            EffectData = new CropEffectData
            {
                CardsToRemove = 1
            }
        };
        
        // 遗忘作物 - 删除多张卡牌（更强效果）
        _crops["crop_forget_major"] = new CropData
        {
            Id = "crop_forget_major",
            Name = "大遗忘之果",
            Description = "删除卡组中的2张随机卡牌",
            GrowthTimeSeconds = 1200, // 20分钟
            Rewards = new CropReward
            {
                Gold = 50
            },
            EffectType = GameEnums.CropEffectType.Forget,
            EffectData = new CropEffectData
            {
                CardsToRemove = 2
            }
        };
        
        // 珍稀作物 - 击败Boss后解锁（综合效果）
        _crops["crop_rare_boss"] = new CropData
        {
            Id = "crop_rare_boss",
            Name = "母体核心",
            Description = "击败最终Boss后解锁，大幅提升所有属性",
            GrowthTimeSeconds = 1800, // 30分钟
            Rewards = new CropReward
            {
                HealthRestore = 50,
                Gold = 100
            },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                StatBoosts = new Dictionary<string, float>
                {
                    { "MaxHealth", 50f },
                    { "Attack", 10f },
                    { "Defense", 5f }
                }
            }
        };
    }
    
    public override void _Ready()
    {
        InitializeTestData();
        GD.Print($"数据加载完成: {_cards.Count}卡牌, {_enemies.Count}敌人, {_crops.Count}作物");
    }
    
    // 数据获取方法
    public CardData GetCard(string id)
    {
        return _cards.GetValueOrDefault(id);
    }
    
    public EnemyData GetEnemy(string id)
    {
        return _enemies.GetValueOrDefault(id);
    }
    
    public CropData GetCrop(string id)
    {
        return _crops.GetValueOrDefault(id);
    }
    
    public List<CardData> GetAllCards()
    {
        return new List<CardData>(_cards.Values);
    }
    
    public List<CropData> GetAllCrops()
    {
        return new List<CropData>(_crops.Values);
    }
}