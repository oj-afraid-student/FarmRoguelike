// EnderChestData.cs - 末影箱相关数据结构
using System;
using System.Collections.Generic;

// 末影箱代价类型
public enum EnderChestCostType
{
    Gold,       // 金币
    Health,     // 生命值
    Card,       // 卡牌
    Stat        // 属性（如降低某个属性）
}

// 末影箱代价
[Serializable]
public class EnderChestCost
{
    public EnderChestCostType Type { get; set; }
    public float Value { get; set; } // 代价数值
    public string Description { get; set; } = ""; // 代价描述
    public string StatType { get; set; } = ""; // 如果是属性代价，指定属性类型
}

// 末影箱作物选项
[Serializable]
public class EnderChestCropOption
{
    public string CropId { get; set; }
    public CropData CropData { get; set; } // 作物数据引用
    public EnderChestCost Cost { get; set; } // 获得该作物需要支付的代价
    public bool IsAvailable { get; set; } = true; // 是否可用（可能因为资源不足）
}

// 末影箱数据
[Serializable]
public class EnderChestData
{
    public int FloorNumber { get; set; }
    public List<EnderChestCropOption> AvailableCrops { get; set; } = new();
    public bool IsOpened { get; set; } = false;
    public DateTime OpenTime { get; set; }
}
