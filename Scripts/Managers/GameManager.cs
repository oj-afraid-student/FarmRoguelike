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
        PlayerData.Energy = playerData.Energy;
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

        // 保持在主菜单状态，延迟发送当前状态事件以便 UIManager 正确显示
        CallDeferred(nameof(EmitInitialState));
    }

    private void EmitInitialState()
    {
        EventBus.Instance?.EmitGameStateChanged(CurrentState);
    }
    
    private void InitializePlayerData()
    {
        PlayerData = new PlayerData();
        
        // 初始卡组
        PlayerData.Deck = new List<string>
        {
            "card_scythe_slash",
            "card_hoe_smash",
            "card_raise_tools",
            "card_urgent_bandage",
            "card_spray_pesticide",
            "card_observe_weakness"
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
        eventBus.CombatEnded += OnCombatEnded;
        eventBus.CropHarvested += OnCropHarvested;
        eventBus.RewardSelected += OnRewardSelected;
        eventBus.StatUpdated += OnStatUpdated;
        eventBus.CropEffectApplied += OnCropEffectApplied;
        eventBus.CropEffectRemoved += OnCropEffectRemoved;
        eventBus.RoomEntered += OnRoomEntered;
    }
    
    // 公共方法
    public void StartNewGame()
    {
        // 标准新游戏流程：初始化数据、生成地图并进入地图探索
        InitializePlayerData();
        CurrentFloor = 1;
        ChangeState(GameEnums.GameState.MapExploration);
        GameRoot.Instance?.MapSystem?.GenerateFloor(CurrentFloor);   
        EventBus.Instance.EmitGameStarted();
        GD.Print("新游戏开始");      
    }

    /// <summary>
    /// 为仅进入农场的测试或试玩初始化新游戏（不直接进入地图）。
    /// </summary>
    public void InitializeNewGameForFarm()
    {
        InitializePlayerData();
        CurrentFloor = 1;
        // 不生成地图，直接进入耕作状态
        ChangeState(GameEnums.GameState.Farming);
        EventBus.Instance.EmitGameStarted();
        GD.Print("新游戏（仅农场）初始化完成");
    }

    public void LoadMainMenu()
    {
        InitializePlayerData();
        ChangeState(GameEnums.GameState.MainMenu);
        EventBus.Instance.EmitGameStarted();
    }


    public void ChangeState(GameEnums.GameState newState)
    {
        if (CurrentState == newState) return;
        
        CurrentState = newState;
        EventBus.Instance.EmitGameStateChanged(newState);
        
        GD.Print($"游戏状态改变: {newState}");
        
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
    private void OnRoomEntered(RoomData room)
    {
        try
        {
        if (room.IsCleared)
        {
            string msg = $"[系统] 你来到了已经清理过的 {room.Type} 房间。";
            GD.Print(msg);
            EventBus.Instance?.EmitNotificationRequested(msg);
            // 已经是 MapExploration 状态，UI 会自动高亮当前块
            return;
        }

        switch (room.Type)
        {
            case GameEnums.RoomType.Reward:
                int goldReward = 20;
                PlayerData.Gold += goldReward;
                string rewardMsg = $"[系统] 你发现了一个宝箱！获得了 {goldReward} 金币。当前金币: {PlayerData.Gold}";
                GD.Print(rewardMsg);
                EventBus.Instance?.EmitNotificationRequested(rewardMsg);
                EventBus.Instance?.EmitCenterPopupRequested($"宝箱房间\n获得了 {goldReward} 金币！");
                
                GameRoot.Instance?.MapSystem?.CompleteCurrentRoom();
                // 仅通知 UI 刷新地图网格即可
                EventBus.Instance?.EmitMapVisualsUpdateRequested();
                break;
                
            case GameEnums.RoomType.Trap:
                int damage = 10;
                int goldLoss = 5;
                
                ApplyDamageToPlayer(damage);
                PlayerData.Gold = Math.Max(0, PlayerData.Gold - goldLoss);
                
                string trapMsg = $"[系统] 踩中陷阱！损失 {damage} 生命和 {goldLoss} 金币。剩余生命: {PlayerData.CurrentHealth}";
                GD.Print(trapMsg);
                
                EventBus.Instance?.EmitNotificationRequested(trapMsg);
                EventBus.Instance?.EmitCenterPopupRequested($"陷阱房间\n损失了 {damage} 生命和 {goldLoss} 金币！");
                
                GameRoot.Instance?.MapSystem?.CompleteCurrentRoom();
                if (PlayerData.CurrentHealth > 0)
                {
                    // 仅通知 UI 刷新地图网格即可，避免触发 ClearAllUI
                    EventBus.Instance?.EmitMapVisualsUpdateRequested();
                }
                break;
                
            case GameEnums.RoomType.Combat:
                var normalEnemy = _dataManager?.GetRandomNormalEnemy();
                if (normalEnemy != null)
                {
                    string msg = $"[系统] 你遭遇了敌人 {normalEnemy.Name}！准备战斗。";
                    GD.Print(msg);
                    EventBus.Instance?.EmitNotificationRequested(msg);
                    EventBus.Instance?.EmitCombatStarted(normalEnemy.Id);
                    ChangeState(GameEnums.GameState.Combat);
                }
                else
                {
                    GD.PrintErr("没有找到普通敌人，跳过战斗");
                    ChangeState(GameEnums.GameState.MapExploration);
                }
                break;
                
            case GameEnums.RoomType.Boss:
                var bossEnemy = _dataManager?.GetRandomBossEnemy();
                if (bossEnemy != null)
                {
                    string msg = $"[系统] 你遭遇了强大的Boss {bossEnemy.Name}！";
                    GD.Print(msg);
                    EventBus.Instance?.EmitNotificationRequested(msg);
                    EventBus.Instance?.EmitCombatStarted(bossEnemy.Id);
                    ChangeState(GameEnums.GameState.Combat);
                }
                else
                {
                    GD.PrintErr("没有找到Boss敌人，跳过战斗");
                    ChangeState(GameEnums.GameState.MapExploration);
                }
                break;
        }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[Error] GameManager.OnRoomEntered 处理异常: {ex}");
        }
    }
    private void OnPlayerDamaged(int damage)
    {
        GD.Print($"玩家受到 {damage} 点伤害，剩余生命: {PlayerData.CurrentHealth}");
    }
    
    private void OnEnemyDefeated(string enemyId)
    {
        // 战斗系统已经负责发放奖励（读取怪物配置和给予金币/经验），这里可以处理后续流程
        GD.Print($"GameManager 检测到击败敌人 {enemyId}");

        // 如果是 Boss，发出专门事件以便 UI 提供返回农场的选项
        var dm = GetNodeOrNull<DataManager>("/root/DataManager");
        if (dm != null && dm.IsBossEnemy(enemyId))
        {
            EventBus.Instance?.EmitBossDefeated(enemyId);
        }
    }
    
    private void OnCombatEnded(bool playerWon)
    {
        if (playerWon)
        {
            string msg = "[系统] 战斗胜利！";
            GD.Print(msg);
            EventBus.Instance?.EmitNotificationRequested(msg);
            
            GameRoot.Instance?.MapSystem?.CompleteCurrentRoom();
            
            // 如果完成了房间（比如Boss），系统可能抛出了FloorCompleted，UIManager会自动处理楼层结束界面。
            // 否则退回地图
            if (CurrentState == GameEnums.GameState.Combat)
            {
                ChangeState(GameEnums.GameState.MapExploration);
            }
        }
        else
        {
            // 如果玩家输了而且尚未GameOver（正常在生命归0时就GameOver了），我们兜底判断一次
            if (CurrentState != GameEnums.GameState.GameOver)
            {
                GameOver(false);
            }
        }
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