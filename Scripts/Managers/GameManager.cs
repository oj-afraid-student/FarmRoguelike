using System;
using System.Collections.Generic;
using Godot;

public partial class GameManager : Node
{
    // 公开属性
    public GameEnums.GameState CurrentState { get; private set; } = GameEnums.GameState.MainMenu;
    public PlayerData PlayerData { get; private set; }
    public int CurrentFloor { get; private set; } = 1;
    public bool IsPaused { get; private set; } = false;
    
    /// <summary>
    /// 设置当前层数（用于加载存档）
    /// </summary>
    public void SetCurrentFloor(int floor)
    {
        CurrentFloor = floor;
    }
    
    /// <summary>
    /// 恢复玩家数据（用于加载存档）
    /// </summary>
    public void RestorePlayerData(PlayerData playerData)
    {
        if (playerData == null)
        {
            InitializePlayerData();
            return;
        }
        
        // 直接更新现有PlayerData的属性，而不是替换整个对象
        PlayerData.MaxHealth = playerData.MaxHealth;
        PlayerData.CurrentHealth = playerData.CurrentHealth;
        PlayerData.Attack = playerData.Attack;
        PlayerData.Defense = playerData.Defense;
        PlayerData.ActionPoints = playerData.ActionPoints;
        PlayerData.Speed = playerData.Speed;
        PlayerData.Luck = playerData.Luck;
        PlayerData.Gold = playerData.Gold;
        
        // 恢复集合类型
        PlayerData.Deck = new List<string>(playerData.Deck ?? new List<string>());
        PlayerData.EquippedCards = new List<string>(playerData.EquippedCards ?? new List<string>());
        PlayerData.Inventory = new Dictionary<string, int>(playerData.Inventory ?? new Dictionary<string, int>());
        PlayerData.Crops = new Dictionary<int, CropPlotData>(playerData.Crops ?? new Dictionary<int, CropPlotData>());
        PlayerData.PermanentUpgrades = new Dictionary<string, float>(playerData.PermanentUpgrades ?? new Dictionary<string, float>());
    }
    
    // 私有字段
    private DataManager _dataManager;
    
    public override void _Ready()
    {
        InitializePlayerData();
        InitializeDataManager();
        SubscribeToEvents();
        
        GD.Print("GameManager 初始化完成");
    }
    
    private void InitializePlayerData()
    {
        PlayerData = new PlayerData();
        
        // 初始卡组
        PlayerData.Deck = new List<string>
        {
            "card_attack_basic",
            "card_attack_basic",
            "card_attack_basic",
            "card_defend_basic",
            "card_defend_basic",
            "card_skill_weakness"
        };
    }
    
    private void InitializeDataManager()
    {
        // 尝试获取DataManager
        _dataManager = GetNode<DataManager>("/root/DataManager");
        if (_dataManager == null)
        {
            GD.Print("DataManager 未找到，创建新实例");
            _dataManager = new DataManager();
            GetTree().Root.AddChild(_dataManager);
            _dataManager.Name = "DataManager";
        }
    }
    
    private void SubscribeToEvents()
    {
        var eventBus = EventBus.Instance;
        if (eventBus == null) return;
        
        eventBus.PlayerDamaged += OnPlayerDamaged;
        eventBus.EnemyDefeated += OnEnemyDefeated;
        eventBus.CropHarvested += OnCropHarvested;
        eventBus.RewardSelected += OnRewardSelected;
        eventBus.StatUpdated += OnStatUpdated;
        eventBus.CropEffectApplied += OnCropEffectApplied;
        eventBus.CropEffectRemoved += OnCropEffectRemoved;
    }
    
    // 公共方法
    public void StartNewGame()
    {
        InitializePlayerData();
        CurrentFloor = 1;
        ChangeState(GameEnums.GameState.MapExploration);
        EventBus.Instance.EmitGameStarted();
        GD.Print("新游戏开始");
    }
    
    public void ChangeState(GameEnums.GameState newState)
    {
        if (CurrentState == newState) return;
        
        CurrentState = newState;
        EventBus.Instance.EmitGameStateChanged(newState);
        
        GD.Print($"游戏状态改变: {newState}");
        
        // 状态特定的逻辑
        switch (newState)
        {
            case GameEnums.GameState.MainMenu:
                // 主菜单逻辑
                break;
            case GameEnums.GameState.Combat:
                // 战斗初始化
                break;
        }
    }
    
    public void TogglePause()
    {
        IsPaused = !IsPaused;
        GetTree().Paused = IsPaused;
        
        if (IsPaused)
        {
            EventBus.Instance.EmitGamePaused();
            GD.Print("游戏暂停");
        }
        else
        {
            EventBus.Instance.EmitGameResumed();
            GD.Print("游戏继续");
        }
    }
    
    public void ApplyDamageToPlayer(int damage)
    {
        int actualDamage = Math.Max(1, damage - PlayerData.Defense);
        PlayerData.CurrentHealth -= actualDamage;
        
        EventBus.Instance.EmitPlayerDamaged(actualDamage);
        
        if (PlayerData.CurrentHealth <= 0)
        {
            GameOver(false);
        }
    }
    public void ApplyDamageToEnemy(EnemyData enemy, int damage)
	{
		if (enemy == null) return;
		
		int actualDamage = Math.Max(1, damage - enemy.Defense);
		enemy.Health -= actualDamage;
		
		EventBus.Instance.EmitEnemyDamaged(enemy.Id, actualDamage);
		
		if (enemy.Health <= 0)
		{
			EventBus.Instance.EmitEnemyDefeated(enemy.Id);
		}
	}

    public void HealPlayer(int amount)
    {
        PlayerData.CurrentHealth = Math.Min(
            PlayerData.CurrentHealth + amount,
            PlayerData.MaxHealth
        );
        
        EventBus.Instance.EmitStatUpdated(GameEnums.PlayerStatType.Health, PlayerData.CurrentHealth);
    }
    
    public void AddCardToDeck(string cardId)
    {
        if (!PlayerData.Deck.Contains(cardId))
        {
            PlayerData.Deck.Add(cardId);
            EventBus.Instance.EmitCardAddedToDeck(cardId);
        }
    }
    
    public void UpdateStat(GameEnums.PlayerStatType statType, float value)
    {
        PlayerData.SetStat(statType, value);
        EventBus.Instance.EmitStatUpdated(statType, value);
    }
    
    private void GameOver(bool isVictory)
    {
        ChangeState(GameEnums.GameState.GameOver);
        EventBus.Instance.EmitGameEnded(isVictory);
        
        GD.Print($"游戏结束 - 胜利: {isVictory}");
    }
    
    // 事件处理
    private void OnPlayerDamaged(int damage)
    {
        GD.Print($"玩家受到 {damage} 点伤害，剩余生命: {PlayerData.CurrentHealth}");
    }
    
    private void OnEnemyDefeated(string enemyId)
    {
        // 给予金币奖励
        PlayerData.Gold += 10;
        GD.Print($"击败敌人 {enemyId}，获得10金币，当前金币: {PlayerData.Gold}");
    }
    
    private void OnCropHarvested(string cropId, int plotIndex, CropReward reward)
    {
        ApplyCropReward(reward);
    }
    
    private void OnRewardSelected(RewardData reward)
    {
        // 应用选择的奖励
        switch (reward.Type)
        {
            case GameEnums.RewardType.StatIncrease:
                UpdateStat(reward.StatType, PlayerData.GetStat(reward.StatType) + reward.Value);
                break;
            case GameEnums.RewardType.CardReward:
                AddCardToDeck(reward.CardId);
                break;
            case GameEnums.RewardType.GoldReward:
                PlayerData.Gold += (int)reward.Value;
                break;
        }
    }
    
    private void OnStatUpdated(GameEnums.PlayerStatType statType, float value)
    {
        GD.Print($"属性更新: {statType} = {value}");
    }
    
    private void ApplyCropReward(CropReward reward)
    {
        if (reward.HealthRestore > 0)
        {
            HealPlayer(reward.HealthRestore);
        }
        
        if (reward.Gold > 0)
        {
            PlayerData.Gold += reward.Gold;
        }
        
        if (reward.StatModifiers != null)
        {
            foreach (var modifier in reward.StatModifiers)
            {
                // 这里简化处理，实际需要将string转换为PlayerStatType
                GD.Print($"作物提供属性修改: {modifier.Key} * {modifier.Value}");
            }
        }
    }
    
    /// <summary>
    /// 应用作物效果（供UI或其他系统调用）
    /// </summary>
    public bool ApplyCropEffect(string cropId)
    {
        var cropEffectSystem = GameRoot.Instance?.CropEffectSystem;
        if (cropEffectSystem == null)
        {
            GD.PrintErr("CropEffectSystem 未找到");
            return false;
        }
        
        return cropEffectSystem.ApplyCropEffect(cropId);
    }
    
    /// <summary>
    /// 移除作物效果
    /// </summary>
    public bool RemoveCropEffect(string cropId)
    {
        var cropEffectSystem = GameRoot.Instance?.CropEffectSystem;
        if (cropEffectSystem == null)
        {
            GD.PrintErr("CropEffectSystem 未找到");
            return false;
        }
        
        return cropEffectSystem.RemoveCropEffect(cropId);
    }
    
    /// <summary>
    /// 获取所有激活的作物效果
    /// </summary>
    public List<ActiveCropEffect> GetActiveCropEffects()
    {
        var cropEffectSystem = GameRoot.Instance?.CropEffectSystem;
        if (cropEffectSystem == null)
        {
            return new List<ActiveCropEffect>();
        }
        
        return cropEffectSystem.GetActiveEffects();
    }
    
    /// <summary>
    /// 检查作物效果是否已激活
    /// </summary>
    public bool IsCropEffectActive(string cropId)
    {
        var cropEffectSystem = GameRoot.Instance?.CropEffectSystem;
        if (cropEffectSystem == null)
        {
            return false;
        }
        
        return cropEffectSystem.IsEffectActive(cropId);
    }
    
    // 作物效果事件处理
    private void OnCropEffectApplied(string cropId)
    {
        GD.Print($"作物效果已应用: {cropId}");
    }
    
    private void OnCropEffectRemoved(string cropId)
    {
        GD.Print($"作物效果已移除: {cropId}");
    }
    
    /// <summary>
    /// 打开末影箱（供UI调用）
    /// </summary>
    public EnderChestData OpenEnderChest(int floorNumber)
    {
        var enderChestSystem = GameRoot.Instance?.EnderChestSystem;
        if (enderChestSystem == null)
        {
            GD.PrintErr("EnderChestSystem 未找到");
            return null;
        }
        
        return enderChestSystem.OpenEnderChest(floorNumber);
    }
    
    /// <summary>
    /// 从末影箱选择作物
    /// </summary>
    public bool SelectCropFromEnderChest(string cropId)
    {
        var enderChestSystem = GameRoot.Instance?.EnderChestSystem;
        if (enderChestSystem == null)
        {
            GD.PrintErr("EnderChestSystem 未找到");
            return false;
        }
        
        return enderChestSystem.SelectCrop(cropId);
    }
    
    /// <summary>
    /// 获取当前末影箱数据
    /// </summary>
    public EnderChestData GetCurrentEnderChest()
    {
        var enderChestSystem = GameRoot.Instance?.EnderChestSystem;
        if (enderChestSystem == null)
        {
            return null;
        }
        
        return enderChestSystem.CurrentChest;
    }
    
    /// <summary>
    /// 使用物资到作物
    /// </summary>
    public bool UseResourceOnCrop(string resourceId, int plotIndex)
    {
        var resourceSystem = GameRoot.Instance?.CombatResourceSystem;
        if (resourceSystem == null)
        {
            GD.PrintErr("CombatResourceSystem 未找到");
            return false;
        }
        
        return resourceSystem.UseResource(resourceId, plotIndex);
    }
    
    /// <summary>
    /// 获取玩家物资数量
    /// </summary>
    public int GetResourceCount(string resourceId)
    {
        var resourceSystem = GameRoot.Instance?.CombatResourceSystem;
        if (resourceSystem == null)
        {
            return 0;
        }
        
        return resourceSystem.GetResourceCount(resourceId);
    }
    
    /// <summary>
    /// 获取所有玩家物资
    /// </summary>
    public Dictionary<string, int> GetPlayerResources()
    {
        var resourceSystem = GameRoot.Instance?.CombatResourceSystem;
        if (resourceSystem == null)
        {
            return new Dictionary<string, int>();
        }
        
        return resourceSystem.GetPlayerResources();
    }
}