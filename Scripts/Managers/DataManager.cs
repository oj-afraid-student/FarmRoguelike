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

        _cards["card_heavy_axe_strike"] = new CardData
        {
            Id = "card_heavy_axe_strike",
            Name = "重斧打击",
            Description = "造成 12 点伤害。若本回合未使用其他牌，额外造成 8 点伤害。",
            Type = GameEnums.CardType.Attack,
            Cost = 2,
            Effects = new Dictionary<string, float> { { "damage", 12 }, { "heavy_strike_bonus", 8 } }
        };

        _cards["card_hoe_combo"] = new CardData
        {
            Id = "card_hoe_combo",
            Name = "锄头连击",
            Description = "造成 4 点伤害，连续使用 2 次，并获得 1 能量。",
            Type = GameEnums.CardType.Attack,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "damage", 4 }, { "multi_hit", 2 }, { "restore_energy", 1 } }
        };

        _cards["card_last_stand"] = new CardData
        {
            Id = "card_last_stand",
            Name = "破釜沉舟",
            Description = "失去 2 点生命，获得 2 层力量。",
            Type = GameEnums.CardType.Skill,
            Cost = 0,
            Effects = new Dictionary<string, float> { { "lose_hp", 2 }, { "strength_stacks", 2 } }
        };

        _cards["card_scythe_harvest"] = new CardData
        {
            Id = "card_scythe_harvest",
            Name = "镰刀收割",
            Description = "造成 12 点伤害。若目标带有中毒/燃烧/冰冻等 debuff，每层 debuff 使本次伤害+2。",
            Type = GameEnums.CardType.Attack,
            Cost = 2,
            Effects = new Dictionary<string, float> { { "damage", 12 }, { "debuff_bonus_damage", 2 } }
        };

        _cards["card_flame_spray"] = new CardData
        {
            Id = "card_flame_spray",
            Name = "火焰喷射",
            Description = "造成 5 点伤害，施加 2 层燃烧。",
            Type = GameEnums.CardType.Attack,
            Cost = 2,
            Effects = new Dictionary<string, float> { { "damage", 5 }, { "burn_stacks", 2 } }
        };

        _cards["card_frost_spray"] = new CardData
        {
            Id = "card_frost_spray",
            Name = "冰霜喷射",
            Description = "造成 8 点伤害，施加 1 层冰冻。",
            Type = GameEnums.CardType.Attack,
            Cost = 2,
            Effects = new Dictionary<string, float> { { "damage", 8 }, { "freeze_stacks", 1 } }
        };

        _cards["card_thunder_strike"] = new CardData
        {
            Id = "card_thunder_strike",
            Name = "雷霆一击",
            Description = "造成 18 点伤害，施加 1 层眩晕（跳过下回合）。",
            Type = GameEnums.CardType.Attack,
            Cost = 3,
            Effects = new Dictionary<string, float> { { "damage", 18 }, { "stun_stacks", 1 } }
        };

        _cards["card_thorn_poison_blade"] = new CardData
        {
            Id = "card_thorn_poison_blade",
            Name = "荆棘毒刃",
            Description = "造成 7 点伤害，施加 1 层中毒，持续 2 回合。",
            Type = GameEnums.CardType.Attack,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "damage", 7 }, { "poison_stacks", 1 } }
        };

        _cards["card_armor_break_strike"] = new CardData
        {
            Id = "card_armor_break_strike",
            Name = "破甲一击",
            Description = "造成 10 点伤害，施加 2 层破甲（收到伤害增加20%）。",
            Type = GameEnums.CardType.Attack,
            Cost = 2,
            Effects = new Dictionary<string, float> { { "damage", 10 }, { "vulnerable_stacks", 2 } }
        };

        _cards["card_precision_axe"] = new CardData
        {
            Id = "card_precision_axe",
            Name = "精准斧击",
            Description = "造成 8 点伤害，本回合获得精准（无视防御）。",
            Type = GameEnums.CardType.Attack,
            Cost = 2,
            Effects = new Dictionary<string, float> { { "damage", 8 }, { "precision_stacks", 1 } }
        };

        _cards["card_lifesteal_strike"] = new CardData
        {
            Id = "card_lifesteal_strike",
            Name = "嗜血打击",
            Description = "造成 10 点伤害，获得 2 层嗜血（伤害 20%回血）。",
            Type = GameEnums.CardType.Attack,
            Cost = 2,
            Effects = new Dictionary<string, float> { { "damage", 10 }, { "lifesteal_stacks", 2 } }
        };
        
        // ===== 新增能力卡与技能卡 =====
        _cards["card_herbal_soup"] = new CardData
        {
            Id = "card_herbal_soup", Name = "草药汤", Type = GameEnums.CardType.Skill, Cost = 1, Description = "获得 8 点护盾。",
            Effects = new Dictionary<string, float> { { "armor", 8f } }
        };
        _cards["card_bandage"] = new CardData
        {
            Id = "card_bandage", Name = "绷带", Type = GameEnums.CardType.Skill, Cost = 2, Description = "回复 8 点生命，但本回合无法攻击。",
            Effects = new Dictionary<string, float> { { "heal", 8f }, { "pacifist_stacks", 1f } }
        };
        _cards["card_thorn_armor"] = new CardData
        {
            Id = "card_thorn_armor", Name = "荆棘护甲", Type = GameEnums.CardType.Skill, Cost = 1, Description = "获得 6 点防御，并获得 1 层反射（受到攻击时反击3点伤害）。",
            Effects = new Dictionary<string, float> { { "armor", 6f }, { "reflect_stacks", 1f } }
        };
        _cards["card_corrosive_liquid"] = new CardData
        {
            Id = "card_corrosive_liquid", Name = "腐蚀液", Type = GameEnums.CardType.Skill, Cost = 1, Description = "施加 2 层破甲。",
            Effects = new Dictionary<string, float> { { "vulnerable_stacks", 2f } }
        };
        _cards["card_toxic_spray"] = new CardData
        {
            Id = "card_toxic_spray", Name = "剧毒喷雾", Type = GameEnums.CardType.Skill, Cost = 2, Description = "施加 3 层中毒，持续 3 回合。",
            Effects = new Dictionary<string, float> { { "poison_stacks", 3f } }
        };
        _cards["card_tactical_analysis"] = new CardData
        {
            Id = "card_tactical_analysis", Name = "战术分析", Type = GameEnums.CardType.Skill, Cost = 1, Description = "抽 2 张牌，丢弃 1 张牌。",
            Effects = new Dictionary<string, float> { { "draw_cards", 2f }, { "discard_cards", 1f } }
        };
        _cards["card_motivation"] = new CardData
        {
            Id = "card_motivation", Name = "干劲", Type = GameEnums.CardType.Skill, Cost = 0, Description = "获得 1 点临时能量（本回合可用）。",
            Effects = new Dictionary<string, float> { { "gain_energy", 1f } }
        };
        _cards["card_roaring_shout"] = new CardData
        {
            Id = "card_roaring_shout", Name = "振奋呐喊", Type = GameEnums.CardType.Skill, Cost = 2, Description = "本回合获得 振奋。",
            Effects = new Dictionary<string, float> { { "inspire_stacks", 1f } }
        };
        _cards["card_raise_shield"] = new CardData
        {
            Id = "card_raise_shield", Name = "举起护盾", Type = GameEnums.CardType.Skill, Cost = 2, Description = "获得 2 层铁壁。",
            Effects = new Dictionary<string, float> { { "ironwall_stacks", 2f } }
        };
        _cards["card_purify"] = new CardData
        {
            Id = "card_purify", Name = "净化", Type = GameEnums.CardType.Skill, Cost = 1, Description = "移除自身所有负面效果（中毒、虚弱、破甲等）。",
            Effects = new Dictionary<string, float> { { "purify", 1f } }
        };
        _cards["card_weakening_powder"] = new CardData
        {
            Id = "card_weakening_powder", Name = "药粉削弱", Type = GameEnums.CardType.Skill, Cost = 1, Description = "施加 2 层虚弱。",
            Effects = new Dictionary<string, float> { { "weak_stacks", 2f } }
        };
        _cards["card_fearful_scream"] = new CardData
        {
            Id = "card_fearful_scream", Name = "恐惧尖叫", Type = GameEnums.CardType.Attack, Cost = 2, Description = "造成 10 点伤害，施加 1 层眩晕。",
            Effects = new Dictionary<string, float> { { "damage", 10f }, { "stun_stacks", 1f } }
        };
        _cards["card_sonic_shock"] = new CardData
        {
            Id = "card_sonic_shock", Name = "声波冲击", Type = GameEnums.CardType.Attack, Cost = 1, Description = "造成 5 点伤害，施加 1 层缴械（无法使用攻击牌）。",
            Effects = new Dictionary<string, float> { { "damage", 5f }, { "disarm_stacks", 1f } }
        };
        _cards["card_intimidating_scream"] = new CardData
        {
            Id = "card_intimidating_scream", Name = "尖叫恐吓", Type = GameEnums.CardType.Attack, Cost = 1, Description = "造成 7 点伤害，施加 1 层沉默（无法使用技能/能力牌）。",
            Effects = new Dictionary<string, float> { { "damage", 7f }, { "silence_stacks", 1f } }
        };
        
        // ===== 新增能力与高阶技能卡 =====
        _cards["card_frenzy"] = new CardData
        {
            Id = "card_frenzy", Name = "狂暴", Type = GameEnums.CardType.Ability, Cost = 2, Description = "每回合内，每使用一张攻击牌，获得 1 层力量。",
            Effects = new Dictionary<string, float> { { "frenzy_stacks", 1f } }
        };
        _cards["card_focus"] = new CardData
        {
            Id = "card_focus", Name = "专注", Type = GameEnums.CardType.Ability, Cost = 1, Description = "每回合首次使用技能牌时，抽 1 张牌。",
            Effects = new Dictionary<string, float> { { "focus_stacks", 1f } }
        };
        _cards["card_bloodlust"] = new CardData
        {
            Id = "card_bloodlust", Name = "嗜血术", Type = GameEnums.CardType.Skill, Cost = 2, Description = "获得 2 层嗜血。",
            Effects = new Dictionary<string, float> { { "lifesteal_stacks", 2f } }
        };
        _cards["card_elemental_affinity"] = new CardData
        {
            Id = "card_elemental_affinity", Name = "元素亲和", Type = GameEnums.CardType.Skill, Cost = 2, Description = "2 回合内，每次造成燃烧/冰冻/中毒伤害时，额外造成 3 点伤害。",
            Effects = new Dictionary<string, float> { { "elemental_affinity_duration", 2f } } // effect param handles duration, innate strength handles buff power later
        };
        _cards["card_combo_stance"] = new CardData
        {
            Id = "card_combo_stance", Name = "连击架势", Type = GameEnums.CardType.Skill, Cost = 1, Description = "本回合内，每用一张攻击牌，下张攻击牌伤害 +2。",
            Effects = new Dictionary<string, float> { { "combo_stance_stacks", 1f } }
        };
        _cards["card_devil_pact"] = new CardData
        {
            Id = "card_devil_pact", Name = "恶魔契约", Type = GameEnums.CardType.Skill, Cost = 0, Description = "抽 4 张牌，本回合获得诅咒（打牌掉血）。",
            Effects = new Dictionary<string, float> { { "draw_cards", 4f }, { "curse_stacks", 1f } }
        };
        _cards["card_hourglass"] = new CardData
        {
            Id = "card_hourglass", Name = "时光沙漏", Type = GameEnums.CardType.Skill, Cost = 2, Description = "本回合结束后，额外进行一次玩家回合（不重置临时BUFF）。",
            Effects = new Dictionary<string, float> { { "extra_turn_stacks", 1f } }
        };

        InitializeCardAcquisitionData();
        
        // 测试作物数据 - 基础作物（无效果）
        _crops["crop_wheat"] = new CropData
        {
            Id = "crop_wheat",
            Name = "小麦",
            Description = "立即回复10点生命",
            Rarity = "普通",
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
            Rarity = "普通",
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
            Rarity = "普通",
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
            Rarity = "普通",
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
            Rarity = "稀有",
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
            Rarity = "稀有",
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
            Rarity = "稀有",
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
            Rarity = "稀有",
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
            Rarity = "稀有",
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
            Rarity = "史诗",
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
            Rarity = "史诗",
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
            Rarity = "普通",
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
            Rarity = "稀有",
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
            Rarity = "普通",
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
            Rarity = "普通",
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

        // ===== 卡牌获取作物（来自 Plant.csv 卡牌作物段） =====
        _crops["crop_power_fruit"] = new CropData
        {
            Id = "crop_power_fruit",
            Name = "力量果",
            Description = "随机获得重斧打击/锄头连击/破釜沉舟/破甲一击中的1张卡牌",
            Rarity = "普通",
            GrowthTimeSeconds = 30,
            Rewards = new CropReward { Gold = 12 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_thorn"] = new CropData
        {
            Id = "crop_thorn",
            Name = "荆棘",
            Description = "随机获得荆棘护甲/荆棘毒刃中的1张卡牌",
            Rarity = "普通",
            GrowthTimeSeconds = 45,
            Rewards = new CropReward { Gold = 12 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_thunder_vine"] = new CropData
        {
            Id = "crop_thunder_vine",
            Name = "雷电藤",
            Description = "获得雷霆一击卡牌",
            Rarity = "史诗",
            GrowthTimeSeconds = 120,
            Rewards = new CropReward { Gold = 40 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_astragalus"] = new CropData
        {
            Id = "crop_astragalus",
            Name = "黄芪",
            Description = "随机获得精准斧击/振奋呐喊/净化中的1张卡牌",
            Rarity = "稀有",
            GrowthTimeSeconds = 80,
            Rewards = new CropReward { Gold = 20 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_blood_flower"] = new CropData
        {
            Id = "crop_blood_flower",
            Name = "嗜血花",
            Description = "随机获得嗜血术/嗜血打击/狂暴/连击架势中的1张卡牌",
            Rarity = "稀有",
            GrowthTimeSeconds = 90,
            Rewards = new CropReward { Gold = 24 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_unknown_herb"] = new CropData
        {
            Id = "crop_unknown_herb",
            Name = "不知名草药",
            Description = "随机获得草药汤/绷带/腐蚀液/药粉削弱中的1张卡牌",
            Rarity = "普通",
            GrowthTimeSeconds = 50,
            Rewards = new CropReward { Gold = 10 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_mint"] = new CropData
        {
            Id = "crop_mint",
            Name = "薄荷",
            Description = "随机获得战术分析/干劲/专注中的1张卡牌",
            Rarity = "普通",
            GrowthTimeSeconds = 35,
            Rewards = new CropReward { Gold = 10 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_mandrake_flower"] = new CropData
        {
            Id = "crop_mandrake_flower",
            Name = "曼陀罗花",
            Description = "获得恐惧尖叫卡牌",
            Rarity = "史诗",
            GrowthTimeSeconds = 120,
            Rewards = new CropReward { Gold = 35 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_mandela_grass"] = new CropData
        {
            Id = "crop_mandela_grass",
            Name = "曼德拉草",
            Description = "随机获得声波冲击/尖叫恐吓中的1张卡牌",
            Rarity = "稀有",
            GrowthTimeSeconds = 100,
            Rewards = new CropReward { Gold = 20 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_mystic_mushroom"] = new CropData
        {
            Id = "crop_mystic_mushroom",
            Name = "神秘蘑菇",
            Description = "随机获得强力/火焰/寒冰/剧毒蘑菇中的一种",
            Rarity = "稀有",
            GrowthTimeSeconds = 110,
            Rewards = new CropReward { Gold = 24 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_strong_mushroom"] = new CropData
        {
            Id = "crop_strong_mushroom",
            Name = "强力蘑菇",
            Description = "随机获得镰刀收割/举起护盾/元素亲和中的1张卡牌",
            Rarity = "稀有",
            GrowthTimeSeconds = 100,
            Rewards = new CropReward { Gold = 24 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_fire_mushroom"] = new CropData
        {
            Id = "crop_fire_mushroom",
            Name = "火焰蘑菇",
            Description = "随机获得火焰喷射/元素亲和中的1张卡牌",
            Rarity = "稀有",
            GrowthTimeSeconds = 100,
            Rewards = new CropReward { Gold = 22 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_ice_mushroom"] = new CropData
        {
            Id = "crop_ice_mushroom",
            Name = "寒冰蘑菇",
            Description = "随机获得冰霜喷射/元素亲和中的1张卡牌",
            Rarity = "稀有",
            GrowthTimeSeconds = 100,
            Rewards = new CropReward { Gold = 22 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
        };

        _crops["crop_toxic_mushroom"] = new CropData
        {
            Id = "crop_toxic_mushroom",
            Name = "剧毒蘑菇",
            Description = "随机获得剧毒喷雾/元素亲和中的1张卡牌",
            Rarity = "稀有",
            GrowthTimeSeconds = 100,
            Rewards = new CropReward { Gold = 22 },
            EffectType = GameEnums.CropEffectType.StatBoost,
            EffectData = new CropEffectData()
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

        _statusEffects["freeze"] = new StatusEffectData
        {
            Id = "freeze",
            Name = "冰冻",
            Description = "带有冰冻效果",
            StackRule = "可叠加",
            Duration = "1-3回合",
            Notes = "控制/附加状态",
            IsStackable = true,
            MaxStacks = 99,
            NumericParams = new Dictionary<string, float>()
        };

        _statusEffects["stun"] = new StatusEffectData
        {
            Id = "stun",
            Name = "眩晕",
            Description = "跳过下回合",
            StackRule = "可叠加",
            Duration = "1回合",
            Notes = "强力控制效果",
            IsStackable = true,
            MaxStacks = 99,
            NumericParams = new Dictionary<string, float>()
        };

        _statusEffects["precision"] = new StatusEffectData
        {
            Id = "precision",
            Name = "精准",
            Description = "无视敌人防御的伤害增益",
            StackRule = "不可叠加",
            Duration = "本回合",
            Notes = "临时性状态",
            IsStackable = false,
            MaxStacks = 1,
            NumericParams = new Dictionary<string, float>()
        };

        _statusEffects["reflect"] = new StatusEffectData
        {
            Id = "reflect",
            Name = "反射",
            Description = "受到攻击时反弹3点伤害给攻击者",
            StackRule = "可叠加",
            Duration = "1回合",
            Notes = "防守反击",
            IsStackable = true,
            MaxStacks = 99,
            NumericParams = new Dictionary<string, float>()
        };

        _statusEffects["inspire"] = new StatusEffectData
        {
            Id = "inspire",
            Name = "振奋",
            Description = "每次使用卡牌恢复1点生命",
            StackRule = "可叠加",
            Duration = "本回合",
            Notes = "临时续航增益",
            IsStackable = true,
            MaxStacks = 99,
            NumericParams = new Dictionary<string, float>()
        };

        _statusEffects["disarm"] = new StatusEffectData
        {
            Id = "disarm",
            Name = "缴械",
            Description = "无法使用攻击牌",
            StackRule = "覆盖刷新",
            Duration = "1回合",
            Notes = "禁止物理攻击",
            IsStackable = false,
            MaxStacks = 1,
            NumericParams = new Dictionary<string, float>()
        };

        _statusEffects["silence"] = new StatusEffectData
        {
            Id = "silence",
            Name = "沉默",
            Description = "无法使用技能牌和能力牌",
            StackRule = "覆盖刷新",
            Duration = "1回合",
            Notes = "禁止技能魔法",
            IsStackable = false,
            MaxStacks = 1,
            NumericParams = new Dictionary<string, float>()
        };
        
        _statusEffects["pacifist"] = new StatusEffectData
        {
            Id = "pacifist",
            Name = "和平",
            Description = "本回合无法攻击",
            StackRule = "覆盖刷新",
            Duration = "1回合",
            Notes = "临时无法攻击",
            IsStackable = false,
            MaxStacks = 1,
            NumericParams = new Dictionary<string, float>()
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
        _normalEnemies.Clear();
        _bossEnemies.Clear();

        // 导出版中 res:// 位于 PCK 内，必须用 Godot 文件 API 读取。
        LoadEnemyDirectory("res://Data/Enemy/normal", _normalEnemies, "普通");
        LoadEnemyDirectory("res://Data/Enemy/boss", _bossEnemies, "Boss");

        EnsureFallbackEnemies();
    }

    private void LoadEnemyDirectory(string dirPath, Dictionary<string, EnemyData> target, string label)
    {
        if (!DirAccess.DirExistsAbsolute(dirPath))
        {
            GD.PrintErr($"未找到{label}敌人配置文件夹: {dirPath}");
            return;
        }

        using var dir = DirAccess.Open(dirPath);
        if (dir == null)
        {
            GD.PrintErr($"无法打开{label}敌人目录: {dirPath}");
            return;
        }

        dir.ListDirBegin();
        while (true)
        {
            var fileName = dir.GetNext();
            if (string.IsNullOrEmpty(fileName))
            {
                break;
            }

            if (dir.CurrentIsDir())
            {
                continue;
            }

            if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string fullPath = $"{dirPath}/{fileName}";
            try
            {
                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    GD.PrintErr($"读取{label}敌人失败: {fullPath}");
                    continue;
                }

                string jsonString = file.GetAsText();
                var enemy = JsonSerializer.Deserialize<EnemyData>(jsonString);
                if (enemy != null && !string.IsNullOrEmpty(enemy.Id))
                {
                    target[enemy.Id] = enemy;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr($"加载{label}敌人失败: {fullPath}, 错误: {e.Message}");
            }
        }

        dir.ListDirEnd();
    }

    private void EnsureFallbackEnemies()
    {
        if (_normalEnemies.Count == 0)
        {
            _normalEnemies["enemy_fallback_slime"] = new EnemyData
            {
                Id = "enemy_fallback_slime",
                Name = "应急史莱姆",
                Health = 45,
                Attack = 8,
                Defense = 1,
                RewardGold = 10,
                RewardExp = 5
            };
            GD.PrintErr("普通敌人配置为空，已注入兜底普通敌人。");
        }

        if (_bossEnemies.Count == 0)
        {
            _bossEnemies["enemy_fallback_boss"] = new EnemyData
            {
                Id = "enemy_fallback_boss",
                Name = "应急Boss",
                Health = 120,
                Attack = 14,
                Defense = 3,
                RewardGold = 40,
                RewardExp = 20
            };
            GD.PrintErr("Boss敌人配置为空，已注入兜底Boss敌人。");
        }
    }
    
    // 数据获取方法
    public CardData GetCard(string id)
    {
        return _cards.GetValueOrDefault(id);
    }

    public List<string> GetStarterCardIds()
    {
        var result = new List<string>();
        foreach (var card in _cards.Values)
        {
            if (string.Equals(card.AcquisitionMethod, "初始卡牌", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(card.Id);
            }
        }
        return result;
    }

    public List<string> GetUnlockableCardIdsByTriggers(IEnumerable<string> triggerKeywords, IEnumerable<string> alreadyUnlockedCardIds = null)
    {
        var result = new List<string>();
        var unlockedSet = new HashSet<string>(alreadyUnlockedCardIds ?? new List<string>());
        var triggers = NormalizeKeywords(triggerKeywords);

        if (triggers.Count == 0)
            return result;

        foreach (var card in _cards.Values)
        {
            if (unlockedSet.Contains(card.Id))
                continue;

            if (string.Equals(card.AcquisitionMethod, "初始卡牌", StringComparison.OrdinalIgnoreCase))
                continue;

            if (IsAcquisitionMatched(card.AcquisitionMethod, triggers))
            {
                result.Add(card.Id);
            }
        }

        return result;
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

    public List<CropData> GetCropsByRarity(string rarity)
    {
        var result = new List<CropData>();
        foreach (var crop in _crops.Values)
        {
            if (string.Equals(crop.Rarity, rarity, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(crop);
            }
        }
        return result;
    }

    public List<CropData> GetCardSourceCropsByRarity(string rarity)
    {
        var sourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "力量果", "荆棘", "雷电藤", "黄芪", "嗜血花", "不知名草药", "薄荷",
            "曼陀罗花", "曼德拉草", "神秘蘑菇", "强力蘑菇", "火焰蘑菇", "寒冰蘑菇", "剧毒蘑菇"
        };

        var result = new List<CropData>();
        foreach (var crop in _crops.Values)
        {
            if (!sourceNames.Contains(crop.Name))
                continue;
            if (!string.Equals(crop.Rarity, rarity, StringComparison.OrdinalIgnoreCase))
                continue;
            result.Add(crop);
        }
        return result;
    }

    public StatusEffectData GetStatusEffect(string id)
    {
        return _statusEffects.GetValueOrDefault(id);
    }

    public List<StatusEffectData> GetAllStatusEffects()
    {
        return new List<StatusEffectData>(_statusEffects.Values);
    }

    private void InitializeCardAcquisitionData()
    {
        // 来源：策划 Card.csv（GBK）中的“稀有度/获取方式”
        var meta = new Dictionary<string, (string rarity, string source)>
        {
            ["card_scythe_slash"] = ("基础", "初始卡牌"),
            ["card_hoe_smash"] = ("基础", "初始卡牌"),
            ["card_raise_tools"] = ("基础", "初始卡牌"),
            ["card_urgent_bandage"] = ("基础", "初始卡牌"),
            ["card_spray_pesticide"] = ("基础", "初始卡牌"),
            ["card_observe_weakness"] = ("基础", "初始卡牌"),
            ["card_heavy_axe_strike"] = ("普通", "使用力量果"),
            ["card_hoe_combo"] = ("普通", "使用力量果"),
            ["card_last_stand"] = ("普通", "使用力量果"),
            ["card_scythe_harvest"] = ("稀有", "使用强力蘑菇"),
            ["card_flame_spray"] = ("稀有", "使用火焰蘑菇"),
            ["card_frost_spray"] = ("稀有", "使用寒冰蘑菇"),
            ["card_thunder_strike"] = ("史诗", "使用雷电藤"),
            ["card_thorn_poison_blade"] = ("普通", "使用荆棘"),
            ["card_armor_break_strike"] = ("普通", "使用力量果"),
            ["card_precision_axe"] = ("稀有", "提神薄荷"),
            ["card_lifesteal_strike"] = ("稀有", "使用嗜血花"),
            ["card_herbal_soup"] = ("普通", "使用不知名草药"),
            ["card_bandage"] = ("普通", "使用不知名草药"),
            ["card_thorn_armor"] = ("普通", "使用荆棘"),
            ["card_corrosive_liquid"] = ("普通", "使用不知名草药"),
            ["card_toxic_spray"] = ("稀有", "使用剧毒蘑菇"),
            ["card_tactical_analysis"] = ("普通", "使用薄荷"),
            ["card_motivation"] = ("普通", "使用薄荷"),
            ["card_roaring_shout"] = ("稀有", "使用黄芪"),
            ["card_raise_shield"] = ("稀有", "使用强力蘑菇"),
            ["card_purify"] = ("稀有", "使用黄芪"),
            ["card_weakening_powder"] = ("普通", "使用不知名草药"),
            ["card_fearful_scream"] = ("史诗", "使用曼陀罗花"),
            ["card_sonic_shock"] = ("稀有", "使用曼德拉草"),
            ["card_intimidating_scream"] = ("稀有", "使用曼德拉草"),
            ["card_frenzy"] = ("稀有", "使用嗜血花"),
            ["card_focus"] = ("普通", "使用薄荷"),
            ["card_bloodlust"] = ("稀有", "使用嗜血花"),
            ["card_elemental_affinity"] = ("稀有", "使用火焰/寒冰/剧毒蘑菇"),
            ["card_combo_stance"] = ("稀有", "使用嗜血花"),
            ["card_devil_pact"] = ("普通", "恶魔果实"),
            ["card_hourglass"] = ("史诗", "使用时光草")
        };

        foreach (var kv in meta)
        {
            if (_cards.TryGetValue(kv.Key, out var card))
            {
                card.Rarity = kv.Value.rarity;
                card.AcquisitionMethod = kv.Value.source;
            }
        }
    }

    private static List<string> NormalizeKeywords(IEnumerable<string> keywords)
    {
        var result = new List<string>();
        if (keywords == null)
            return result;

        foreach (var raw in keywords)
        {
            var normalized = NormalizeText(raw);
            if (normalized.Length > 0)
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    private static bool IsAcquisitionMatched(string acquisitionMethod, List<string> normalizedTriggers)
    {
        var method = NormalizeText(acquisitionMethod);
        if (method.Length == 0)
            return false;

        foreach (var trigger in normalizedTriggers)
        {
            if (trigger.Length == 0)
                continue;

            if (method.Contains(trigger, StringComparison.OrdinalIgnoreCase))
                return true;

            if (trigger.Contains(method, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var value = text.Trim();
        value = value.Replace("（", "(").Replace("）", ")");
        value = value.Replace("，", ",").Replace("。", ".");
        value = value.Replace(" ", string.Empty);
        value = value.Replace("\t", string.Empty);
        return value;
    }
}