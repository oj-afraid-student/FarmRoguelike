// SaveData.cs - 保存数据结构
using System;
using System.Collections.Generic;

// 游戏保存数据
[Serializable]
public class GameSaveData
{
    public string Version { get; set; } = "1.0";
    public DateTime SaveTime { get; set; }
    
    // 玩家数据
    public PlayerData PlayerData { get; set; }
    
    // 游戏进度
    public int CurrentFloor { get; set; }
    public GameEnums.GameState CurrentState { get; set; }
    
    // 作物系统数据
    public FarmingSaveData FarmingData { get; set; }
    
    // 作物效果数据
    public CropEffectSaveData CropEffectData { get; set; }
    
    // 战斗物资数据
    public CombatResourceSaveData ResourceData { get; set; }
}

// 农场保存数据
[Serializable]
public class FarmingSaveData
{
    public Dictionary<int, CropPlotSaveData> Plots { get; set; } = new();
    public DateTime LastUpdateTime { get; set; }
}

// 地块保存数据
[Serializable]
public class CropPlotSaveData
{
    public int Index { get; set; }
    public bool IsOccupied { get; set; }
    public string CropId { get; set; }
    public DateTime? PlantTime { get; set; }
    public float GrowthProgress { get; set; }
    public bool IsReady { get; set; }
    public int Stage { get; set; }
}

// 作物效果保存数据
[Serializable]
public class CropEffectSaveData
{
    public List<ActiveCropEffectSaveData> ActiveEffects { get; set; } = new();
}

// 激活效果保存数据
[Serializable]
public class ActiveCropEffectSaveData
{
    public string CropId { get; set; }
    public string CropName { get; set; }
    public GameEnums.CropEffectType EffectType { get; set; }
    public DateTime AppliedTime { get; set; }
}

// 战斗物资保存数据
[Serializable]
public class CombatResourceSaveData
{
    public Dictionary<string, int> Resources { get; set; } = new();
}
