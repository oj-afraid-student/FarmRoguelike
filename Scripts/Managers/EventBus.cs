// EventBus.cs
using System;
using Godot;

public partial class EventBus : Node
{
    // 单例实例
    public static EventBus Instance { get; private set; }
    
    // 游戏状态事件
    public event Action<GameEnums.GameState> GameStateChanged;
    public event Action GameStarted;
    public event Action GamePaused;
    public event Action GameResumed;
    public event Action<bool> GameEnded; // bool:是否胜利
    
    // 战斗事件
    public event Action<string> CombatStarted; // enemyId
    public event Action<string, string> CardPlayed; // cardId, targetId
    public event Action TurnEnded;
    public event Action PlayerTurnStarted;
    public event Action<int> PlayerDamaged; // damage
    public event Action<string, int> EnemyDamaged; // enemyId, damage
    public event Action<string> EnemyDefeated; // enemyId
    
    // 农场事件
    public event Action<string, int> CropPlanted; // cropId, plotIndex
    public event Action<string, int, float> CropGrowthUpdated; // cropId, plotIndex, progress
    public event Action<string, int, CropReward> CropHarvested; // cropId, plotIndex, reward
    public event Action<string> CropWithered; // cropId
    
    // 地图事件
    public event Action<RoomData> RoomEntered;
    public event Action<int> FloorCompleted; // floorNumber
    public event Action<RewardData> RewardSelected;
    
    // 玩家事件
    public event Action<GameEnums.PlayerStatType, float> StatUpdated;
    public event Action<string> ItemCollected; // itemId
    public event Action<string> CardAddedToDeck; // cardId
    
    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            GD.Print("EventBus 初始化完成");
        }
        else
        {
            QueueFree();
        }
    }
    
    // 安全的触发事件方法
    public void EmitGameStateChanged(GameEnums.GameState newState)
    {
        GameStateChanged?.Invoke(newState);
    }
    
    public void EmitGameStarted()
    {
        GameStarted?.Invoke();
    }
    
    public void EmitCombatStarted(string enemyId)
    {
        CombatStarted?.Invoke(enemyId);
    }
    
    public void EmitCardPlayed(string cardId, string targetId = "")
    {
        CardPlayed?.Invoke(cardId, targetId);
    }
    
    public void EmitPlayerDamaged(int damage)
    {
        PlayerDamaged?.Invoke(damage);
    }
    
    public void EmitEnemyDamaged(string enemyId, int damage)
    {
        EnemyDamaged?.Invoke(enemyId, damage);
    }
    
    public void EmitEnemyDefeated(string enemyId)
    {
        EnemyDefeated?.Invoke(enemyId);
    }
    
    public void EmitCropPlanted(string cropId, int plotIndex)
    {
        CropPlanted?.Invoke(cropId, plotIndex);
    }
    
    public void EmitCropHarvested(string cropId, int plotIndex, CropReward reward)
    {
        CropHarvested?.Invoke(cropId, plotIndex, reward);
    }
    
    public void EmitRoomEntered(RoomData room)
    {
        RoomEntered?.Invoke(room);
    }
    
    public void EmitFloorCompleted(int floorNumber)
    {
        FloorCompleted?.Invoke(floorNumber);
    }
    
    public void EmitRewardSelected(RewardData reward)
    {
        RewardSelected?.Invoke(reward);
    }
    
    public void EmitStatUpdated(GameEnums.PlayerStatType statType, float value)
    {
        StatUpdated?.Invoke(statType, value);
    }
    
    public void EmitCardAddedToDeck(string cardId)
    {
        CardAddedToDeck?.Invoke(cardId);
    }

	public void EmitGamePaused()
	{
		GamePaused?.Invoke();
	}

	public void EmitGameResumed()
	{
		GameResumed?.Invoke();
	}

	public void EmitGameEnded(bool isVictory)
	{
		GameEnded?.Invoke(isVictory);
	}

	public void EmitCropGrowthUpdated(string cropId, int plotIndex, float progress)
	{
		CropGrowthUpdated?.Invoke(cropId, plotIndex, progress);
	}

	public void EmitPlayerTurnStarted()
	{
		PlayerTurnStarted?.Invoke();
	}

	public void EmitTurnEnded()
	{
		TurnEnded?.Invoke();
	}

	public void StartCombatRoom(RoomData room)
	{
		throw new NotImplementedException();
	}

	public void StartEventRoom(RoomData room)
	{
		throw new NotImplementedException();
	}

	public void StartShopRoom(RoomData room)
	{
		throw new NotImplementedException();
	}

	public void StartRestRoom(RoomData room)
	{
		throw new NotImplementedException();
	}

	public void StartBossRoom(RoomData room)
	{
		throw new NotImplementedException();
	}

	public void StartFarmingRoom(RoomData room)
	{
		throw new NotImplementedException();
	}

	
	
}