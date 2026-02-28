using Godot;
using System;
using System.Collections.Generic;

public partial class UIManager : Control
{
	// UI场景引用
	private PackedScene _combatUIScene;
	private PackedScene _farmUIScene;
	private PackedScene _mapUIScene;
	private PackedScene _rewardUIScene;
	private PackedScene _mainMenuScene;
	private PackedScene _gameOverScene;
	private PackedScene _pauseMenuScene;
	private PackedScene _enderChestUIScene;
	
	// 当前UI实例
	private Control _currentCombatUI;
	private Control _currentFarmUI;
	private Control _currentMapUI;
	private Control _currentRewardUI;
	private Control _currentMainMenu;
	private Control _currentGameOver;
	private Control _currentPauseMenu;
	private Control _currentEnderChestUI;
	
	// 画布层保证渲染在其上
	private CanvasLayer _uiLayer;
	
	// UI元素引用（通过Export在编辑器中设置）
	[Export] private Label _playerHealthLabel;
	private Label _combatPlayerHealthLabel; // 仅在默认战斗UI创建时赋值

	[Export] private Label _playerGoldLabel;
	private Label _combatPlayerShieldLabel; // 显示本回合护甲/护盾
	[Export] private Label _floorLabel;
	[Export] private Label _actionPointsLabel;
	[Export] private HBoxContainer _handContainer;
	[Export] private ProgressBar _healthBar;
	[Export] private Control _notificationPanel;
	[Export] private Label _notificationLabel;
	
	// 游戏管理器引用
	private GameManager _gameManager;
	private DataManager _dataManager;
	
	// UI状态
	private bool _isInitialized = false;
	// 初始化为非法值以确保首次状态切换生效
	private GameEnums.GameState _lastGameState = (GameEnums.GameState)(-1);
	
	// 卡牌UI缓存
	private List<CardUI> _cardUIs = new List<CardUI>();
	
	// 通知系统
	private Timer _notificationTimer;
	private Queue<string> _notificationQueue = new Queue<string>();

	// Boss 击败后等待末影箱关闭再弹窗标记
	private string _pendingBossDefeatedEnemyId = null;

		// --- 农场界面相关字段 (原 FarmUIController 内容合并) ---
		[Export] private GridContainer _plotsContainer;
		[Export] private VBoxContainer _cropSelectionContainer;
		[Export] private VBoxContainer _activeEffectsContainer;
		[Export] private VBoxContainer _resourcesContainer;
		[Export] private Button _farmCloseButton;
		[Export] private Label _farmTitleLabel;
		[Export] private PackedScene _plotUIScene;

		private Dictionary<int, Control> _plotUIs = new();
		private int? _selectedPlotIndex = null;

	public override void _Ready()
	{
		_uiLayer = new CanvasLayer();
		AddChild(_uiLayer);
		
		InitializeManagers();
		LoadUIScenes();
		SetupNotificationSystem();
		SubscribeToEvents();
		InitializeUI();
		
		GD.Print("UIManager 初始化完成");
	}

	public override void _Process(double delta)
	{
		// 处理通知队列
		ProcessNotificationQueue();
	}

	#region 初始化方法

	private void InitializeManagers()
	{
		// 获取游戏管理器
		_gameManager = GetNode<GameManager>("/root/GameManager");
		if (_gameManager == null)
		{
			GD.PrintErr("GameManager 未找到");
			return;
		}
		
		// 获取数据管理器
		_dataManager = GetNode<DataManager>("/root/DataManager");
		if (_dataManager == null)
		{
			GD.PrintErr("DataManager 未找到");
		}
		
		_isInitialized = true;
	}

	private void LoadUIScenes()
	{
		// 加载UI场景（如果存在）
		// 注意：这些场景需要您在Godot编辑器中创建
		// 暂时注释掉空场景加载，强制使用代码生成的 DefaultCombatUI 进行测试
		// if (ResourceLoader.Exists("res://UI/CombatUI.tscn"))
		// {
		// 	_combatUIScene = GD.Load<PackedScene>("res://UI/CombatUI.tscn");
		// }
		
		if (ResourceLoader.Exists("res://UI/FarmUI.tscn"))
		{
			_farmUIScene = GD.Load<PackedScene>("res://UI/FarmUI.tscn");
		}
		
		// if (ResourceLoader.Exists("res://UI/MapUI.tscn"))
		// {
		// 	_mapUIScene = GD.Load<PackedScene>("res://UI/MapUI.tscn");
		// }
		
		// if (ResourceLoader.Exists("res://UI/RewardUI.tscn"))
		// {
		// 	_rewardUIScene = GD.Load<PackedScene>("res://UI/RewardUI.tscn");
		// }
		// if (ResourceLoader.Exists("res://UI/MainMenuUI.tscn"))
		// {
		// 	_mainMenuScene = GD.Load<PackedScene>("res://UI/MainMenuUI.tscn");
		// }
		
		if (ResourceLoader.Exists("res://UI/GameOverUI.tscn"))
		{
			_gameOverScene = GD.Load<PackedScene>("res://UI/GameOverUI.tscn");
		}
		
		if (ResourceLoader.Exists("res://UI/PauseMenuUI.tscn"))
		{
			_pauseMenuScene = GD.Load<PackedScene>("res://UI/PauseMenuUI.tscn");
		}
		
		if (ResourceLoader.Exists("res://UI/EnderChestUI.tscn"))
		{
			_enderChestUIScene = GD.Load<PackedScene>("res://UI/EnderChestUI.tscn");
		}
		
		GD.Print("UI场景加载完成");
	}

	private void SetupNotificationSystem()
	{
		_notificationTimer = new Timer();
		AddChild(_notificationTimer);
		_notificationTimer.WaitTime = 3.0;
		_notificationTimer.OneShot = true;
		_notificationTimer.Timeout += HideNotification;
		
		if (_notificationPanel != null)
		{
			_notificationPanel.Visible = false;
		}
	}

	private void SubscribeToEvents()
	{
		var eventBus = EventBus.Instance;
		if (eventBus == null)
		{
			GD.PrintErr("EventBus 未找到");
			return;
		}
		
		// 游戏状态事件
		eventBus.GameStateChanged += OnGameStateChanged;
		eventBus.GameStarted += OnGameStarted;
		eventBus.GamePaused += OnGamePaused;
		eventBus.GameResumed += OnGameResumed;
		eventBus.GameEnded += OnGameEnded;
		
		// 战斗事件
		eventBus.CombatStarted += OnCombatStarted;
		eventBus.CardPlayed += OnCardPlayed;
		eventBus.PlayerDamaged += OnPlayerDamaged;
		eventBus.EnemyDamaged += OnEnemyDamaged;
		eventBus.EnemyDefeated += OnEnemyDefeated;
		eventBus.TurnEnded += OnTurnEnded;
		eventBus.PlayerTurnStarted += OnPlayerTurnStarted;
		
		// 农场事件
		eventBus.CropPlanted += OnCropPlanted;
		eventBus.CropHarvested += OnCropHarvested;
		eventBus.CropGrowthUpdated += OnCropGrowthUpdated;
		eventBus.CropEffectApplied += OnCropEffectApplied;
		
		// 地图事件
		eventBus.RoomEntered += OnRoomEntered;
		eventBus.FloorCompleted += OnFloorCompleted;
		eventBus.NotificationRequested += OnNotificationRequested;
		eventBus.CenterPopupRequested += OnCenterPopupRequested;
		
		// 末影箱事件
		eventBus.EnderChestOpened += OnEnderChestOpened;
		eventBus.CropSelectedFromChest += OnCropSelectedFromChest;
		eventBus.EnderChestClosed += OnEnderChestClosed;
		// Boss 击败后选择返回农场
		eventBus.BossDefeated += OnBossDefeated;
		
		// 战斗物资事件
		eventBus.CombatResourcesGenerated += OnCombatResourcesGenerated;
		eventBus.ResourceAppliedToCrop += OnResourceAppliedToCrop;
		
		// 玩家事件
		eventBus.StatUpdated += OnStatUpdated;
		eventBus.ItemCollected += OnItemCollected;
		eventBus.CardAddedToDeck += OnCardAddedToDeck;
		
		GD.Print("UI事件订阅完成");
	}

	private void InitializeUI()
	{
		if (!_isInitialized) return;
		
		// 初始化UI元素
		UpdatePlayerStats();
		UpdateFloorInfo();
		
		// 根据游戏管理器当前状态显示对应UI，以避免主菜单与其他界面重叠
		var initialState = _gameManager != null ? _gameManager.CurrentState : GameEnums.GameState.MainMenu;
		OnGameStateChanged(initialState);
	}

	#endregion

	#region UI显示控制

	private void OnGameStateChanged(GameEnums.GameState newState)
	{
		if (_lastGameState == newState) return;
		
		_lastGameState = newState;
		
		// 清理所有UI
		ClearAllUI();
		
		// 根据状态显示对应UI
		switch (newState)
		{
			case GameEnums.GameState.MainMenu:
				ShowMainMenu();
				break;
				
			case GameEnums.GameState.MapExploration:
				ShowMapUI();
				break;
				
			case GameEnums.GameState.Combat:
				ShowCombatUI();
				break;
				
			case GameEnums.GameState.Farming:
				ShowFarmUI();
				break;
				
			case GameEnums.GameState.RewardSelection:
				ShowRewardUI();
				break;
				
			case GameEnums.GameState.GameOver:
				ShowGameOverUI();
				break;
		}
		
		// 更新全局UI
		UpdateGlobalUI();
		
		GD.Print($"UI状态切换: {newState}");
	}

	private void ShowMainMenu()
	{
		if (_mainMenuScene != null)
		{
			_currentMainMenu = _mainMenuScene.Instantiate<Control>();
			_uiLayer.AddChild(_currentMainMenu);
			
			// 连接主菜单按钮
			ConnectMainMenuButtons();
		}
		else
		{
			GD.Print("主菜单场景未加载，显示默认UI");
			CreateDefaultMainMenu();
		}
	}

	private void ShowMapUI()
	{
		if (_mapUIScene != null)
		{
			_currentMapUI = _mapUIScene.Instantiate<Control>();
			_uiLayer.AddChild(_currentMapUI);
		}
		else
		{
			
			CreateDefaultMapUI();
			GD.Print("地图UI场景未加载，显示默认UI");
		}
		
		UpdateMapNavigation();
		UpdateMapVisuals();
		ShowNotification("探索地图中...");
	}

	private void ShowCombatUI()
	{
		if (_combatUIScene != null)
		{
			_currentCombatUI = _combatUIScene.Instantiate<Control>();
			_uiLayer.AddChild(_currentCombatUI);
			
			// 初始化战斗UI
			InitializeCombatUI();
		}
		else
		{
			CreateDefaultCombatUI();
		}
		
		ShowNotification("战斗开始!");
		UpdateCombatEnemyInfo();
		UpdateHandCards();
	}

	private void ShowFarmUI()
	{
		if (_farmUIScene != null)
		{
			_currentFarmUI = _farmUIScene.Instantiate<Control>();
			_uiLayer.AddChild(_currentFarmUI);

			// 如果场景中有关闭按钮，连接其回调到 OnCloseFarmButtonPressed
			var closeBtn = FindCloseButton(_currentFarmUI);
			if (closeBtn != null)
			{
				closeBtn.Pressed += OnCloseFarmButtonPressed;
			}

			// 初始化农场UI
			InitializeFarmPlotUIs();	
		}
		else
		{
			CreateDefaultFarmUI();
		}
		
		ShowNotification("进入农场");
		SetupFarmUI();
	}

	private void ShowRewardUI()
	{
		if (_rewardUIScene != null)
		{
			_currentRewardUI = _rewardUIScene.Instantiate<Control>();
			AddChild(_currentRewardUI);
		}
		else
		{
			CreateDefaultRewardUI();
		}
		
		ShowNotification("选择奖励");
	}

	private void ShowGameOverUI()
	{
		if (_gameOverScene != null)
		{
			_currentGameOver = _gameOverScene.Instantiate<Control>();
			AddChild(_currentGameOver);
			
			// 连接游戏结束按钮
			ConnectGameOverButtons();
		}
		else
		{
			CreateDefaultGameOverUI();
		}
	}

	private void ShowPauseMenu()
	{
		if (_pauseMenuScene != null)
		{
			_currentPauseMenu = _pauseMenuScene.Instantiate<Control>();
			AddChild(_currentPauseMenu);
			
			// 连接暂停菜单按钮
			ConnectPauseMenuButtons();
		}
		else
		{
			CreateDefaultPauseMenu();
		}
	}

	private void ShowEnderChestUI(EnderChestData chestData)
	{
		if (_enderChestUIScene != null)
		{
			_currentEnderChestUI = _enderChestUIScene.Instantiate<Control>();
			// 保证所有 UI 统一渲染在 CanvasLayer 上
			_uiLayer.AddChild(_currentEnderChestUI);

			// 尝试将场景内的主面板居中显示（若场景结构发生变化，做合理回退）
			Control center = _currentEnderChestUI.GetNodeOrNull<Control>("CenterContainer");
			if (center == null)
			{
				foreach (Node child in _currentEnderChestUI.GetChildren())
				{
					if (child is Control c)
					{
						center = c;
						break;
					}
				}
			}
			if (center != null)
			{
				var viewportSize = GetViewport().GetVisibleRect().Size;
				var targetPos = (viewportSize - center.Size) / 2;
				// 使用延迟设置以确保在布局完成后应用位置
				center.SetDeferred("rect_position", targetPos);
			}

			// 显示末影箱内容
			var controller = _currentEnderChestUI as EnderChestUIController;
			if (controller != null)
			{
				controller.ShowEnderChest(chestData);
			}
		}
		else
		{
			GD.Print("末影箱UI场景未加载，显示默认通知");
			ShowNotification("末影箱已打开！");
		}
	}

	private void ClearAllUI()
	{
		// 清理所有UI实例
		if (_currentCombatUI != null)
		{
			_currentCombatUI.QueueFree();
			_currentCombatUI = null;
		}
		
		if (_currentFarmUI != null)
		{
			_currentFarmUI.QueueFree();
			_currentFarmUI = null;
		}
		
		if (_currentMapUI != null)
		{
			_currentMapUI.QueueFree();
			_currentMapUI = null;
		}
		
		if (_currentRewardUI != null)
		{
			_currentRewardUI.QueueFree();
			_currentRewardUI = null;
		}
		
		if (_currentMainMenu != null)
		{
			_currentMainMenu.QueueFree();
			_currentMainMenu = null;
		}
		
		if (_currentGameOver != null)
		{
			_currentGameOver.QueueFree();
			_currentGameOver = null;
		}
		
		if (_currentPauseMenu != null)
		{
			_currentPauseMenu.QueueFree();
			_currentPauseMenu = null;
		}
		
		if (_currentEnderChestUI != null)
		{
			_currentEnderChestUI.QueueFree();
			_currentEnderChestUI = null;
		}
		
		// 清理卡牌UI
		foreach (var cardUI in _cardUIs)
		{
			cardUI.QueueFree();
		}
		_cardUIs.Clear();
		
		_handContainer = null;
	}

	private void UpdateGlobalUI()
	{
		// 更新全局显示的UI元素
		UpdatePlayerStats();
		UpdateFloorInfo();
		
		// 根据暂停状态更新
		if (_gameManager != null && _gameManager.IsPaused)
		{
			// 显示暂停菜单
			ShowPauseMenu();
		}
		else
		{
			// 隐藏暂停菜单
			if (_currentPauseMenu != null)
			{
				_currentPauseMenu.QueueFree();
				_currentPauseMenu = null;
			}
		}
	}

	#region 农场UI方法

	private void SetupFarmUI()
	{
		// 初始化管理器引用在上层已完成
		InitializeFarmPlotUIs();
		UpdateAllFarmUI();
	}

	private void InitializeFarmPlotUIs()
	{
		if (_plotsContainer == null) return;
		// 清除已有
		foreach (var child in _plotsContainer.GetChildren())
		{
			child.QueueFree();
		}
		_plotUIs.Clear();

		var plots = GameRoot.Instance?.FarmingSystem?.Plots;
		if (plots == null) return;
		foreach (var plot in plots.Values)
		{
			CreatePlotUI(plot.Index);
		}
	}

	private Button FindCloseButton(Node root)
	{
		if (root == null) return null;
		// 先尝试常见名字
		var tryNames = new string[] { "CloseButton", "CloseBtn", "Close", "BtnClose", "CloseFarmButton" };
		foreach (var name in tryNames)
		{
			var btn = root.GetNodeOrNull<Button>(name);
			if (btn != null) return btn;
		}

		// 递归搜索：找到任意文本为“关闭”或"Close"的 Button
		foreach (Node child in root.GetChildren())
		{
			if (child is Button b)
			{
				if (!string.IsNullOrEmpty(b.Text) && (b.Text.Contains("关闭") || b.Text.Equals("Close", StringComparison.OrdinalIgnoreCase)))
					return b;
			}
			else
			{
				var found = FindCloseButton(child);
				if (found != null) return found;
			}
		}
		return null;
	}

	private void CreatePlotUI(int plotIndex)
	{
		var plotContainer = new VBoxContainer();
		plotContainer.Name = $"Plot_{plotIndex}";

		var plotButton = new Button();
		plotButton.Text = $"地块 {plotIndex + 1}";
		plotButton.Pressed += () => OnPlotClicked(plotIndex);
		plotContainer.AddChild(plotButton);

		var cropLabel = new Label { Name = "CropLabel", Text = "空" };
		plotContainer.AddChild(cropLabel);

		var progressBar = new ProgressBar
		{
			Name = "ProgressBar",
			MinValue = 0,
			MaxValue = 1,
			Value = 0
		};
		plotContainer.AddChild(progressBar);

		var actionContainer = new HBoxContainer { Name = "ActionContainer" };

		var harvestButton = new Button { Name = "HarvestButton", Text = "收获", Visible = false };
		harvestButton.Pressed += () => OnHarvestClicked(plotIndex);
		actionContainer.AddChild(harvestButton);

		var useResourceButton = new Button { Name = "UseResourceButton", Text = "使用物资", Visible = false };
		useResourceButton.Pressed += () => OnUseResourceClicked(plotIndex);
		actionContainer.AddChild(useResourceButton);

		plotContainer.AddChild(actionContainer);

		_plotsContainer.AddChild(plotContainer);
		_plotUIs[plotIndex] = plotContainer;

		UpdatePlotUI(plotIndex);
	}

	private void UpdatePlotUI(int plotIndex)
	{
		if (!_plotUIs.ContainsKey(plotIndex)) return;
		var plotUI = _plotUIs[plotIndex];
		var farming = GameRoot.Instance?.FarmingSystem;
		var dataMgr = GameRoot.Instance?.DataManager;
		if (farming == null) return;
		if (!farming.Plots.ContainsKey(plotIndex)) return;
		var plot = farming.Plots[plotIndex];

		var cropLabel = plotUI.GetNodeOrNull<Label>("CropLabel");
		var progressBar = plotUI.GetNodeOrNull<ProgressBar>("ProgressBar");
		var harvestButton = plotUI.GetNodeOrNull<Button>("ActionContainer/HarvestButton");
		var useResourceButton = plotUI.GetNodeOrNull<Button>("ActionContainer/UseResourceButton");

		if (plot.IsOccupied && !string.IsNullOrEmpty(plot.CropId))
		{
			var cropData = dataMgr?.GetCrop(plot.CropId);
			if (cropData != null)
			{
				cropLabel.Text = $"{cropData.Name}\n进度: {plot.GrowthProgress * 100:F1}%";
				progressBar.Value = plot.GrowthProgress;
				harvestButton.Visible = plot.IsReady;
				useResourceButton.Visible = !plot.IsReady;
			}
		}
		else
		{
			cropLabel.Text = "空";
			progressBar.Value = 0;
			harvestButton.Visible = false;
			useResourceButton.Visible = false;
		}
	}

	private void UpdateAllFarmUI()
	{
		if (GameRoot.Instance?.FarmingSystem == null) return;
		foreach (var idx in _plotUIs.Keys)
		{
			UpdatePlotUI(idx);
		}
		UpdateActiveEffectsUI();
		UpdateResourcesUI();
	}

	private void UpdateActiveEffectsUI()
	{
		if (_activeEffectsContainer == null) return;
		var effects = GameRoot.Instance?.CropEffectSystem?.GetActiveEffects();
		foreach (var child in _activeEffectsContainer.GetChildren()) child.QueueFree();
		if (effects != null && effects.Count > 0)
		{
			foreach (var effect in effects)
			{
				var label = new Label { Text = $"✓ {effect.CropName} ({effect.EffectType})" };
				_activeEffectsContainer.AddChild(label);
			}
		}
		else
		{
			var label = new Label { Text = "暂无激活效果", Modulate = new Color(0.7f,0.7f,0.7f) };
			_activeEffectsContainer.AddChild(label);
		}
	}

	private void UpdateResourcesUI()
	{
		if (_resourcesContainer == null) return;
		var resources = GameRoot.Instance?.CombatResourceSystem?.GetPlayerResources();
		foreach (var child in _resourcesContainer.GetChildren()) child.QueueFree();
		if (resources == null || resources.Count == 0)
		{
			var label = new Label { Text = "暂无物资", Modulate = new Color(0.7f,0.7f,0.7f) };
			_resourcesContainer.AddChild(label);
		}
		else
		{
			foreach (var res in resources)
			{
				var data = GameRoot.Instance?.CombatResourceSystem?.GetResourceData(res.Key);
				if (data != null)
				{
					_resourcesContainer.AddChild(new Label { Text = $"{data.Name} x{res.Value}" });
				}
			}
		}
	}

	// 重用现有事件处理或新增相关的返回更新UI的逻辑
	private void OnCropGrowthUpdated(string cropId, int plotIndex, float progress)
	{
		UpdatePlotUI(plotIndex);
	}

	private void OnResourceAppliedToCrop(int plotIndex, string resourceId)
	{
		UpdatePlotUI(plotIndex);
		UpdateResourcesUI();
		// 通知显示
		var resourceData = GameRoot.Instance?.CombatResourceSystem?.GetResourceData(resourceId);
		if (resourceData != null) ShowNotification($"对地块 {plotIndex + 1} 使用了 {resourceData.Name}");
	}

	private void OnCombatResourcesGenerated_Farm(Dictionary<string, int> resources)
	{
		UpdateResourcesUI();
		if (resources == null || resources.Count == 0) return;
		var names = new List<string>();
		foreach (var kv in resources)
		{
			var data = GameRoot.Instance?.CombatResourceSystem?.GetResourceData(kv.Key);
			if (data != null) names.Add(data.Name);
		}
		if (names.Count>0) ShowNotification($"战斗胜利！获得物资: {string.Join(", ", names)}");
	}

	// 交互
	private void OnPlotClicked(int plotIndex)
	{
		_selectedPlotIndex = plotIndex;
		var farming = GameRoot.Instance?.FarmingSystem;
		if (farming == null || !farming.Plots.ContainsKey(plotIndex)) return;
		var plot = farming.Plots[plotIndex];
		if (!plot.IsOccupied)
		{
			ShowCropSelection(plotIndex);
		}
		else
		{
			ShowCropInfo(plotIndex);
		}
	}

	private void OnHarvestClicked(int plotIndex)
	{
		var reward = GameRoot.Instance?.FarmingSystem?.TryHarvestCrop(plotIndex);
		if (reward != null)
		{
			GD.Print("收获成功！获得奖励");
			UpdatePlotUI(plotIndex);
		}
		else
		{
			GD.PrintErr("收获失败");
		}
	}

	private void OnUseResourceClicked(int plotIndex)
	{
		ShowResourceSelection(plotIndex);
	}

	private void OnCloseFarmButtonPressed()
	{
		// 关闭农场并进入地图探索
		if (_gameManager != null)
		{
			// 关闭农场后通过 Main.StartGame 触发战斗地图测试。
			// StartGame 会初始化新游戏并负责状态切换。
			var mainNode = GetNodeOrNull<Main>("/root/Main");
			if (mainNode != null)
			{
				mainNode.CallDeferred(nameof(Main.StartGame));
				GD.Print("调用 Main.StartGame 进行测试流程");
			}
		}
		if (_currentFarmUI != null)
		{
			_currentFarmUI.QueueFree();
		}

		
	}

	private void ShowCropSelection(int plotIndex)
	{
		if (_cropSelectionContainer == null) return;
		foreach (var child in _cropSelectionContainer.GetChildren()) child.QueueFree();
		var allCrops = GameRoot.Instance?.DataManager?.GetAllCrops();
		var title = new Label { Text = "选择要种植的作物:" };
		title.AddThemeFontSizeOverride("font_size", 18);
		_cropSelectionContainer.AddChild(title);
		if (allCrops != null)
		{
			foreach (var crop in allCrops)
			{
				var btn = new Button { Text = $"{crop.Name}\n{crop.Description}" };
				btn.Pressed += () => OnCropSelected(plotIndex, crop.Id);
				_cropSelectionContainer.AddChild(btn);
			}
		}
		var cancel = new Button { Text = "取消" };
		cancel.Pressed += () => { foreach (var child in _cropSelectionContainer.GetChildren()) child.QueueFree(); };
		_cropSelectionContainer.AddChild(cancel);
	}

	private void OnCropSelected(int plotIndex, string cropId)
	{
		if (GameRoot.Instance?.FarmingSystem == null) return;
		if (GameRoot.Instance.FarmingSystem.TryPlantCrop(plotIndex, cropId))
		{
			UpdatePlotUI(plotIndex);
			foreach (var child in _cropSelectionContainer.GetChildren()) child.QueueFree();
		}
		else
		{
			GD.PrintErr($"种植失败: {cropId}");
		}
	}

	private void ShowResourceSelection(int plotIndex)
	{
		var dialog = new AcceptDialog();
		dialog.Title = "选择要使用的物资";
		dialog.Size = new Vector2I(400,300);
		var container = new VBoxContainer();
		dialog.AddChild(container);
		var resources = GameRoot.Instance?.CombatResourceSystem?.GetPlayerResources();
		if (resources == null || resources.Count==0)
		{
			container.AddChild(new Label { Text = "没有可用物资" });
		}
		else
		{
			foreach (var res in resources)
			{
				var data = GameRoot.Instance?.CombatResourceSystem?.GetResourceData(res.Key);
				if (data != null)
				{
					var btn = new Button { Text = $"{data.Name} x{res.Value}\n{data.Description}" };
					btn.Pressed += () => {
						if (_gameManager.UseResourceOnCrop(res.Key, plotIndex))
						{
							UpdatePlotUI(plotIndex);
							UpdateResourcesUI();
							dialog.QueueFree();
						}
						else GD.PrintErr("使用物资失败");
					};
					container.AddChild(btn);
				}
			}
		}
		var cancelBtn = new Button { Text = "取消" };
		cancelBtn.Pressed += () => dialog.QueueFree();
		container.AddChild(cancelBtn);
		AddChild(dialog);
		dialog.PopupCentered();
	}

	private void ShowCropInfo(int plotIndex)
	{
		GD.Print($"显示作物信息: 地块 {plotIndex}");
	}

	#endregion

	#endregion

	#region 默认UI创建方法（备用）

	private void CreateDefaultMainMenu()
	{
		var container = new VBoxContainer();
		container.Name = "DefaultMainMenu";
		container.SetAnchorsPreset(Control.LayoutPreset.Center);
		container.GrowHorizontal = Control.GrowDirection.Both;
		container.GrowVertical = Control.GrowDirection.Both;
		container.Alignment = BoxContainer.AlignmentMode.Center;
		
		var title = new Label();
		title.Text = "蝗灾农场防御战";
		title.AddThemeFontSizeOverride("font_size", 32);
		container.AddChild(title);
		
		var startButton = new Button();
		startButton.Text = "开始游戏";
		startButton.Pressed += OnStartButtonPressed;
		container.AddChild(startButton);
		
		var loadButton = new Button();
		loadButton.Text = "加载存档";
		loadButton.Pressed += OnLoadButtonPressed;
		container.AddChild(loadButton);
		
		var settingsButton = new Button();
		settingsButton.Text = "设置";
		settingsButton.Pressed += OnSettingsButtonPressed;
		container.AddChild(settingsButton);
		
		var quitButton = new Button();
		quitButton.Text = "退出游戏";
		quitButton.Pressed += OnQuitButtonPressed;
		container.AddChild(quitButton);
		
		_currentMainMenu = container;
		_uiLayer.AddChild(container);
	}

	private void CreateDefaultCombatUI()
	{
		var root = new ColorRect();
		root.Name = "DefaultCombatUI";
		// 让背景覆盖全屏
		root.Color = new Color(0, 0, 0, 0.9f); 
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		
		// 提供一个居中的容器
		var box = new MarginContainer();
		box.SetAnchorsPreset(Control.LayoutPreset.Center);
		box.GrowHorizontal = Control.GrowDirection.Both;
		box.GrowVertical = Control.GrowDirection.Both;
		root.AddChild(box);
		
		var container = new VBoxContainer();
		container.Name = "CombatLayout";
		container.Alignment = BoxContainer.AlignmentMode.Center;
		box.AddChild(container);
		
		// 顶部：玩家生命与护盾显示
		var playerStats = new HBoxContainer();
		playerStats.Alignment = BoxContainer.AlignmentMode.Center;
		playerStats.AddThemeConstantOverride("separation", 12);

		_combatPlayerHealthLabel = new Label();
		_combatPlayerHealthLabel.Name = "PlayerHealthLabel";
		_combatPlayerHealthLabel.Text = "生命: --/--";
		playerStats.AddChild(_combatPlayerHealthLabel);

		_combatPlayerShieldLabel = new Label();
		_combatPlayerShieldLabel.Name = "PlayerShieldLabel";
		_combatPlayerShieldLabel.Text = "护甲: 0";
		playerStats.AddChild(_combatPlayerShieldLabel);

		container.AddChild(playerStats);

		var enemyInfo = new Label();
		enemyInfo.Name = "EnemyInfo";
		enemyInfo.Text = "敌人: ???";
		container.AddChild(enemyInfo);
		
		var handLabel = new Label();
		handLabel.Text = "手牌:";
		container.AddChild(handLabel);
		
		var handContainer = new HBoxContainer();
		handContainer.Name = "HandContainer";
		handContainer.Alignment = BoxContainer.AlignmentMode.Center; //居中
		handContainer.AddThemeConstantOverride("separation", 15);
		container.AddChild(handContainer);
		
		// 将其赋值给全局变量
		_handContainer = handContainer;
		
		// 拉开点距离
		container.AddChild(new MarginContainer { CustomMinimumSize = new Vector2(0, 30) });
		
		var endTurnButton = new Button();
		endTurnButton.Text = "结束回合";
		endTurnButton.Pressed += OnEndTurnButtonPressed;
		container.AddChild(endTurnButton);
		
		_currentCombatUI = root;
		_uiLayer.AddChild(root);
	}

	private void CreateDefaultMapUI()
	{
		var container = new Control();
		container.Name = "DefaultMapUI";
		container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		
		var centerBox = new CenterContainer();
		centerBox.Name = "CenterBox";
		centerBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		container.AddChild(centerBox);
		
		// 加一个明显的背景底板避免和游戏画面混在一起
		var bgPanel = new ColorRect();
		bgPanel.Name = "BgPanel";
		bgPanel.Color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
		bgPanel.CustomMinimumSize = new Vector2(500, 450);
		centerBox.AddChild(bgPanel);
		
		var marginBox = new MarginContainer();
		marginBox.Name = "MarginBox";
		marginBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		bgPanel.AddChild(marginBox);
		
		var innerContainer = new VBoxContainer();
		innerContainer.Name = "VBoxContainer";
		innerContainer.Alignment = BoxContainer.AlignmentMode.Center;
		marginBox.AddChild(innerContainer);
		
		var mapLabel = new Label();
		mapLabel.Text = "地图探索界面";
		mapLabel.AddThemeFontSizeOverride("font_size", 24);
		mapLabel.HorizontalAlignment = HorizontalAlignment.Center;
		innerContainer.AddChild(mapLabel);
		
		var roomInfo = new Label();
		roomInfo.Name = "RoomInfo";
		roomInfo.Text = "当前房间: ???";
		roomInfo.HorizontalAlignment = HorizontalAlignment.Center;
		innerContainer.AddChild(roomInfo);
		
		// 加个地图网格容器来可视化地图
		var mapGrid = new GridContainer();
		mapGrid.Name = "MapGrid";
		// mapGrid.Alignment = BoxContainer.AlignmentMode.Center; // GridContainer does not have Alignment
		mapGrid.SetAnchorsPreset(Control.LayoutPreset.Center);
		mapGrid.AddThemeConstantOverride("h_separation", 5);
		mapGrid.AddThemeConstantOverride("v_separation", 5);
		innerContainer.AddChild(mapGrid);
		
		// 拉开点距离
		innerContainer.AddChild(new MarginContainer { CustomMinimumSize = new Vector2(0, 20) });
		
		var moveButtons = new HBoxContainer();
		moveButtons.Name = "MoveButtons";
		moveButtons.Alignment = BoxContainer.AlignmentMode.Center;
		
		var upButton = new Button();
		upButton.Name = "UpButton";
		upButton.Text = "上";
		upButton.Pressed += () => OnMoveButtonPressed(new Vector2I(0, -1));
		moveButtons.AddChild(upButton);
		
		var leftButton = new Button();
		leftButton.Name = "LeftButton";
		leftButton.Text = "左";
		leftButton.Pressed += () => OnMoveButtonPressed(new Vector2I(-1, 0));
		moveButtons.AddChild(leftButton);
		
		var rightButton = new Button();
		rightButton.Name = "RightButton";
		rightButton.Text = "右";
		rightButton.Pressed += () => OnMoveButtonPressed(new Vector2I(1, 0));
		moveButtons.AddChild(rightButton);
		
		var downButton = new Button();
		downButton.Name = "DownButton";
		downButton.Text = "下";
		downButton.Pressed += () => OnMoveButtonPressed(new Vector2I(0, 1));
		moveButtons.AddChild(downButton);
		
		innerContainer.AddChild(moveButtons);
		
		_currentMapUI = container;
		_uiLayer.AddChild(container);
	}

	private void CreateDefaultFarmUI()
	{
		//调整布局结构：使用全屏根节点，整体靠左排列
		var rootControl = new Control();
		rootControl.Name = "DefaultFarmUI";
		rootControl.SetAnchorsPreset(Control.LayoutPreset.FullRect);

		var mainMargin = new MarginContainer();
		mainMargin.SetAnchorsPreset(Control.LayoutPreset.LeftWide);
		mainMargin.AddThemeConstantOverride("margin_left", 40);
		mainMargin.AddThemeConstantOverride("margin_top", 100);
		mainMargin.AddThemeConstantOverride("margin_bottom", 40);
		mainMargin.MouseFilter = Control.MouseFilterEnum.Ignore;
		rootControl.AddChild(mainMargin);

		var mainHBox = new HBoxContainer();
		mainHBox.AddThemeConstantOverride("separation", 50);
		mainMargin.AddChild(mainHBox);

		// 左侧第一列：农场标题、关闭按钮、地块容器
		var leftCol = new VBoxContainer();
		leftCol.Alignment = BoxContainer.AlignmentMode.Begin;
		leftCol.AddThemeConstantOverride("separation", 12);
		mainHBox.AddChild(leftCol);

		var farmLabel = new Label();
		farmLabel.Text = "农场";
		farmLabel.AddThemeFontSizeOverride("font_size", 24);
		farmLabel.HorizontalAlignment = HorizontalAlignment.Center;
		leftCol.AddChild(farmLabel);
		
		leftCol.AddChild(new MarginContainer { CustomMinimumSize = new Vector2(0, 10) });

		var closeBtn = new Button();
		closeBtn.Text = "关闭";
		closeBtn.Pressed += OnCloseFarmButtonPressed;
		leftCol.AddChild(closeBtn);
		
		leftCol.AddChild(new MarginContainer { CustomMinimumSize = new Vector2(0, 8) });

		_plotsContainer = new GridContainer();
		_plotsContainer.Name = "PlotsContainer";
		_plotsContainer.Columns = 3;
		_plotsContainer.AddThemeConstantOverride("h_separation", 10);
		_plotsContainer.AddThemeConstantOverride("v_separation", 10);
		leftCol.AddChild(_plotsContainer);

		// 左侧第二列：选择作物、物资效果、激活库存
		var rightCol = new VBoxContainer();
		rightCol.Alignment = BoxContainer.AlignmentMode.Begin;
		rightCol.AddThemeConstantOverride("separation", 20);
		mainHBox.AddChild(rightCol);

		// 作物选择容器
		var cropSelectionTitle = new Label { Text = "=== 选择作物 ===" };
		rightCol.AddChild(cropSelectionTitle);
		_cropSelectionContainer = new VBoxContainer();
		_cropSelectionContainer.Name = "CropSelectionContainer";
		_cropSelectionContainer.AddThemeConstantOverride("separation", 6);
		rightCol.AddChild(_cropSelectionContainer);

		// 激活效果容器
		var activeEffectsTitle = new Label { Text = "=== 激活效果 ===" };
		rightCol.AddChild(activeEffectsTitle);
		_activeEffectsContainer = new VBoxContainer();
		_activeEffectsContainer.Name = "ActiveEffectsContainer";
		_activeEffectsContainer.AddThemeConstantOverride("separation", 6);
		rightCol.AddChild(_activeEffectsContainer);

		// 物资容器
		var resourcesTitle = new Label { Text = "=== 物资库存 ===" };
		rightCol.AddChild(resourcesTitle);
		_resourcesContainer = new VBoxContainer();
		_resourcesContainer.Name = "ResourcesContainer";
		_resourcesContainer.AddThemeConstantOverride("separation", 6);
		rightCol.AddChild(_resourcesContainer);

		_currentFarmUI = rootControl;
		_uiLayer.AddChild(rootControl);

		// 完成初始设置
		SetupFarmUI();
	}

	private void CreateDefaultRewardUI()
	{
		var container = new VBoxContainer();
		container.Name = "DefaultRewardUI";
		container.SetAnchorsPreset(Control.LayoutPreset.Center);
		container.GrowHorizontal = Control.GrowDirection.Both;
		container.GrowVertical = Control.GrowDirection.Both;
		container.Alignment = BoxContainer.AlignmentMode.Center;
		
		var rewardLabel = new Label();
		rewardLabel.Text = "选择奖励";
		rewardLabel.AddThemeFontSizeOverride("font_size", 24);
		container.AddChild(rewardLabel);
		
		var reward1Button = new Button();
		reward1Button.Text = "奖励1: +10生命";
		reward1Button.Pressed += () => OnRewardSelected(new RewardData 
		{ 
			Type = GameEnums.RewardType.StatIncrease, 
			StatType = GameEnums.PlayerStatType.MaxHealth, 
			Value = 10 
		});
		container.AddChild(reward1Button);
		
		var reward2Button = new Button();
		reward2Button.Text = "奖励2: +5攻击";
		reward2Button.Pressed += () => OnRewardSelected(new RewardData 
		{ 
			Type = GameEnums.RewardType.StatIncrease, 
			StatType = GameEnums.PlayerStatType.Attack, 
			Value = 5 
		});
		container.AddChild(reward2Button);
		
		var reward3Button = new Button();
		reward3Button.Text = "奖励3: 新卡牌";
		reward3Button.Pressed += () => OnRewardSelected(new RewardData 
		{ 
			Type = GameEnums.RewardType.CardReward, 
			CardId = "card_attack_strong" 
		});
		container.AddChild(reward3Button);
		
		_currentRewardUI = container;
		AddChild(container);
	}

	private void CreateDefaultGameOverUI()
	{
		var container = new VBoxContainer();
		container.Name = "DefaultGameOverUI";
		container.SetAnchorsPreset(Control.LayoutPreset.Center);
		container.GrowHorizontal = Control.GrowDirection.Both;
		container.GrowVertical = Control.GrowDirection.Both;
		container.Alignment = BoxContainer.AlignmentMode.Center;
		
		var gameOverLabel = new Label();
		gameOverLabel.Text = "游戏结束";
		gameOverLabel.AddThemeFontSizeOverride("font_size", 36);
		container.AddChild(gameOverLabel);
		
		var resultLabel = new Label();
		resultLabel.Name = "ResultLabel";
		resultLabel.Text = "你失败了!";
		container.AddChild(resultLabel);
		
		var restartButton = new Button();
		restartButton.Text = "重新开始";
		restartButton.Pressed += OnRestartButtonPressed;
		container.AddChild(restartButton);
		
		var mainMenuButton = new Button();
		mainMenuButton.Text = "返回主菜单";
		mainMenuButton.Pressed += OnMainMenuButtonPressed;
		container.AddChild(mainMenuButton);
		
		_currentGameOver = container;
		AddChild(container);
	}

	private void CreateDefaultPauseMenu()
	{
		var container = new VBoxContainer();
		container.Name = "DefaultPauseMenu";
		container.SetAnchorsPreset(Control.LayoutPreset.Center);
		container.GrowHorizontal = Control.GrowDirection.Both;
		container.GrowVertical = Control.GrowDirection.Both;
		container.Alignment = BoxContainer.AlignmentMode.Center;
		
		var pauseLabel = new Label();
		pauseLabel.Text = "游戏暂停";
		pauseLabel.AddThemeFontSizeOverride("font_size", 28);
		container.AddChild(pauseLabel);
		
		var resumeButton = new Button();
		resumeButton.Text = "继续游戏";
		resumeButton.Pressed += OnResumeButtonPressed;
		container.AddChild(resumeButton);
		
		var settingsButton = new Button();
		settingsButton.Text = "设置";
		settingsButton.Pressed += OnPauseSettingsButtonPressed;
		container.AddChild(settingsButton);
		
		var mainMenuButton = new Button();
		mainMenuButton.Text = "返回主菜单";
		mainMenuButton.Pressed += OnPauseMainMenuButtonPressed;
		container.AddChild(mainMenuButton);
		
		_currentPauseMenu = container;
		AddChild(container);
	}

	#endregion

	#region UI更新方法

	private void UpdatePlayerStats()
	{
		if (!_isInitialized || _gameManager.PlayerData == null) return;
		
		var playerData = _gameManager.PlayerData;
		
		// 更新生命值显示
		if (_playerHealthLabel != null)
		{
			_playerHealthLabel.Text = $"生命: {playerData.CurrentHealth}/{playerData.MaxHealth}";
		}
		
		// 更新生命值进度条
		if (_healthBar != null)
		{
			_healthBar.MaxValue = playerData.MaxHealth;
			_healthBar.Value = playerData.CurrentHealth;
		}
		
		// 更新金币显示
		if (_playerGoldLabel != null)
		{
			_playerGoldLabel.Text = $"金币: {playerData.Gold}";
		}
		
		// 更新行动点显示
		if (_actionPointsLabel != null)
		{
			_actionPointsLabel.Text = $"行动点: {playerData.ActionPoints}";
		}

		// 更新默认战斗UI上的玩家生命/护盾显示（如果创建了默认战斗UI）
		var combatSys = GameRoot.Instance?.CombatSystem;
		if (_combatPlayerHealthLabel != null && playerData != null)
		{
			_combatPlayerHealthLabel.Text = $"生命: {playerData.CurrentHealth}/{playerData.MaxHealth}";
		}
		if (_combatPlayerShieldLabel != null && combatSys != null)
		{
			_combatPlayerShieldLabel.Text = $"护甲: {combatSys.GetPlayerDefense()}";
		}
	}

	private void UpdateFloorInfo()
	{
		if (!_isInitialized) return;
		
		if (_floorLabel != null)
		{
			_floorLabel.Text = $"楼层: {_gameManager.CurrentFloor}";
		}
	}

	private void UpdateHandCards()
	{
		if (!_isInitialized || _handContainer == null || _gameManager.PlayerData == null) return;
		
		var combatSys = GameRoot.Instance?.CombatSystem;
		if (combatSys == null) return;
		
		// 清空现有手牌
		foreach (var cardUI in _cardUIs)
		{
			cardUI.QueueFree();
		}
		_cardUIs.Clear();
		
		// 显示手牌（从战斗系统获取真实的当前手牌）
		var hand = combatSys.PlayerHand;
		int cardCount = hand.Count;
		
		for (int i = 0; i < cardCount; i++)
		{
			var cardId = hand[i];
			var cardData = _dataManager?.GetCard(cardId);
			
			if (cardData != null)
			{
				var cardUI = new CardUI(cardData);
				cardUI.CardClicked += (card) => OnCardClicked(card);
				_handContainer.AddChild(cardUI);
				_cardUIs.Add(cardUI);
			}
		}
		
		GD.Print($"手牌更新: {cardCount} 张");
	}

	private void InitializeCombatUI()
	{
		// 初始化战斗UI的特定元素
		UpdateHandCards();
		
		// 更新敌人信息等
		// 这里可以根据需要扩展
	}

	#endregion

	#region 通知系统

	private void ShowNotification(string message)
	{
		if (_notificationPanel == null || _notificationLabel == null) return;
		
		_notificationQueue.Enqueue(message);
	}

	private void ProcessNotificationQueue()
	{
		if (_notificationQueue.Count > 0 && !_notificationTimer.IsStopped() == false)
		{
			string message = _notificationQueue.Dequeue();
			DisplayNotification(message);
		}
	}

	private void DisplayNotification(string message)
	{
		if (_notificationPanel == null || _notificationLabel == null) return;
		
		_notificationLabel.Text = message;
		_notificationPanel.Visible = true;
		
		// 重新开始计时器
		_notificationTimer.Start();
		
		GD.Print($"显示通知: {message}");
	}

	private void HideNotification()
	{
		if (_notificationPanel != null)
		{
			_notificationPanel.Visible = false;
		}
	}

	private void OnCenterPopupRequested(string message)
	{
		ShowCenterPopup(message);
	}

	private void ShowCenterPopup(string message)
	{
		var popupContainer = new CenterContainer();
		popupContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		popupContainer.MouseFilter = Control.MouseFilterEnum.Ignore;
		
		var panel = new PanelContainer();
		panel.MouseFilter = Control.MouseFilterEnum.Ignore;
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0, 0, 0, 0.7f);
		styleBox.SetCornerRadiusAll(10);
		styleBox.ContentMarginLeft = 40;
		styleBox.ContentMarginRight = 40;
		styleBox.ContentMarginTop = 20;
		styleBox.ContentMarginBottom = 20;
		panel.AddThemeStyleboxOverride("panel", styleBox);

		var label = new Label();
		label.Text = message;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.AddThemeFontSizeOverride("font_size", 32);
		
		panel.AddChild(label);
		popupContainer.AddChild(panel);
		
		if (_uiLayer != null)
		{
			_uiLayer.AddChild(popupContainer);
		}
		else
		{
			AddChild(popupContainer);
		}

		var tween = CreateTween();
		popupContainer.Modulate = new Color(1, 1, 1, 0);
		tween.TweenProperty(popupContainer, "modulate", new Color(1, 1, 1, 1), 0.3f);
		tween.TweenInterval(2.0f);
		tween.TweenProperty(popupContainer, "modulate", new Color(1, 1, 1, 0), 0.5f);
		tween.TweenCallback(Callable.From(popupContainer.QueueFree));
	}

	#endregion

	#region 事件处理方法

	private void OnGameStarted()
	{
		ShowNotification("游戏开始!");
		UpdateGlobalUI();
	}

	private void OnGamePaused()
	{
		ShowNotification("游戏暂停");
		ShowPauseMenu();
	}

	private void OnGameResumed()
	{
		ShowNotification("游戏继续");
		if (_currentPauseMenu != null)
		{
			_currentPauseMenu.QueueFree();
			_currentPauseMenu = null;
		}
	}

	private void OnGameEnded(bool isVictory)
	{
		string message = isVictory ? "恭喜你获胜了!" : "游戏结束，你失败了!";
		ShowNotification(message);
		
		// 更新游戏结束UI的结果标签
		if (_currentGameOver != null)
		{
			var resultLabel = _currentGameOver.GetNode<Label>("ResultLabel");
			if (resultLabel != null)
			{
				resultLabel.Text = isVictory ? "恭喜你获胜了!" : "你失败了!";
			}
		}
	}

	private void OnCombatStarted(string enemyId)
	{
		ShowNotification($"战斗开始! 敌人: {enemyId}");
		
		// 战斗开始时我们需要重绘画布（比如手卡数据现在才准备好）
		UpdateHandCards();
		
		// 更新战斗UI中的敌人信息
		UpdateCombatEnemyInfo(enemyId);
	}

	private void UpdateCombatEnemyInfo(string specificEnemyId = null)
	{
		if (_currentCombatUI == null) return;
		
		var combatSys = GameRoot.Instance?.CombatSystem;
		if (combatSys == null) return;
		
		string enemyId = specificEnemyId ?? combatSys.CurrentEnemyId;
		if (string.IsNullOrEmpty(enemyId)) return;
		
		var enemyInfo = _currentCombatUI.FindChild("EnemyInfo", true, false) as Label;
		if (enemyInfo != null)
		{
			var enemyData = _dataManager?.GetEnemy(enemyId);
			if (enemyData != null)
			{
				enemyInfo.Text = $"敌人: {enemyData.Name} (生命: {combatSys.CurrentEnemyHealth}/{combatSys.CurrentEnemyMaxHealth}) \n攻击: {enemyData.Attack} 防御: {enemyData.Defense}";
			}
		}
	}

	private void OnCardPlayed(string cardId, string targetId)
	{
		ShowNotification($"使用卡牌: {cardId}");
		UpdateHandCards();
		// 卡牌可能会改变玩家的临时护甲/护盾，更新相关显示
		UpdatePlayerStats();
	}

	private void OnPlayerDamaged(int damage)
	{
		ShowNotification($"受到 {damage} 点伤害!");
		UpdatePlayerStats();
		
		// 伤害数字效果
		if (IsInstanceValid(_playerHealthLabel) && _playerHealthLabel.IsInsideTree())
		{
			ShowDamageEffect(damage, _playerHealthLabel.GlobalPosition, false);
		}
		else
		{
			// 如果没有找到玩家血量标签，默认显示在屏幕中央
			var viewportSize = GetViewport().GetVisibleRect().Size;
			ShowDamageEffect(damage, viewportSize / 2, false);
		}
	}

	private void OnEnemyDamaged(string enemyId, int damage)
	{
		ShowNotification($"对敌人造成 {damage} 点伤害");
		
		// 伤害数字效果
		if (_currentCombatUI != null)
		{
			var enemyInfo = _currentCombatUI.FindChild("EnemyInfo", true, false) as Label;
			if (enemyInfo != null)
			{
				ShowDamageEffect(damage, enemyInfo.GlobalPosition, true);
				
				// 获取战斗系统中的实时血量
				var enemyData = _dataManager?.GetEnemy(enemyId);
				var combatSys = GameRoot.Instance?.CombatSystem;
				if (enemyData != null && combatSys != null)
				{
					enemyInfo.Text = $"敌人: {enemyData.Name} (生命: {combatSys.CurrentEnemyHealth}/{combatSys.CurrentEnemyMaxHealth}) \n攻击: {enemyData.Attack} 防御: {enemyData.Defense}";
				}
			}
		}
	}

	private void OnEnemyDefeated(string enemyId)
	{
		ShowNotification($"击败敌人!");
	}

	private void OnTurnEnded()
	{
		ShowNotification("回合结束");
		
		// 重新渲染空手牌
		UpdateHandCards();

		// 刷新玩家状态显示（生命/护甲）
		UpdatePlayerStats();
		
		// 禁用所有卡牌交互
		foreach (var cardUI in _cardUIs)
		{
			cardUI.SetInteractable(false);
		}
	}

	private void OnPlayerTurnStarted()
	{
		ShowNotification("你的回合");
		
		// 渲染新抽的手牌
		UpdateHandCards();
		
		// 启用所有卡牌交互
		foreach (var cardUI in _cardUIs)
		{
			cardUI.SetInteractable(true);
		}

		// 刷新玩家状态显示（生命/护甲）
		UpdatePlayerStats();
	}

	private void OnCropPlanted(string cropId, int plotIndex)
	{
		ShowNotification($"种植了作物: {cropId}");
		UpdatePlotUI(plotIndex);
	}

	private void OnCropHarvested(string cropId, int plotIndex, CropReward reward)
	{
		ShowNotification($"收获了作物: {cropId}");
		UpdatePlotUI(plotIndex);
	}

	private void OnRoomEntered(RoomData room)
	{
		// 具体的节点提示现在由 GameManager 负责通过事件分发，或者在这里统一处理
		// UI不再执行默认的提示以支持自定义的格式
		// ShowNotification($"进入房间: {room.Type}");
		
		// 更新地图UI中的房间信息
		if (_currentMapUI != null)
		{
			var roomInfo = _currentMapUI.GetNodeOrNull<Label>("CenterBox/BgPanel/MarginBox/VBoxContainer/RoomInfo");
			if (roomInfo != null)
			{
				roomInfo.Text = $"当前房间: {room.Type}";
			}
			
			UpdateMapNavigation();
			UpdateMapVisuals();
		}
	}
	
	private void OnNotificationRequested(string message)
	{
		ShowNotification(message);
	}

	private void OnFloorCompleted(int floorNumber)
	{
		ShowNotification($"完成第 {floorNumber} 层!");
		UpdateFloorInfo();
	}

	private void OnStatUpdated(GameEnums.PlayerStatType statType, float value)
	{
		UpdatePlayerStats();
		
		// 显示属性更新效果
		string statName = statType.ToString();
		ShowNotification($"{statName} 更新: {value}");
	}

	private void OnItemCollected(string itemId)
	{
		ShowNotification($"获得物品: {itemId}");
	}

	private void OnCardAddedToDeck(string cardId)
	{
		ShowNotification($"获得新卡牌: {cardId}");
		UpdateHandCards();
	}

	private void OnCropEffectApplied(string cropId)
	{
		var cropData = _dataManager?.GetCrop(cropId);
		if (cropData != null)
		{
			ShowNotification($"作物效果已激活: {cropData.Name}");
		}
		UpdateActiveEffectsUI();
	}

	private void OnEnderChestOpened(EnderChestData chestData)
	{
		ShowEnderChestUI(chestData);
	}

	private void OnCropSelectedFromChest(string cropId, float costValue)
	{
		var cropData = _dataManager?.GetCrop(cropId);
		if (cropData != null)
		{
			ShowNotification($"从末影箱获得: {cropData.Name}");
		}
	}

	private void OnBossDefeated(string enemyId)
	{
		// 如果末影箱UI当前打开，等待其关闭后再弹出对话
		_pendingBossDefeatedEnemyId = enemyId;
		if (_currentEnderChestUI == null)
		{
			// 如果没有末影箱UI，则立即弹出
			ShowBossDefeatedDialog(enemyId);
		}
		else
		{
			GD.Print("Boss 击败：等待末影箱选择完成后再弹出返回农场对话");
		}
	}

	private void OnEnderChestClosed()
	{
		if (!string.IsNullOrEmpty(_pendingBossDefeatedEnemyId))
		{
			ShowBossDefeatedDialog(_pendingBossDefeatedEnemyId);
			_pendingBossDefeatedEnemyId = null;
		}
	}

	private void ShowBossDefeatedDialog(string enemyId)
	{
		var dialog = new AcceptDialog();
		dialog.Title = "击败Boss";
		dialog.DialogText = "你击败了Boss！是否要返回农场领取特殊奖励，或返回地图继续探索？";

		var btnReturnFarm = new Button { Text = "返回农场" };
		btnReturnFarm.Pressed += () => {
			if (_gameManager != null) _gameManager.ChangeState(GameEnums.GameState.Farming);
			dialog.QueueFree();
		};

		var btnReturnMap = new Button { Text = "返回地图" };
		btnReturnMap.Pressed += () => {
			if (_gameManager != null) _gameManager.ChangeState(GameEnums.GameState.MapExploration);
			dialog.QueueFree();
		};

		var container = new VBoxContainer();
		container.AddChild(new Label { Text = dialog.DialogText });
		container.AddChild(btnReturnFarm);
		container.AddChild(btnReturnMap);
		dialog.AddChild(container);
		AddChild(dialog);
		dialog.PopupCentered();
	}

	private void OnCombatResourcesGenerated(Dictionary<string, int> resources)
	{
		if (resources == null || resources.Count == 0)
			return;
		
		var resourceSystem = GameRoot.Instance?.CombatResourceSystem;
		if (resourceSystem == null)
			return;
		
		var resourceNames = new List<string>();
		foreach (var resource in resources)
		{
			var resourceData = resourceSystem.GetResourceData(resource.Key);
			if (resourceData != null)
			{
				resourceNames.Add($"{resourceData.Name} x{resource.Value}");
			}
		}
		
		if (resourceNames.Count > 0)
		{
			ShowNotification($"战斗胜利！获得物资: {string.Join(", ", resourceNames)}");
		}
	}

	#endregion

	#region UI按钮回调方法

	private void OnStartButtonPressed()
	{
		if (_gameManager != null)
		{
			// 初始化玩家数据并直接进入农场界面（不显示地图）
			_gameManager.InitializeNewGameForFarm();
		}
	}

	private void OnLoadButtonPressed()
	{
		ShowNotification("加载存档功能");
		// 这里可以实现存档加载逻辑
	}

	private void OnSettingsButtonPressed()
	{
		ShowNotification("打开设置");
		// 这里可以打开设置界面
	}

	private void OnQuitButtonPressed()
	{
		GetTree().Quit();
	}

	private void OnEndTurnButtonPressed()
	{
		ShowNotification("结束回合");
		
		// 优先尝试调用CombatSystem的主动结束
		var combatSystem = GameRoot.Instance?.CombatSystem;
		if (combatSystem != null)
		{
			combatSystem.EndPlayerTurnEarly();
		}
		else 
		{
			EventBus.Instance?.EmitTurnEnded();
		}
	}

	private void OnMoveButtonPressed(Vector2I direction)
	{
		var mapSystem = GameRoot.Instance?.MapSystem;
		if (mapSystem == null) return;
		
		var currentRoom = mapSystem.CurrentRoom;
		if (currentRoom == null) return;
		
		var newPosition = currentRoom.Position + direction;
		var availableMoves = mapSystem.GetAvailableMoves();
		
		if (availableMoves.Contains(newPosition))
		{
			ShowNotification($"向 {direction} 移动");
			mapSystem.EnterRoom(newPosition);
		}
		else
		{
			ShowNotification("无法向该方向移动！");
		}
	}
	
	private void UpdateMapNavigation()
	{
		if (_currentMapUI == null) return;
		var mapSystem = GameRoot.Instance?.MapSystem;
		if (mapSystem == null) return;
		
		var currentRoom = mapSystem.CurrentRoom;
		if (currentRoom == null) return;
		
		var availableMoves = mapSystem.GetAvailableMoves();
		
		var moveButtons = _currentMapUI.GetNodeOrNull<Control>("CenterBox/BgPanel/MarginBox/VBoxContainer/MoveButtons");
		if (moveButtons != null)
		{
			var upButton = moveButtons.GetNodeOrNull<Button>("UpButton");
			var leftButton = moveButtons.GetNodeOrNull<Button>("LeftButton");
			var rightButton = moveButtons.GetNodeOrNull<Button>("RightButton");
			var downButton = moveButtons.GetNodeOrNull<Button>("DownButton");
			
			if (upButton != null) upButton.Visible = availableMoves.Contains(currentRoom.Position + new Vector2I(0, -1));
			if (leftButton != null) leftButton.Visible = availableMoves.Contains(currentRoom.Position + new Vector2I(-1, 0));
			if (rightButton != null) rightButton.Visible = availableMoves.Contains(currentRoom.Position + new Vector2I(1, 0));
			if (downButton != null) downButton.Visible = availableMoves.Contains(currentRoom.Position + new Vector2I(0, 1));
		}
	}
	
	private void UpdateMapVisuals()
	{
		if (_currentMapUI == null) return;
		var mapSystem = GameRoot.Instance?.MapSystem;
		if (mapSystem == null) return;
		
		var currentRoom = mapSystem.CurrentRoom;
		if (currentRoom == null) return;
		
		var mapGrid = _currentMapUI.GetNodeOrNull<GridContainer>("CenterBox/BgPanel/MarginBox/VBoxContainer/MapGrid");
		if (mapGrid == null) return;
		
		// 获取地图尺寸 (通过向底层系统发请求，这里简化为根据当前层数计算出的默认大小，或者通过反射/扩展公开接口)
		// 为了简单起见，既然层数影响尺寸：3 + Mathf.Min(floor / 3, 2);
		int baseSize = 3;
		int extra = Mathf.Min(mapSystem.CurrentFloor / 3, 2);
		int mapSize = baseSize + extra;
		
		mapGrid.Columns = mapSize;
		
		// 清理旧节点
		foreach (Node child in mapGrid.GetChildren())
		{
			child.QueueFree();
		}
		
		// 重新生成格子 (GridContainer 的子节点按行依次排列，所以外侧循环 Y，内侧循环 X)
		for (int y = 0; y < mapSize; y++)
		{
			for (int x = 0; x < mapSize; x++)
			{
				var pos = new Vector2I(x, y);
				
				// 向 MapSystem 查询该点的房间状况
				var roomDataInfo = mapSystem.GetType().GetMethod("GetRoom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
				RoomData room = null;
				if (roomDataInfo != null)
				{
					room = (RoomData)roomDataInfo.Invoke(mapSystem, new object[] { pos });
				}
				else
				{
					// 回退：直接反射字段
					var mapField = mapSystem.GetType().GetField("_currentFloorMap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
					if (mapField != null)
					{
						var mapData = mapField.GetValue(mapSystem) as List<List<RoomData>>;
						if (mapData != null && x < mapData.Count && y < mapData[0].Count)
						{
							room = mapData[x][y];
						}
					}
				}
				
				var cell = new ColorRect();
				cell.CustomMinimumSize = new Vector2(50, 50);
				
				var label = new Label();
				label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
				label.HorizontalAlignment = HorizontalAlignment.Center;
				label.VerticalAlignment = VerticalAlignment.Center;
				cell.AddChild(label);
				
				if (room == null)
				{
					// 越界或者无效
					cell.Color = new Color(0, 0, 0, 0);
				}
				else
				{
					// 玩家当前所在
					if (pos == currentRoom.Position)
					{
						cell.Color = new Color(0.8f, 0.8f, 0.2f, 1f); // 亮黄色
						label.Text = "You";
						label.AddThemeColorOverride("font_color", new Color(0,0,0,1));
					}
					// 已访问
					else if (room.IsVisited)
					{
						cell.Color = new Color(0.2f, 0.6f, 0.2f, 1f); // 绿色
						label.Text = "√";
					}
					// 当前节点的相邻可访问节点
					else if (currentRoom.Connections.Contains(pos))
					{
						cell.Color = new Color(0.6f, 0.6f, 0.6f, 1f); // 亮灰色
						label.Text = "?";
					}
					// 未相邻未访问
					else
					{
						cell.Color = new Color(0.3f, 0.3f, 0.3f, 1f); // 暗黑色
						label.Text = "";
					}
					
					// 如果是BOSS特殊标记
					if (room.Type == GameEnums.RoomType.Boss)
					{
						label.Text = "B";
						if (pos != currentRoom.Position)
						{
						   if (!room.IsVisited) cell.Color = new Color(0.5f, 0.1f, 0.1f, 1f); // 暗红
						}
					}
				}
				
				mapGrid.AddChild(cell);
			}
		}
	}

	private void OnPlantButtonPressed(string cropId)
	{
		ShowNotification($"种植 {cropId}");
		// 这里应该调用农场系统的种植方法
	}

	private void OnHarvestButtonPressed()
	{
		ShowNotification("收获作物");
		// 这里应该调用农场系统的收获方法
	}

	private void OnRewardSelected(RewardData reward)
	{
		ShowNotification($"选择奖励: {reward.Type}");
		EventBus.Instance?.EmitRewardSelected(reward);
	}

	private void OnRestartButtonPressed()
	{
		if (_gameManager != null)
		{
			_gameManager.StartNewGame();
		}
	}

	private void OnMainMenuButtonPressed()
	{
		if (_gameManager != null)
		{
			_gameManager.ChangeState(GameEnums.GameState.MainMenu);
		}
	}

	private void OnResumeButtonPressed()
	{
		if (_gameManager != null)
		{
			_gameManager.TogglePause();
		}
	}

	private void OnPauseSettingsButtonPressed()
	{
		ShowNotification("暂停菜单设置");
		// 这里可以打开设置界面
	}

	private void OnPauseMainMenuButtonPressed()
	{
		OnMainMenuButtonPressed();
	}

	private void OnCardClicked(CardUI cardUI)
	{
		// 处理卡牌点击
		ShowNotification($"点击卡牌: {cardUI.CardData.Name}");
		
		var combatSystem = GameRoot.Instance?.CombatSystem;
		if (combatSystem != null)
		{
			// 假设我们需要targetId可以后续再扩充目标选择逻辑，目前为空
			combatSystem.PlayCard(cardUI.CardData.Id, "");
		}
		else
		{
			GD.PrintErr("GameRoot 中未找到 CombatSystem 无法出牌");
		}
	}

	#endregion

	#region 辅助方法

	private void ShowDamageEffect(int damage, Vector2 position, bool isEnemy)
	{
		var damageLabel = new Label();
		damageLabel.Text = damage.ToString();
		damageLabel.AddThemeFontSizeOverride("font_size", 24);
		damageLabel.Modulate = isEnemy ? Colors.Red : Colors.White;
		
		AddChild(damageLabel);
		damageLabel.GlobalPosition = position;
		
		// 动画效果
		var tween = CreateTween();
		tween.TweenProperty(damageLabel, "position:y", position.Y - 50, 0.5);
		tween.Parallel().TweenProperty(damageLabel, "modulate:a", 0, 0.5);
		tween.TweenCallback(Callable.From(() => damageLabel.QueueFree()));
	}

	private void ConnectMainMenuButtons()
	{
		// 连接主菜单按钮的事件
		// 这里假设主菜单场景中有特定名称的按钮
		if (_currentMainMenu != null)
		{
			var startButton = _currentMainMenu.GetNode<Button>("StartButton");
			if (startButton != null)
			{
				startButton.Pressed += OnStartButtonPressed;
			}
			
			var loadButton = _currentMainMenu.GetNode<Button>("LoadButton");
			if (loadButton != null)
			{
				loadButton.Pressed += OnLoadButtonPressed;
			}
			
			var quitButton = _currentMainMenu.GetNode<Button>("QuitButton");
			if (quitButton != null)
			{
				quitButton.Pressed += OnQuitButtonPressed;
			}
		}
	}

	private void ConnectGameOverButtons()
	{
		// 连接游戏结束按钮的事件
		if (_currentGameOver != null)
		{
			var restartButton = _currentGameOver.GetNode<Button>("RestartButton");
			if (restartButton != null)
			{
				restartButton.Pressed += OnRestartButtonPressed;
			}
			
			var mainMenuButton = _currentGameOver.GetNode<Button>("MainMenuButton");
			if (mainMenuButton != null)
			{
				mainMenuButton.Pressed += OnMainMenuButtonPressed;
			}
		}
	}

	private void ConnectPauseMenuButtons()
	{
		// 连接暂停菜单按钮的事件
		if (_currentPauseMenu != null)
		{
			var resumeButton = _currentPauseMenu.GetNode<Button>("ResumeButton");
			if (resumeButton != null)
			{
				resumeButton.Pressed += OnResumeButtonPressed;
			}
			
			var mainMenuButton = _currentPauseMenu.GetNode<Button>("MainMenuButton");
			if (mainMenuButton != null)
			{
				mainMenuButton.Pressed += OnPauseMainMenuButtonPressed;
			}
		}
	}

	#endregion
}

#region 卡牌UI类

public partial class CardUI : Button
{
	public CardData CardData { get; private set; }
	public event Action<CardUI> CardClicked;
	
	private Label _nameLabel;
	private Label _costLabel;
	private Label _descriptionLabel;
	
	public CardUI(CardData cardData)
	{
		CardData = cardData;
		InitializeUI();
	}
	
	private void InitializeUI()
	{
		CustomMinimumSize = new Vector2(100, 150);
		
		var container = new VBoxContainer();
		container.Size = Size;
		
		// 卡牌名称
		_nameLabel = new Label();
		_nameLabel.Text = CardData.Name;
		_nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
		container.AddChild(_nameLabel);
		
		// 卡牌费用
		_costLabel = new Label();
		_costLabel.Text = $"费用: {CardData.Cost}";
		container.AddChild(_costLabel);
		
		// 卡牌描述
		_descriptionLabel = new Label();
		_descriptionLabel.Text = CardData.Description;
		_descriptionLabel.AutowrapMode = TextServer.AutowrapMode.Word;
		_descriptionLabel.CustomMinimumSize = new Vector2(90, 60);
		container.AddChild(_descriptionLabel);
		
		AddChild(container);
		
		// 点击事件
		Pressed += OnCardPressed;
	}
	
	private void OnCardPressed()
	{
		CardClicked?.Invoke(this);
	}
	
	public void SetInteractable(bool interactable)
	{
		Disabled = !interactable;
		Modulate = interactable ? Colors.White : Colors.Gray;
	}
}

#endregion
