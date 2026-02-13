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
	
	// UI元素引用（通过Export在编辑器中设置）
	[Export] private Label _playerHealthLabel;
	[Export] private Label _playerGoldLabel;
	[Export] private Label _floorLabel;
	[Export] private Label _actionPointsLabel;
	[Export] private VBoxContainer _handContainer;
	[Export] private ProgressBar _healthBar;
	[Export] private Control _notificationPanel;
	[Export] private Label _notificationLabel;
	
	// 游戏管理器引用
	private GameManager _gameManager;
	private DataManager _dataManager;
	
	// UI状态
	private bool _isInitialized = false;
	private GameEnums.GameState _lastGameState;
	
	// 卡牌UI缓存
	private List<CardUI> _cardUIs = new List<CardUI>();
	
	// 通知系统
	private Timer _notificationTimer;
	private Queue<string> _notificationQueue = new Queue<string>();

	public override void _Ready()
	{
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
		if (ResourceLoader.Exists("res://UI/CombatUI.tscn"))
		{
			_combatUIScene = GD.Load<PackedScene>("res://UI/CombatUI.tscn");
		}
		
		if (ResourceLoader.Exists("res://UI/FarmUI.tscn"))
		{
			_farmUIScene = GD.Load<PackedScene>("res://UI/FarmUI.tscn");
		}
		
		if (ResourceLoader.Exists("res://UI/MapUI.tscn"))
		{
			_mapUIScene = GD.Load<PackedScene>("res://UI/MapUI.tscn");
		}
		
		if (ResourceLoader.Exists("res://UI/RewardUI.tscn"))
		{
			_rewardUIScene = GD.Load<PackedScene>("res://UI/RewardUI.tscn");
		}
		
		if (ResourceLoader.Exists("res://UI/MainMenuUI.tscn"))
		{
			_mainMenuScene = GD.Load<PackedScene>("res://UI/MainMenuUI.tscn");
		}
		
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
		eventBus.CropEffectApplied += OnCropEffectApplied;
		
		// 地图事件
		eventBus.RoomEntered += OnRoomEntered;
		eventBus.FloorCompleted += OnFloorCompleted;
		
		// 末影箱事件
		eventBus.EnderChestOpened += OnEnderChestOpened;
		eventBus.CropSelectedFromChest += OnCropSelectedFromChest;
		
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
		
		// 根据当前游戏状态显示对应UI
		OnGameStateChanged(_gameManager.CurrentState);
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
			AddChild(_currentMainMenu);
			
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
			AddChild(_currentMapUI);
		}
		else
		{
			CreateDefaultMapUI();
		}
		
		ShowNotification("探索地图中...");
	}

	private void ShowCombatUI()
	{
		if (_combatUIScene != null)
		{
			_currentCombatUI = _combatUIScene.Instantiate<Control>();
			AddChild(_currentCombatUI);
			
			// 初始化战斗UI
			InitializeCombatUI();
		}
		else
		{
			CreateDefaultCombatUI();
		}
		
		ShowNotification("战斗开始!");
	}

	private void ShowFarmUI()
	{
		if (_farmUIScene != null)
		{
			_currentFarmUI = _farmUIScene.Instantiate<Control>();
			AddChild(_currentFarmUI);
		}
		else
		{
			CreateDefaultFarmUI();
		}
		
		ShowNotification("进入农场");
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
			AddChild(_currentEnderChestUI);
			
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

	#endregion

	#region 默认UI创建方法（备用）

	private void CreateDefaultMainMenu()
	{
		var container = new VBoxContainer();
		container.Name = "DefaultMainMenu";
		container.Alignment = BoxContainer.AlignmentMode.Center;
		container.Size = new Vector2(400, 300);
		container.Position = new Vector2(GetViewportRect().Size.X / 2 - 200, GetViewportRect().Size.Y / 2 - 150);
		
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
		AddChild(container);
	}

	private void CreateDefaultCombatUI()
	{
		var container = new VBoxContainer();
		container.Name = "DefaultCombatUI";
		
		var enemyInfo = new Label();
		enemyInfo.Name = "EnemyInfo";
		enemyInfo.Text = "敌人: ???";
		container.AddChild(enemyInfo);
		
		var handLabel = new Label();
		handLabel.Text = "手牌:";
		container.AddChild(handLabel);
		
		var handContainer = new HBoxContainer();
		handContainer.Name = "HandContainer";
		container.AddChild(handContainer);
		
		var endTurnButton = new Button();
		endTurnButton.Text = "结束回合";
		endTurnButton.Pressed += OnEndTurnButtonPressed;
		container.AddChild(endTurnButton);
		
		_currentCombatUI = container;
		AddChild(container);
	}

	private void CreateDefaultMapUI()
	{
		var container = new VBoxContainer();
		container.Name = "DefaultMapUI";
		
		var mapLabel = new Label();
		mapLabel.Text = "地图探索界面";
		mapLabel.AddThemeFontSizeOverride("font_size", 24);
		container.AddChild(mapLabel);
		
		var roomInfo = new Label();
		roomInfo.Name = "RoomInfo";
		roomInfo.Text = "当前房间: ???";
		container.AddChild(roomInfo);
		
		var moveButtons = new HBoxContainer();
		
		var upButton = new Button();
		upButton.Text = "上";
		upButton.Pressed += () => OnMoveButtonPressed(new Vector2I(0, -1));
		moveButtons.AddChild(upButton);
		
		var leftButton = new Button();
		leftButton.Text = "左";
		leftButton.Pressed += () => OnMoveButtonPressed(new Vector2I(-1, 0));
		moveButtons.AddChild(leftButton);
		
		var rightButton = new Button();
		rightButton.Text = "右";
		rightButton.Pressed += () => OnMoveButtonPressed(new Vector2I(1, 0));
		moveButtons.AddChild(rightButton);
		
		var downButton = new Button();
		downButton.Text = "下";
		downButton.Pressed += () => OnMoveButtonPressed(new Vector2I(0, 1));
		moveButtons.AddChild(downButton);
		
		container.AddChild(moveButtons);
		
		_currentMapUI = container;
		AddChild(container);
	}

	private void CreateDefaultFarmUI()
	{
		var container = new VBoxContainer();
		container.Name = "DefaultFarmUI";
		
		var farmLabel = new Label();
		farmLabel.Text = "农场界面";
		farmLabel.AddThemeFontSizeOverride("font_size", 24);
		container.AddChild(farmLabel);
		
		var cropInfo = new Label();
		cropInfo.Name = "CropInfo";
		cropInfo.Text = "作物: 无";
		container.AddChild(cropInfo);
		
		var plantButton = new Button();
		plantButton.Text = "种植小麦";
		plantButton.Pressed += () => OnPlantButtonPressed("crop_wheat");
		container.AddChild(plantButton);
		
		var harvestButton = new Button();
		harvestButton.Text = "收获作物";
		harvestButton.Pressed += OnHarvestButtonPressed;
		container.AddChild(harvestButton);
		
		_currentFarmUI = container;
		AddChild(container);
	}

	private void CreateDefaultRewardUI()
	{
		var container = new VBoxContainer();
		container.Name = "DefaultRewardUI";
		
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
		container.Alignment = BoxContainer.AlignmentMode.Center;
		container.Size = new Vector2(400, 200);
		container.Position = new Vector2(GetViewportRect().Size.X / 2 - 200, GetViewportRect().Size.Y / 2 - 100);
		
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
		container.Alignment = BoxContainer.AlignmentMode.Center;
		container.Size = new Vector2(300, 200);
		container.Position = new Vector2(GetViewportRect().Size.X / 2 - 150, GetViewportRect().Size.Y / 2 - 100);
		
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
		
		// 清空现有手牌
		foreach (var cardUI in _cardUIs)
		{
			cardUI.QueueFree();
		}
		_cardUIs.Clear();
		
		// 显示手牌（最多显示5张）
		var playerData = _gameManager.PlayerData;
		int cardCount = Math.Min(5, playerData.Deck.Count);
		
		for (int i = 0; i < cardCount; i++)
		{
			var cardId = playerData.Deck[i];
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
		
		// 更新战斗UI中的敌人信息
		if (_currentCombatUI != null)
		{
			var enemyInfo = _currentCombatUI.GetNode<Label>("EnemyInfo");
			if (enemyInfo != null)
			{
				var enemyData = _dataManager?.GetEnemy(enemyId);
				if (enemyData != null)
				{
					enemyInfo.Text = $"敌人: {enemyData.Name} (生命: {enemyData.Health})";
				}
			}
		}
	}

	private void OnCardPlayed(string cardId, string targetId)
	{
		ShowNotification($"使用卡牌: {cardId}");
		UpdateHandCards();
	}

	private void OnPlayerDamaged(int damage)
	{
		ShowNotification($"受到 {damage} 点伤害!");
		UpdatePlayerStats();
		
		// 伤害数字效果
		if (_playerHealthLabel != null)
		{
			ShowDamageEffect(damage, _playerHealthLabel.GlobalPosition, false);
		}
	}

	private void OnEnemyDamaged(string enemyId, int damage)
	{
		ShowNotification($"对敌人造成 {damage} 点伤害");
		
		// 伤害数字效果
		if (_currentCombatUI != null)
		{
			var enemyInfo = _currentCombatUI.GetNode<Label>("EnemyInfo");
			if (enemyInfo != null)
			{
				ShowDamageEffect(damage, enemyInfo.GlobalPosition, true);
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
		
		// 禁用所有卡牌交互
		foreach (var cardUI in _cardUIs)
		{
			cardUI.SetInteractable(false);
		}
	}

	private void OnPlayerTurnStarted()
	{
		ShowNotification("你的回合");
		
		// 启用所有卡牌交互
		foreach (var cardUI in _cardUIs)
		{
			cardUI.SetInteractable(true);
		}
	}

	private void OnCropPlanted(string cropId, int plotIndex)
	{
		ShowNotification($"种植了作物: {cropId}");
	}

	private void OnCropHarvested(string cropId, int plotIndex, CropReward reward)
	{
		ShowNotification($"收获了作物: {cropId}");
	}

	private void OnRoomEntered(RoomData room)
	{
		ShowNotification($"进入房间: {room.Type}");
		
		// 更新地图UI中的房间信息
		if (_currentMapUI != null)
		{
			var roomInfo = _currentMapUI.GetNode<Label>("RoomInfo");
			if (roomInfo != null)
			{
				roomInfo.Text = $"当前房间: {room.Type}";
			}
		}
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

	private void OnResourceAppliedToCrop(int plotIndex, string resourceId)
	{
		var resourceSystem = GameRoot.Instance?.CombatResourceSystem;
		if (resourceSystem != null)
		{
			var resourceData = resourceSystem.GetResourceData(resourceId);
			if (resourceData != null)
			{
				ShowNotification($"对地块 {plotIndex + 1} 使用了 {resourceData.Name}");
			}
		}
	}

	#endregion

	#region UI按钮回调方法

	private void OnStartButtonPressed()
	{
		if (_gameManager != null)
		{
			_gameManager.StartNewGame();
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
		// 这里应该调用战斗系统的结束回合方法
		ShowNotification("结束回合");
		EventBus.Instance?.EmitTurnEnded();
	}

	private void OnMoveButtonPressed(Vector2I direction)
	{
		ShowNotification($"向 {direction} 移动");
		// 这里应该调用地图系统的移动方法
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
		
		// 这里应该调用战斗系统的出牌方法
		// EventBus.Instance?.EmitCardPlayed(cardUI.CardData.Id, "target");
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
