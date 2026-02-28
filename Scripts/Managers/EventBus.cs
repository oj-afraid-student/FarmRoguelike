// EventBus.cs
using System;
using System.Collections.Generic;
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
	public event Action<string> BossDefeated; // enemyId
	public event Action<bool> CombatEnded; // playerWon
	
	// 农场事件
	public event Action<string, int> CropPlanted; // cropId, plotIndex
	public event Action<string, int, float> CropGrowthUpdated; // cropId, plotIndex, progress
	public event Action<string, int, CropReward> CropHarvested; // cropId, plotIndex, reward
	public event Action<string> CropWithered; // cropId
	
	// 作物效果事件
	public event Action<string> CropEffectApplied; // cropId
	public event Action<string> CropEffectRemoved; // cropId
	
	// 地图事件
	public event Action<RoomData> RoomEntered;
	public event Action<int> FloorCompleted; // floorNumber
	public event Action<RewardData> RewardSelected;
	public event Action<string> NotificationRequested; // message
	public event Action<string> CenterPopupRequested; // center screen message
	
	// 末影箱事件
	public event Action<EnderChestData> EnderChestOpened; // 末影箱打开
	public event Action<string, float> CropSelectedFromChest; // cropId, costValue
    public event Action EnderChestClosed; // 末影箱关闭
	
	// 战斗物资事件
	public event Action<Dictionary<string, int>> CombatResourcesGenerated; // 战斗物资生成
	public event Action<int, string> ResourceAppliedToCrop; // plotIndex, resourceId
	
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

	public void EmitBossDefeated(string enemyId)
	{
		BossDefeated?.Invoke(enemyId);
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
	
	public void EmitNotificationRequested(string message)
	{
		NotificationRequested?.Invoke(message);
	}
	
	public void EmitCenterPopupRequested(string message)
	{
		CenterPopupRequested?.Invoke(message);
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

	public void EmitCropEffectApplied(string cropId)
	{
		CropEffectApplied?.Invoke(cropId);
	}

	public void EmitCropEffectRemoved(string cropId)
	{
		CropEffectRemoved?.Invoke(cropId);
	}

	public void EmitEnderChestOpened(EnderChestData chestData)
	{
		EnderChestOpened?.Invoke(chestData);
	}

	public void EmitCropSelectedFromChest(string cropId, float costValue)
	{
		CropSelectedFromChest?.Invoke(cropId, costValue);
	}

	public void EmitEnderChestClosed()
	{
		EnderChestClosed?.Invoke();
	}

	public void EmitCombatEnded(bool playerWon)
	{
		CombatEnded?.Invoke(playerWon);
	}

	public void EmitCombatResourcesGenerated(Dictionary<string, int> resources)
	{
		CombatResourcesGenerated?.Invoke(resources);
	}

	public void EmitResourceAppliedToCrop(int plotIndex, string resourceId)
	{
		ResourceAppliedToCrop?.Invoke(plotIndex, resourceId);
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
		// Safe default: notify that a combat room was entered. GameManager is already subscribed to RoomEntered
		NotificationRequested?.Invoke($"进入战斗房间: {room.Id}");
	}

	public void StartBossRoom(RoomData room)
	{
		// Safe default: notify that a boss room was entered
		NotificationRequested?.Invoke($"进入Boss房间: {room.Id}");
	}

	public void StartRewardRoom(RoomData room)
	{
		// Safe default: notify that a reward room was entered
		NotificationRequested?.Invoke($"进入奖励房间: {room.Id}");
	}

	public void StartTrapRoom(RoomData room)
	{
		// Safe default: notify that a trap room was entered
		NotificationRequested?.Invoke($"进入陷阱房间: {room.Id}");
	}

	
	
}
