// EnderChestSystem.cs - 末影箱系统
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class EnderChestSystem : Node
{
	// 当前打开的末影箱数据
	private EnderChestData _currentChest;
	
	// 管理器引用
	private GameManager _gameManager;
	private DataManager _dataManager;
	private FarmingSystem _farmingSystem;
	private EventBus _eventBus;
	
	// 配置
	private const int MIN_CROPS_PER_CHEST = 3; // 每次最少3个选项
	private const int MAX_CROPS_PER_CHEST = 5; // 每次最多5个选项
	
	public EnderChestData CurrentChest => _currentChest;
	
	public override void _Ready()
	{
		// 获取管理器引用
		_gameManager = GameRoot.Instance?.GameManager;
		_dataManager = GameRoot.Instance?.DataManager;
		_farmingSystem = GameRoot.Instance?.FarmingSystem;
		_eventBus = GameRoot.Instance?.EventBus;
		
		if (_eventBus == null)
		{
			GD.PrintErr("EnderChestSystem: EventBus 未找到");
			return;
		}
		
		// 订阅事件
		SubscribeToEvents();
		
		GD.Print("EnderChestSystem 初始化完成");
	}
	
	private void SubscribeToEvents()
	{
		_eventBus.FloorCompleted += OnFloorCompleted;
	}
	
	/// <summary>
	/// 打开末影箱，生成可选作物列表
	/// </summary>
	public EnderChestData OpenEnderChest(int floorNumber)
	{
		if (_gameManager == null || _gameManager.PlayerData == null)
		{
			GD.PrintErr("EnderChestSystem: GameManager 或 PlayerData 未找到");
			return null;
		}
		
		if (_dataManager == null)
		{
			GD.PrintErr("EnderChestSystem: DataManager 未找到");
			return null;
		}
		
		// 生成可选作物列表
		var availableCrops = GenerateAvailableCrops(floorNumber);
		
		// 为每个作物生成代价
		var cropOptions = new List<EnderChestCropOption>();
		foreach (var crop in availableCrops)
		{
			var cost = GenerateCropCost(crop, floorNumber);
			var option = new EnderChestCropOption
			{
				CropId = crop.Id,
				CropData = crop,
				Cost = cost,
				IsAvailable = CanAffordCost(cost)
			};
			cropOptions.Add(option);
		}
		
		// 创建末影箱数据
		_currentChest = new EnderChestData
		{
			FloorNumber = floorNumber,
			AvailableCrops = cropOptions,
			IsOpened = true,
			OpenTime = DateTime.Now
		};
		
		_eventBus?.EmitEnderChestOpened(_currentChest);
		GD.Print($"末影箱已打开（第{floorNumber}层），提供 {cropOptions.Count} 个作物选项");
		
		return _currentChest;
	}
	
	/// <summary>
	/// 根据层数生成可用作物列表
	/// </summary>
	private List<CropData> GenerateAvailableCrops(int floorNumber)
	{
		var allCrops = _dataManager.GetAllCrops();
		if (allCrops.Count == 0)
		{
			GD.PrintErr("没有可用作物数据");
			return new List<CropData>();
		}
		
		// 过滤掉珍稀作物（除非是特定层数）
		var availableCrops = allCrops
			.Where(c => c.Id != "crop_rare_boss" || floorNumber >= 10) // 10层以上才可能出现珍稀作物
			.ToList();
		
		// 根据层数决定作物数量和质量
		int cropCount = Math.Min(
			MIN_CROPS_PER_CHEST + (floorNumber / 3), // 每3层增加1个选项
			MAX_CROPS_PER_CHEST
		);
		
		// 随机选择作物
		var random = new Random();
		var selectedCrops = new List<CropData>();
		var availableList = new List<CropData>(availableCrops);
		
		for (int i = 0; i < cropCount && availableList.Count > 0; i++)
		{
			int randomIndex = random.Next(availableList.Count);
			selectedCrops.Add(availableList[randomIndex]);
			availableList.RemoveAt(randomIndex);
		}
		
		return selectedCrops;
	}
	
	/// <summary>
	/// 为作物生成代价
	/// </summary>
	private EnderChestCost GenerateCropCost(CropData crop, int floorNumber)
	{
		var random = new Random();
		
		// 根据作物稀有度和层数决定代价
		float baseCost = CalculateBaseCost(crop, floorNumber);
		
		// 随机选择代价类型（70%金币，20%生命，10%卡牌）
		float costTypeRoll = (float)random.NextDouble();
		EnderChestCostType costType;
		float costValue;
		string description;
		
		if (costTypeRoll < 0.7f)
		{
			// 金币代价
			costType = EnderChestCostType.Gold;
			costValue = baseCost;
			description = $"需要支付 {costValue} 金币";
		}
		else if (costTypeRoll < 0.9f)
		{
			// 生命值代价
			costType = EnderChestCostType.Health;
			costValue = Math.Max(5, baseCost * 0.1f); // 生命代价是金币的10%
			description = $"需要消耗 {costValue} 点生命值";
		}
		else
		{
			// 卡牌代价
			costType = EnderChestCostType.Card;
			costValue = 1; // 删除1张卡牌
			description = "需要删除1张卡牌";
		}
		
		return new EnderChestCost
		{
			Type = costType,
			Value = costValue,
			Description = description
		};
	}
	
	/// <summary>
	/// 计算基础代价
	/// </summary>
	private float CalculateBaseCost(CropData crop, int floorNumber)
	{
		// 基础代价根据作物生长时间和效果强度决定
		float timeFactor = crop.GrowthTimeSeconds / 300f; // 以5分钟为基准
		float effectFactor = 1.0f;
		
		if (crop.EffectData != null)
		{
			// 根据效果类型调整
			switch (crop.EffectType)
			{
				case GameEnums.CropEffectType.StatBoost:
					effectFactor = 1.5f; // 属性加成更贵
					break;
				case GameEnums.CropEffectType.CurseTrade:
					effectFactor = 1.2f; // 诅咒权衡中等
					break;
				case GameEnums.CropEffectType.Forget:
					effectFactor = 0.8f; // 遗忘效果较便宜
					break;
			}
		}
		
		// 层数影响（层数越高，代价越高）
		float floorFactor = 1.0f + (floorNumber - 1) * 0.1f;
		
		float baseCost = 20f * timeFactor * effectFactor * floorFactor;
		return Mathf.Round(baseCost);
	}
	
	/// <summary>
	/// 检查玩家是否能支付代价
	/// </summary>
	private bool CanAffordCost(EnderChestCost cost)
	{
		if (_gameManager == null || _gameManager.PlayerData == null)
			return false;
		
		var playerData = _gameManager.PlayerData;
		
		switch (cost.Type)
		{
			case EnderChestCostType.Gold:
				return playerData.Gold >= cost.Value;
				
			case EnderChestCostType.Health:
				return playerData.CurrentHealth > cost.Value; // 至少保留1点生命
				
			case EnderChestCostType.Card:
				return playerData.Deck != null && playerData.Deck.Count > cost.Value;
				
			default:
				return true;
		}
	}
	
	/// <summary>
	/// 选择作物并支付代价
	/// </summary>
	public bool SelectCrop(string cropId)
	{
		if (_currentChest == null || !_currentChest.IsOpened)
		{
			GD.PrintErr("末影箱未打开");
			return false;
		}
		
		var option = _currentChest.AvailableCrops
			.FirstOrDefault(o => o.CropId == cropId);
		
		if (option == null)
		{
			GD.PrintErr($"作物选项不存在: {cropId}");
			return false;
		}
		
		if (!option.IsAvailable)
		{
			GD.PrintErr($"无法支付该作物的代价: {option.Cost.Description}");
			return false;
		}
		
		// 支付代价
		if (!PayCost(option.Cost))
		{
			GD.PrintErr("支付代价失败");
			return false;
		}
		
		// 将作物添加到农场（找到空地块）
		if (_farmingSystem != null)
		{
			var plots = _farmingSystem.Plots;
			int? emptyPlotIndex = null;
			
			foreach (var plot in plots.Values)
			{
				if (!plot.IsOccupied)
				{
					emptyPlotIndex = plot.Index;
					break;
				}
			}
			
			if (emptyPlotIndex.HasValue)
			{
				bool planted = _farmingSystem.TryPlantCrop(emptyPlotIndex.Value, cropId);
				if (planted)
				{
					_eventBus?.EmitCropSelectedFromChest(cropId, (int)option.Cost.Value);
					GD.Print($"成功从末影箱获得作物: {option.CropData.Name}");
					return true;
				}
			}
			else
			{
				GD.PrintErr("没有空余的地块可以种植");
				return false;
			}
		}
		else
		{
			GD.PrintErr("FarmingSystem 未找到");
			return false;
		}
		
		return false;
	}
	
	/// <summary>
	/// 支付代价
	/// </summary>
	private bool PayCost(EnderChestCost cost)
	{
		if (_gameManager == null || _gameManager.PlayerData == null)
			return false;
		
		var playerData = _gameManager.PlayerData;
		
		switch (cost.Type)
		{
			case EnderChestCostType.Gold:
				if (playerData.Gold < cost.Value)
					return false;
				playerData.Gold -= (int)cost.Value;
				GD.Print($"支付 {cost.Value} 金币");
				return true;
				
			case EnderChestCostType.Health:
				int healthCost = (int)cost.Value;
				if (playerData.CurrentHealth <= healthCost)
					return false;
				playerData.CurrentHealth -= healthCost;
				_eventBus?.EmitStatUpdated(GameEnums.PlayerStatType.Health, playerData.CurrentHealth);
				GD.Print($"消耗 {healthCost} 点生命值（当前生命: {playerData.CurrentHealth}）");
				return true;
				
			case EnderChestCostType.Card:
				if (playerData.Deck == null || playerData.Deck.Count < cost.Value)
					return false;
				
				// 随机删除卡牌
				var random = new Random();
				for (int i = 0; i < cost.Value; i++)
				{
					if (playerData.Deck.Count > 0)
					{
						int randomIndex = random.Next(playerData.Deck.Count);
						string removedCard = playerData.Deck[randomIndex];
						playerData.Deck.RemoveAt(randomIndex);
						GD.Print($"删除卡牌: {removedCard}");
					}
				}
				return true;
				
			default:
				return false;
		}
	}
	
	/// <summary>
	/// 关闭末影箱
	/// </summary>
	public void CloseEnderChest()
	{
		if (_currentChest != null)
		{
			_currentChest.IsOpened = false;
			GD.Print("末影箱已关闭");
		}
	}
	
	/// <summary>
	/// 获取可用作物列表（根据层数）
	/// </summary>
	public List<CropData> GetAvailableCrops(int floorNumber)
	{
		return GenerateAvailableCrops(floorNumber);
	}
	
	// 事件处理
	private void OnFloorCompleted(int floorNumber)
	{
		// 每层完成时自动打开末影箱
		OpenEnderChest(floorNumber);
	}
}
