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
    private Dictionary<string, StatusEffectData> _statusEffects = new();
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
            Description = "立即回复10点生命",
            GrowthTimeSeconds = 30,
            Rewards = new CropReward
            {
                HealthRestore = 10,
                Gold = 10
            },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = null // 无效果
        };
        
        // 属性加成作物 - 生命上限提升
        _crops["crop_health_boost"] = new CropData
        {
            Id = "crop_health_boost",
            Name = "胡萝卜",
            Description = "永久增加3点最大生命",
            GrowthTimeSeconds = 45,
            Rewards = new CropReward
            {
                HealthRestore = 0,
                Gold = 15
            },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                StatBoosts = new Dictionary<string, float>
                {
                    { "MaxHealth", 3f }
                }
            }
        };
        
        // 属性加成作物 - 攻击力提升
        _crops["crop_attack_boost"] = new CropData
        {
            Id = "crop_attack_boost",
            Name = "辣椒",
            Description = "永久增加10%攻击力（建议最多触发7次）",
            GrowthTimeSeconds = 45,
            Rewards = new CropReward
            {
                Gold = 20
            },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                StatBoosts = new Dictionary<string, float>
                {
                    { "Attack", 0.10f }
                },
                ConditionalModifiers = new Dictionary<string, float>
                {
                    { "attack_boost_max_uses", 7f }
                }
            }
        };

        _crops["crop_potato"] = new CropData
        {
            Id = "crop_potato",
            Name = "土豆",
            Description = "收获获得30金币",
            GrowthTimeSeconds = 25,
            Rewards = new CropReward { Gold = 30 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = null
        };

        _crops["crop_cactus"] = new CropData
        {
            Id = "crop_cactus",
            Name = "仙人掌",
            Description = "永久获得5点防御",
            GrowthTimeSeconds = 50,
            Rewards = new CropReward { Gold = 20 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                StatBoosts = new Dictionary<string, float>
                {
                    { "Defense", 5f }
                }
            }
        };

        _crops["crop_sunflower"] = new CropData
        {
            Id = "crop_sunflower",
            Name = "向日葵",
            Description = "增加1点行动力上限（最多+3）",
            GrowthTimeSeconds = 60,
            Rewards = new CropReward { Gold = 25 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                StatBoosts = new Dictionary<string, float>
                {
                    { "Energy", 1f }
                },
                ConditionalModifiers = new Dictionary<string, float>
                {
                    { "energy_bonus_cap", 3f }
                }
            }
        };

        _crops["crop_garlic"] = new CropData
        {
            Id = "crop_garlic",
            Name = "大蒜",
            Description = "永久获得虚弱抗性（虚弱层数-1，建议最多1次）",
            GrowthTimeSeconds = 50,
            Rewards = new CropReward { Gold = 25 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                ConditionalModifiers = new Dictionary<string, float>
                {
                    { "weak_resist_on_apply", 1f },
                    { "weak_resist_max_uses", 1f }
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
            Name = "遗忘花",
            Description = "可移除一张手牌或一项属性负面",
            GrowthTimeSeconds = 90,
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
            Name = "重生草",
            Description = "提供单局复活一次的资格（设计扩展字段）",
            GrowthTimeSeconds = 120,
            Rewards = new CropReward
            {
                Gold = 50
            },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                ConditionalModifiers = new Dictionary<string, float>
                {
                    { "revive_once", 1f }
                }
            }
        };
        
        // 珍稀作物 - 击败Boss后解锁（综合效果）
        _crops["crop_rare_boss"] = new CropData
        {
            Id = "crop_rare_boss",
            Name = "金坷垃",
            Description = "所有属性+5%，并附带下一层敌人生命+20%的代价",
            GrowthTimeSeconds = 90,
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
                    { "MaxHealth", 0.05f },
                    { "Attack", 0.05f },
                    { "Defense", 0.05f }
                },
                NextBattleModifiers = new Dictionary<string, float>
                {
                    { "enemy_health_multiplier", 1.20f }
                }
            }
        };

        _crops["crop_speed_mushroom"] = new CropData
        {
            Id = "crop_speed_mushroom",
            Name = "加速菇",
            Description = "所有正在生长的作物进度+20%",
            GrowthTimeSeconds = 60,
            Rewards = new CropReward { Gold = 15 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                ConditionalModifiers = new Dictionary<string, float>
                {
                    { "farm_growth_boost", 0.20f }
                }
            }
        };

        _crops["crop_copy_melon"] = new CropData
        {
            Id = "crop_copy_melon",
            Name = "复制瓜",
            Description = "复制一张当前牌库中的随机卡牌",
            GrowthTimeSeconds = 90,
            Rewards = new CropReward { Gold = 20 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                ConditionalModifiers = new Dictionary<string, float>
                {
                    { "copy_random_deck_card", 1f }
                }
            }
        };

        _crops["crop_bravery_root"] = new CropData
        {
            Id = "crop_bravery_root",
            Name = "勇气根",
            Description = "下次战斗开始时获得2层力量",
            GrowthTimeSeconds = 900,
            Rewards = new CropReward { Gold = 15 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                NextBattleModifiers = new Dictionary<string, float>
                {
                    { "gain_strength_stacks", 2f }
                }
            }
        };

        _crops["crop_patience_leaf"] = new CropData
        {
            Id = "crop_patience_leaf",
            Name = "忍耐叶",
            Description = "下次战斗开始时获得1层铁壁",
            GrowthTimeSeconds = 900,
            Rewards = new CropReward { Gold = 15 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData
            {
                NextBattleModifiers = new Dictionary<string, float>
                {
                    { "gain_ironwall_stacks", 1f }
                }
            }
        };
    }

    private void InitializeStatusDefinitions()
    {
        _statusEffects.Clear();

        _statusEffects["poison"] = new StatusEffectData
        {
            Id = "poison",
            Name = "中毒",
            Description = "每回合开始受到层数x4%生命伤害（无视防御）",
            StackRule = "可叠加，每层独立计时",
            Duration = "1-3回合",
            Notes = "敌我双方可中毒",
            IsStackable = true,
            MaxStacks = 99,
            NumericParams = new Dictionary<string, float> { { "hp_percent_per_stack", 0.04f } }
        };

        _statusEffects["weak"] = new StatusEffectData
        {
            Id = "weak",
            Name = "虚弱",
            Description = "造成伤害降低10%",
            StackRule = "最多3层，每层-10%",
            Duration = "2回合",
            Notes = "乘法计算",
            IsStackable = true,
            MaxStacks = 3,
            NumericParams = new Dictionary<string, float> { { "damage_multiplier_per_stack", -0.10f } }
        };

        _statusEffects["vulnerable"] = new StatusEffectData
        {
            Id = "vulnerable",
            Name = "易伤",
            Description = "受到的伤害增加20%",
            StackRule = "最多2层，每层+20%",
            Duration = "2回合",
            Notes = "敌我双方均有效",
            IsStackable = true,
            MaxStacks = 2,
            NumericParams = new Dictionary<string, float> { { "taken_damage_multiplier_per_stack", 0.20f } }
        };

        _statusEffects["burn"] = new StatusEffectData
        {
            Id = "burn",
            Name = "燃烧",
            Description = "每回合结束受到层数x8%生命伤害，并递减1层",
            StackRule = "可叠加",
            Duration = "按卡牌说明",
            Notes = "类似中毒但会自然衰减",
            IsStackable = true,
            MaxStacks = 99,
            NumericParams = new Dictionary<string, float> { { "hp_percent_per_stack", 0.08f }, { "decay_per_turn", 1f } }
        };

        _statusEffects["strength"] = new StatusEffectData
        {
            Id = "strength",
            Name = "力量",
            Description = "攻击伤害提升3点",
            StackRule = "可叠加，最多5层",
            Duration = "本回合",
            Notes = "临时增益",
            IsStackable = true,
            MaxStacks = 5,
            NumericParams = new Dictionary<string, float> { { "flat_damage_per_stack", 3f } }
        };

        _statusEffects["ironwall"] = new StatusEffectData
        {
            Id = "ironwall",
            Name = "铁壁",
            Description = "防御力提升50%",
            StackRule = "最多2层",
            Duration = "2回合",
            Notes = "大幅提升防御",
            IsStackable = true,
            MaxStacks = 2,
            NumericParams = new Dictionary<string, float> { { "defense_multiplier_per_stack", 0.5f } }
        };

        _statusEffects["lifesteal"] = new StatusEffectData
        {
            Id = "lifesteal",
            Name = "嗜血",
            Description = "造成伤害的20%回复生命",
            StackRule = "不可叠加",
            Duration = "2回合",
            Notes = "续航向增益",
            IsStackable = false,
            MaxStacks = 1,
            NumericParams = new Dictionary<string, float> { { "lifesteal_ratio", 0.2f } }
        };
    }
    
    public override void _Ready()
    {
        InitializeTestData();
        InitializeStatusDefinitions();
        LoadEnemiesFromDirectory();
        GD.Print($"数据加载完成: {_cards.Count}卡牌, {_normalEnemies.Count}普通敌人, {_bossEnemies.Count}Boss, {_crops.Count}作物, {_statusEffects.Count}状态");
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

    public StatusEffectData GetStatusEffect(string id)
    {
        return _statusEffects.GetValueOrDefault(id);
    }

    public List<StatusEffectData> GetAllStatusEffects()
    {
        return new List<StatusEffectData>(_statusEffects.Values);
    }
}