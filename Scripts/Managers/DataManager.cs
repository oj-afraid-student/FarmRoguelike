using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public partial class DataManager : Node
{
    private Dictionary<string, CardData> _cards = new();
    
    // 敌人数值分开放置和加载
    private Dictionary<string, EnemyData> _normalEnemies = new();
    private Dictionary<string, EnemyData> _bossEnemies = new();
    
    private Dictionary<string, CropData> _crops = new();
    private Dictionary<string, object> _items = new();
    
    // 简单测试数据
    private void InitializeTestData()
    {
        _cards["card_scythe_slash"] = new CardData
        {
            Id = "card_scythe_slash",
            Name = "镰刀挥砍",
            Description = "对目标造成 7 点伤害。",
            Type = GameEnums.CardType.Attack,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "damage", 7 } }
        };
        
        _cards["card_hoe_smash"] = new CardData
        {
            Id = "card_hoe_smash",
            Name = "锄头猛击",
            Description = "对目标造成 12 点伤害并回复 1 点能量。",
            Type = GameEnums.CardType.Attack,
            Cost = 2,
            Effects = new Dictionary<string, float> { { "damage", 12 }, { "restore_energy", 1 } }
        };

        _cards["card_raise_tools"] = new CardData
        {
            Id = "card_raise_tools",
            Name = "架起农具",
            Description = "获得 6 点护盾。",
            Type = GameEnums.CardType.Skill,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "defense", 6 } }
        };

        _cards["card_urgent_bandage"] = new CardData
        {
            Id = "card_urgent_bandage",
            Name = "紧急包扎",
            Description = "恢复当前角色 4 点生命值。",
            Type = GameEnums.CardType.Skill,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "heal", 4 } }
        };

        _cards["card_spray_pesticide"] = new CardData
        {
            Id = "card_spray_pesticide",
            Name = "喷农药",
            Description = "对目标施加 1 层“中毒”状态。",
            Type = GameEnums.CardType.Skill,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "poison_stacks", 1 } }
        };

        _cards["card_observe_weakness"] = new CardData
        {
            Id = "card_observe_weakness",
            Name = "观察弱点",
            Description = "抽 1 张牌并使下一张出的攻击牌伤害 +4。",
            Type = GameEnums.CardType.Skill,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "draw", 1 }, { "buff_next_attack", 4 } }
        };
        
        
        // 测试作物数据 - 基础作物（无效果）
        _crops["crop_wheat"] = new CropData
        {
            Id = "crop_wheat",
            Name = "小麦",
            Description = "恢复生命值",
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
        LoadEnemiesFromDirectory();
        GD.Print($"数据加载完成: {_cards.Count}卡牌, {_normalEnemies.Count}普通敌人, {_bossEnemies.Count}Boss, {_crops.Count}作物");
    }
    
    private void LoadEnemiesFromDirectory()
    {
        // 确保文件夹存在并在真实设备上路径无误
        string baseDir = ProjectSettings.GlobalizePath("res://Data/Enemy");
        string normalDir = System.IO.Path.Combine(baseDir, "normal");
        string bossDir = System.IO.Path.Combine(baseDir, "boss");
        
        if (System.IO.Directory.Exists(normalDir))
        {
            string[] normalFiles = System.IO.Directory.GetFiles(normalDir, "*.json");
            foreach (var path in normalFiles)
            {
                try
                {
                    string jsonString = System.IO.File.ReadAllText(path);
                    var enemy = JsonSerializer.Deserialize<EnemyData>(jsonString);
                    if (enemy != null && !string.IsNullOrEmpty(enemy.Id))
                    {
                        _normalEnemies[enemy.Id] = enemy;
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr($"加载普通敌人失败: {path}, 错误: {e.Message}");
                }
            }
        }
        else
        {
            GD.PrintErr($"未找到普通敌人配置文件夹: {normalDir}");
        }

        if (System.IO.Directory.Exists(bossDir))
        {
            string[] bossFiles = System.IO.Directory.GetFiles(bossDir, "*.json");
            foreach (var path in bossFiles)
            {
                try
                {
                    string jsonString = System.IO.File.ReadAllText(path);
                    var enemy = JsonSerializer.Deserialize<EnemyData>(jsonString);
                    if (enemy != null && !string.IsNullOrEmpty(enemy.Id))
                    {
                        _bossEnemies[enemy.Id] = enemy;
                    }
                }
                catch (Exception e)
                {
                    GD.PrintErr($"加载Boss敌人失败: {path}, 错误: {e.Message}");
                }
            }
        }
        else
        {
            GD.PrintErr($"未找到Boss敌人配置文件夹: {bossDir}");
        }
    }
    
    // 数据获取方法
    public CardData GetCard(string id)
    {
        return _cards.GetValueOrDefault(id);
    }
    
    public EnemyData GetEnemy(string id)
    {
        if (_normalEnemies.ContainsKey(id)) return _normalEnemies[id].Clone();
        if (_bossEnemies.ContainsKey(id)) return _bossEnemies[id].Clone();
        return null; // 这里以前返回 _enemies[id]，现在从两个字典查找
    }

    // 判断某个敌人ID是否为Boss类型
    public bool IsBossEnemy(string id)
    {
        return _bossEnemies.ContainsKey(id);
    }
    
    public EnemyData GetRandomNormalEnemy()
    {
        if (_normalEnemies.Count == 0) return null;
        var keys = new List<string>(_normalEnemies.Keys);
        var rand = new Random();
        return _normalEnemies[keys[rand.Next(keys.Count)]].Clone();
    }

    public EnemyData GetRandomBossEnemy()
    {
        if (_bossEnemies.Count == 0) return null;
        var keys = new List<string>(_bossEnemies.Keys);
        var rand = new Random();
        return _bossEnemies[keys[rand.Next(keys.Count)]].Clone();
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