// SaveManager.cs - 数据持久化管理器
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

public partial class SaveManager : Node
{
    private const string SAVE_FILE_NAME = "save_game.json";
    private const string QUICK_SAVE_NAME = "quick_save.json";
    private const string SAVE_SLOT_PREFIX = "slot";   // 存档槽前缀
    private const string SAVE_SLOT_EXT = ".json";     // 存档扩展名

    // 保存目录：相对路径到项目目录的父文件夹的父文件夹下的 saves 文件夹
    // 即： project_root/../../saves
    private string SaveDirectoryPath
    {
        get
        {
            try
            {
                var projectRoot = ProjectSettings.GlobalizePath("res://");
                var relative = Path.Combine(projectRoot, "saves");
                var full = Path.GetFullPath(relative);
                return full;
            }
            catch
            {
                // 回退到用户目录下的 saves
                return ProjectSettings.GlobalizePath("user://saves");
            }
        }
    }

    private string SaveFilePath => Path.Combine(SaveDirectoryPath, SAVE_FILE_NAME);
    private string QuickSaveFilePath => Path.Combine(SaveDirectoryPath, QUICK_SAVE_NAME);
    
    public override void _Ready()
    {
        // 确保保存目录存在
        EnsureSaveDirectoryExists();
        
        GD.Print("SaveManager 初始化完成");
    }

    public override void _Input(InputEvent @event)
    {
        // 仅在按键按下且非回音事件时处理
        if (!(@event is InputEventKey keyEvent))
            return;

        if (!keyEvent.Pressed || keyEvent.IsEcho())
            return;

        // 仅在非战斗时允许快速保存/加载
        var gameManager = GameRoot.Instance?.GameManager;
        if (gameManager == null)
            return;

        if (gameManager.CurrentState == GameEnums.GameState.Combat)
        {
            // 在战斗中禁用快捷存档/读档
            return;
        }

        // F5 快速保存，F9 快速加载
        if (keyEvent.Keycode == Key.F5)
        {
            bool ok = QuickSave();
            if (ok)
                EventBus.Instance?.EmitNotificationRequested("游戏已快速保存 (F5)");
            else
                EventBus.Instance?.EmitNotificationRequested("快速保存失败");
        }
        else if (keyEvent.Keycode == Key.F9)
        {
            bool ok = QuickLoad();
            if (ok)
                EventBus.Instance?.EmitNotificationRequested("已加载快速存档 (F9)");
            else
                EventBus.Instance?.EmitNotificationRequested("快速加载失败：未找到存档或加载出错");
        }
    }
    
    private void EnsureSaveDirectoryExists()
    {
        if (!DirAccess.DirExistsAbsolute(SaveDirectoryPath))
        {
            DirAccess.MakeDirRecursiveAbsolute(SaveDirectoryPath);
            GD.Print($"创建保存目录: {SaveDirectoryPath}");
        }
    }
    
    /// <summary>
    /// 保存游戏
    /// </summary>
    public bool SaveGame(string fileName = null)
    {
        try
        {
            var saveData = CreateSaveData();
            if (saveData == null)
            {
                GD.PrintErr("创建保存数据失败");
                return false;
            }
            
            string filePath = string.IsNullOrEmpty(fileName) 
                ? SaveFilePath 
                : Path.Combine(SaveDirectoryPath, fileName);
            
            // 序列化为JSON
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                IncludeFields = true
            };
            
            string json = JsonSerializer.Serialize(saveData, options);
            
            // 写入文件
            Godot.FileAccess file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"无法打开文件进行写入: {filePath}");
                return false;
            }
            
            file.StoreString(json);
            file.Close();
            
            GD.Print($"游戏已保存到: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"保存游戏失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 快速保存
    /// </summary>
    public bool QuickSave()
    {
        return SaveGame(QUICK_SAVE_NAME);
    }
    
    /// <summary>
    /// 加载游戏
    /// </summary>
    public bool LoadGame(string fileName = null)
    {
        try
        {
            string filePath = string.IsNullOrEmpty(fileName) 
                ? SaveFilePath 
                : Path.Combine(SaveDirectoryPath, fileName);
            
            if (!Godot.FileAccess.FileExists(filePath))
            {
                GD.PrintErr($"保存文件不存在: {filePath}");
                return false;
            }
            
            // 读取文件
            Godot.FileAccess file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"无法打开文件进行读取: {filePath}");
                return false;
            }
            
            string json = file.GetAsText();
            file.Close();
            
            // 反序列化
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            
            var saveData = JsonSerializer.Deserialize<GameSaveData>(json, options);
            if (saveData == null)
            {
                GD.PrintErr("反序列化保存数据失败");
                return false;
            }
            
            // 应用保存数据
            ApplySaveData(saveData);
            
            GD.Print($"游戏已从 {filePath} 加载");
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"加载游戏失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 快速加载
    /// </summary>
    public bool QuickLoad()
    {
        return LoadGame(QUICK_SAVE_NAME);
    }
    
    /// <summary>
    /// 创建保存数据
    /// </summary>
    private GameSaveData CreateSaveData()
    {
        var gameManager = GameRoot.Instance?.GameManager;
        var farmingSystem = GameRoot.Instance?.FarmingSystem;
        var cropEffectSystem = GameRoot.Instance?.CropEffectSystem;
        var resourceSystem = GameRoot.Instance?.CombatResourceSystem;
        
        if (gameManager == null)
        {
            GD.PrintErr("GameManager 未找到");
            return null;
        }
        
        var saveData = new GameSaveData
        {
            Version = "1.0",
            SaveTime = DateTime.Now,
            PlayerData = gameManager.PlayerData,
            CurrentFloor = gameManager.CurrentFloor,
            CurrentState = gameManager.CurrentState
        };
        
        // 保存农场数据
        if (farmingSystem != null)
        {
            saveData.FarmingData = new FarmingSaveData
            {
                LastUpdateTime = DateTime.Now
            };
            
            var plots = farmingSystem.Plots;
            foreach (var plot in plots.Values)
            {
                saveData.FarmingData.Plots[plot.Index] = new CropPlotSaveData
                {
                    Index = plot.Index,
                    IsOccupied = plot.IsOccupied,
                    CropId = plot.CropId,
                    PlantTime = plot.PlantTime,
                    GrowthProgress = plot.GrowthProgress,
                    IsReady = plot.IsReady,
                    Stage = plot.Stage
                };
            }
        }
        
        // 保存作物效果数据
        if (cropEffectSystem != null)
        {
            saveData.CropEffectData = new CropEffectSaveData();
            
            var activeEffects = cropEffectSystem.GetActiveEffects();
            foreach (var effect in activeEffects)
            {
                saveData.CropEffectData.ActiveEffects.Add(new ActiveCropEffectSaveData
                {
                    CropId = effect.CropId,
                    CropName = effect.CropName,
                    EffectType = effect.EffectType,
                    AppliedTime = effect.AppliedTime
                });
            }
        }
        
        // 保存物资数据
        if (resourceSystem != null)
        {
            saveData.ResourceData = new CombatResourceSaveData
            {
                Resources = new Dictionary<string, int>(resourceSystem.GetPlayerResources())
            };
        }
        
        return saveData;
    }
    
    /// <summary>
    /// 应用保存数据
    /// </summary>
    private void ApplySaveData(GameSaveData saveData)
    {
        var gameManager = GameRoot.Instance?.GameManager;
        var farmingSystem = GameRoot.Instance?.FarmingSystem;
        var cropEffectSystem = GameRoot.Instance?.CropEffectSystem;
        var resourceSystem = GameRoot.Instance?.CombatResourceSystem;
        
        if (gameManager == null)
        {
            GD.PrintErr("GameManager 未找到");
            return;
        }
        
        // 恢复玩家数据
        gameManager.RestorePlayerData(saveData.PlayerData);

        // 先恢复各子系统数据（农场、作物效果、物资），确保在切换状态时系统已准备好
        if (farmingSystem != null && saveData.FarmingData != null)
        {
            RestoreFarmingData(farmingSystem, saveData.FarmingData);
        }

        if (cropEffectSystem != null && saveData.CropEffectData != null)
        {
            RestoreCropEffectData(cropEffectSystem, saveData.CropEffectData);
        }

        if (resourceSystem != null && saveData.ResourceData != null)
        {
            RestoreResourceData(resourceSystem, saveData.ResourceData);
        }

        // 恢复当前层数（不触发状态切换）
        gameManager.SetCurrentFloor(saveData.CurrentFloor);

        // 延迟切换游戏状态到下一帧，避免在加载过程中触发大量同步事件导致卡死
        CallDeferred(nameof(DeferredChangeState), (Variant)(int)saveData.CurrentState);
    }

    private void DeferredChangeState(object stateObj)
    {
        try
        {
            var gameManager = GameRoot.Instance?.GameManager;
            if (gameManager != null && stateObj is GameEnums.GameState state)
            {
                gameManager.ChangeState(state);
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"延迟切换游戏状态失败: {ex}");
        }
    }
    
    /// <summary>
    /// 恢复农场数据
    /// </summary>
    private void RestoreFarmingData(FarmingSystem farmingSystem, FarmingSaveData farmingData)
    {
        if (farmingSystem == null || farmingData == null)
            return;
        
        farmingSystem.RestorePlots(farmingData.Plots, farmingData.LastUpdateTime);
        GD.Print("农场数据恢复完成");
    }
    
    /// <summary>
    /// 恢复作物效果数据
    /// </summary>
    private void RestoreCropEffectData(CropEffectSystem cropEffectSystem, CropEffectSaveData effectData)
    {
        if (cropEffectSystem == null || effectData == null)
            return;
        
        // 清除现有效果
        cropEffectSystem.ClearAllEffects();
        
        // 重新应用效果
        GD.Print("恢复作物效果数据...");
        foreach (var effectSave in effectData.ActiveEffects)
        {
            cropEffectSystem.ApplyCropEffect(effectSave.CropId);
        }
        GD.Print($"恢复 {effectData.ActiveEffects.Count} 个作物效果");
    }
    
    /// <summary>
    /// 恢复物资数据
    /// </summary>
    private void RestoreResourceData(CombatResourceSystem resourceSystem, CombatResourceSaveData resourceData)
    {
        GD.Print("恢复物资数据...");
        foreach (var resource in resourceData.Resources)
        {
            resourceSystem.AddResource(resource.Key, resource.Value);
        }
    }
    
    /// <summary>
    /// 检查是否有保存文件
    /// </summary>
    public bool HasSaveFile(string fileName = null)
    {
        string filePath = string.IsNullOrEmpty(fileName) 
            ? SaveFilePath 
            : Path.Combine(SaveDirectoryPath, fileName);
        
        return Godot.FileAccess.FileExists(filePath);
    }
    
    /// <summary>
    /// 删除保存文件
    /// </summary>
    public bool DeleteSaveFile(string fileName = null)
    {
        try
        {
            string filePath = string.IsNullOrEmpty(fileName) 
                ? SaveFilePath 
                : Path.Combine(SaveDirectoryPath, fileName);
            
            if (Godot.FileAccess.FileExists(filePath))
            {
                DirAccess.RemoveAbsolute(filePath);
                GD.Print($"删除保存文件: {filePath}");
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"删除保存文件失败: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 获取保存文件信息
    /// </summary>
    public SaveFileInfo GetSaveFileInfo(string fileName = null)
    {
        string filePath = string.IsNullOrEmpty(fileName) 
            ? SaveFilePath 
            : Path.Combine(SaveDirectoryPath, fileName);
        
        if (!Godot.FileAccess.FileExists(filePath))
        {
            return null;
        }
        
        try
        {
            Godot.FileAccess file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
                return null;
            
            string json = file.GetAsText();
            file.Close();
            
            var options = new JsonSerializerOptions { IncludeFields = true };
            var saveData = JsonSerializer.Deserialize<GameSaveData>(json, options);
            
            if (saveData == null)
                return null;
            
            return new SaveFileInfo
            {
                FileName = fileName ?? SAVE_FILE_NAME,
                SaveTime = saveData.SaveTime,
                Version = saveData.Version,
                Floor = saveData.CurrentFloor
            };
        }
        catch
        {
            return null;
        }
    }



    /// <summary>
    /// 获取指定槽位的文件名
    /// </summary>
    private string GetSlotFileName(int slot)
    {
        return $"{SAVE_SLOT_PREFIX}{slot}{SAVE_SLOT_EXT}";
    }

    /// <summary>
    /// 保存游戏到指定槽位
    /// </summary>
    public bool SaveToSlot(int slot)
    {
        string fileName = GetSlotFileName(slot);
        return SaveGame(fileName);
    }

    /// <summary>
    /// 从指定槽位加载游戏
    /// </summary>
    public bool LoadFromSlot(int slot)
    {
        string fileName = GetSlotFileName(slot);
        return LoadGame(fileName);
    }

    /// <summary>
    /// 检查指定槽位是否有存档
    /// </summary>
    public bool HasSlotSave(int slot)
    {
        string fileName = GetSlotFileName(slot);
        return HasSaveFile(fileName);
    }

    /// <summary>
    /// 获取指定槽位的存档信息
    /// </summary>
    public SaveFileInfo GetSlotInfo(int slot)
    {
        string fileName = GetSlotFileName(slot);
        return GetSaveFileInfo(fileName);
    }

    /// <summary>
    /// 删除指定槽位的存档
    /// </summary>
    public bool DeleteSlotSave(int slot)
    {
        string fileName = GetSlotFileName(slot);
        return DeleteSaveFile(fileName);
    }

    /// <summary>
    /// 获取所有槽位的存档信息（最多支持3个槽位）
    /// </summary>
    public List<SaveFileInfo> GetAllSlotInfos()
    {
        var infos = new List<SaveFileInfo>();
        for (int i = 1; i <= 3; i++)
        {
            var info = GetSlotInfo(i);
            infos.Add(info);
        }
        return infos;
    }
}

// 保存文件信息
public class SaveFileInfo
{
    public string FileName { get; set; }
    public DateTime SaveTime { get; set; }
    public string Version { get; set; }
    public int Floor { get; set; }
}
