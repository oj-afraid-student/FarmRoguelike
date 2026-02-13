// CombatResourceData.cs - 战斗物资相关数据结构
using System;
using System.Collections.Generic;

// 物资类型
public enum ResourceType
{
    Fertilizer,         // 普通肥料 - 加速生长50%
    Water,              // 水 - 加速生长30%
    SpecialFertilizer,  // 特殊肥料 - 加速生长100%并增强效果
    GrowthBoost,        // 生长加速剂 - 立即完成一定进度
    QualityBoost        // 品质提升剂 - 增强作物效果
}

// 物资数据
[Serializable]
public class ResourceData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public ResourceType Type { get; set; }
    public float GrowthAcceleration { get; set; } // 生长加速倍率（0.5 = 加速50%）
    public float EffectEnhancement { get; set; } // 效果增强倍率（0.2 = 增强20%）
    public float InstantProgress { get; set; } // 立即完成的进度（0.3 = 完成30%）
    public int Rarity { get; set; } = 1; // 稀有度（1-5，5最稀有）
}

// 玩家物资库存
[Serializable]
public class PlayerResourceInventory
{
    public Dictionary<string, int> Resources { get; set; } = new(); // 物资ID -> 数量
}
