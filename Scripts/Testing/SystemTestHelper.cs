// SystemTestHelper.cs - 系统测试辅助类
// 用于测试作物系统、末影箱系统和战斗物资系统的功能
using Godot;
using System;
using System.Collections.Generic;

public partial class SystemTestHelper : Node
{
	private GameManager _gameManager;
	private DataManager _dataManager;
	private FarmingSystem _farmingSystem;
	private CropEffectSystem _cropEffectSystem;
	private EnderChestSystem _enderChestSystem;
	private CombatResourceSystem _resourceSystem;
	
	public override void _Ready()
	{
		// 获取所有管理器
		_gameManager = GameRoot.Instance?.GameManager;
		_dataManager = GameRoot.Instance?.DataManager;
		_farmingSystem = GameRoot.Instance?.FarmingSystem;
		_cropEffectSystem = GameRoot.Instance?.CropEffectSystem;
		_enderChestSystem = GameRoot.Instance?.EnderChestSystem;
		_resourceSystem = GameRoot.Instance?.CombatResourceSystem;
		
		GD.Print("SystemTestHelper 初始化完成");
		GD.Print("使用以下方法测试系统功能：");
		GD.Print("- TestPlantCrop() - 测试种植作物");
		GD.Print("- TestHarvestCrop() - 测试收获作物");
		GD.Print("- TestCropEffect() - 测试作物效果");
		GD.Print("- TestEnderChest() - 测试末影箱");
		GD.Print("- TestCombatResources() - 测试战斗物资");
		GD.Print("- TestCombatScene() - 触发验证战斗场景（需切换状态）");
	}
	
	/// <summary>
	/// 测试种植作物
	/// </summary>
	public void TestPlantCrop()
	{
		if (_farmingSystem == null || _dataManager == null)
		{
			GD.PrintErr("系统未初始化");
			return;
		}
		
		GD.Print("=== 测试种植作物 ===");
		
		// 获取可用作物
		var crops = _dataManager.GetAllCrops();
		if (crops.Count == 0)
		{
			GD.PrintErr("没有可用作物");
			return;
		}
		
		// 尝试种植到地块0
		string cropId = crops[0].Id;
		bool success = _farmingSystem.TryPlantCrop(0, cropId);
		
		if (success)
		{
			GD.Print($"✓ 成功种植 {crops[0].Name} 到地块0");
		}
		else
		{
			GD.PrintErr($"✗ 种植失败: {crops[0].Name}");
		}
	}
	
	/// <summary>
	/// 测试收获作物（需要先种植并等待成熟）
	/// </summary>
	public void TestHarvestCrop()
	{
		if (_farmingSystem == null)
		{
			GD.PrintErr("系统未初始化");
			return;
		}
		
		GD.Print("=== 测试收获作物 ===");
		
		var plots = _farmingSystem.Plots;
		foreach (var plot in plots.Values)
		{
			if (plot.IsOccupied && plot.IsReady)
			{
				var reward = _farmingSystem.TryHarvestCrop(plot.Index);
				if (reward != null)
				{
					GD.Print($"✓ 成功从地块{plot.Index}收获作物");
					GD.Print($"  奖励: 生命+{reward.HealthRestore}, 金币+{reward.Gold}");
					return;
				}
			}
		}
		
		GD.Print("没有可收获的作物（需要先种植并等待成熟）");
	}
	
	/// <summary>
	/// 测试作物效果
	/// </summary>
	public void TestCropEffect()
	{
		if (_cropEffectSystem == null || _gameManager == null)
		{
			GD.PrintErr("系统未初始化");
			return;
		}
		
		GD.Print("=== 测试作物效果 ===");
		
		// 测试应用生命加成效果
		bool success = _gameManager.ApplyCropEffect("crop_health_boost");
		
		if (success)
		{
			GD.Print("✓ 成功应用生命之果效果");
			GD.Print($"  当前最大生命: {_gameManager.PlayerData.MaxHealth}");
			
			// 查看激活的效果
			var activeEffects = _gameManager.GetActiveCropEffects();
			GD.Print($"  激活效果数量: {activeEffects.Count}");
		}
		else
		{
			GD.PrintErr("✗ 应用效果失败");
		}
	}
	
	/// <summary>
	/// 测试末影箱
	/// </summary>
	public void TestEnderChest()
	{
		if (_enderChestSystem == null || _gameManager == null)
		{
			GD.PrintErr("系统未初始化");
			return;
		}
		
		GD.Print("=== 测试末影箱 ===");
		
		// 打开末影箱（第1层）
		var chestData = _gameManager.OpenEnderChest(1);
		
		if (chestData != null)
		{
			GD.Print($"✓ 末影箱已打开，提供 {chestData.AvailableCrops.Count} 个选项");
			
			foreach (var option in chestData.AvailableCrops)
			{
				GD.Print($"  选项: {option.CropData.Name}");
				GD.Print($"    代价: {option.Cost.Description}");
				GD.Print($"    可用: {option.IsAvailable}");
			}
		}
		else
		{
			GD.PrintErr("✗ 打开末影箱失败");
		}
	}
	
	/// <summary>
	/// 测试战斗物资
	/// </summary>
	public void TestCombatResources()
	{
		if (_resourceSystem == null || _gameManager == null)
		{
			GD.PrintErr("系统未初始化");
			return;
		}
		
		GD.Print("=== 测试战斗物资 ===");
		
		// 模拟战斗胜利，生成物资
		var resources = _resourceSystem.GenerateCombatResources(true, "test_enemy");
		
		if (resources.Count > 0)
		{
			GD.Print($"✓ 战斗胜利，生成 {resources.Count} 种物资:");
			foreach (var resource in resources)
			{
				var resourceData = _resourceSystem.GetResourceData(resource.Key);
				if (resourceData != null)
				{
					GD.Print($"  {resourceData.Name} x{resource.Value}");
				}
			}
			
			// 显示玩家拥有的所有物资
			var allResources = _gameManager.GetPlayerResources();
			GD.Print($"  玩家物资总数: {allResources.Count} 种");
		}
		else
		{
			GD.Print("没有生成物资（可能是概率问题）");
		}
	}
	
	/// <summary>
	/// 测试使用物资加速作物
	/// </summary>
	public void TestUseResourceOnCrop()
	{
		if (_resourceSystem == null || _farmingSystem == null || _gameManager == null)
		{
			GD.PrintErr("系统未初始化");
			return;
		}
		
		GD.Print("=== 测试使用物资 ===");
		
		// 检查是否有物资
		var resources = _gameManager.GetPlayerResources();
		if (resources.Count == 0)
		{
			GD.Print("没有可用物资，先运行 TestCombatResources()");
			return;
		}
		
		// 检查是否有种植的作物
		var plots = _farmingSystem.Plots;
		int? plotWithCrop = null;
		foreach (var plot in plots.Values)
		{
			if (plot.IsOccupied && !plot.IsReady)
			{
				plotWithCrop = plot.Index;
				break;
			}
		}
		
		if (!plotWithCrop.HasValue)
		{
			GD.Print("没有正在生长的作物，先运行 TestPlantCrop()");
			return;
		}
		
		// 使用第一个可用物资
		string firstResourceId = "";
		foreach (var resource in resources)
		{
			firstResourceId = resource.Key;
			break;
		}
		
		if (!string.IsNullOrEmpty(firstResourceId))
		{
			bool success = _gameManager.UseResourceOnCrop(firstResourceId, plotWithCrop.Value);
			if (success)
			{
				var resourceData = _resourceSystem.GetResourceData(firstResourceId);
				GD.Print($"✓ 成功对地块{plotWithCrop.Value}使用 {resourceData?.Name}");
			}
			else
			{
				GD.PrintErr("✗ 使用物资失败");
			}
		}
	}
	
	/// <summary>
	/// 测试战斗系统与界面 (使用 enemy_sample_boss 数据)
	/// </summary>
	public void TestCombatScene()
	{
		if (_gameManager == null)
		{
			GD.PrintErr("GameManager未初始化");
			return;
		}

		var combatSystem = GameRoot.Instance?.CombatSystem;
		if (combatSystem == null)
		{
			GD.PrintErr("系统中未找到CombatSystem实例");
			return;
		}

	   
	}

	/// <summary>
	/// 运行所有测试
	/// </summary>
	public void RunAllTests()
	{
		GD.Print("========== 开始运行所有测试 ==========");
		
		TestPlantCrop();
		TestCropEffect();
		TestEnderChest();
		TestCombatResources();
		TestUseResourceOnCrop();
		
		GD.Print("========== 测试完成 ==========");
	}
	
	/// <summary>
	/// 显示系统状态
	/// </summary>
	public void ShowSystemStatus()
	{
		GD.Print("========== 系统状态 ==========");
		
		if (_gameManager != null && _gameManager.PlayerData != null)
		{
			var player = _gameManager.PlayerData;
			GD.Print($"玩家生命: {player.CurrentHealth}/{player.MaxHealth}");
			GD.Print($"玩家金币: {player.Gold}");
			GD.Print($"玩家攻击: {player.Attack}");
			GD.Print($"玩家防御: {player.Defense}");
		}
		
		if (_farmingSystem != null)
		{
			var plots = _farmingSystem.Plots;
			int occupiedCount = 0;
			int readyCount = 0;
			foreach (var plot in plots.Values)
			{
				if (plot.IsOccupied) occupiedCount++;
				if (plot.IsReady) readyCount++;
			}
			GD.Print($"农场状态: {occupiedCount}/6 地块已种植, {readyCount} 个可收获");
		}
		
		if (_cropEffectSystem != null)
		{
			var effects = _cropEffectSystem.GetActiveEffects();
			GD.Print($"激活效果: {effects.Count} 个");
		}
		
		if (_resourceSystem != null)
		{
			var resources = _resourceSystem.GetPlayerResources();
			GD.Print($"物资库存: {resources.Count} 种");
		}
		
		GD.Print("==============================");
	}
}
