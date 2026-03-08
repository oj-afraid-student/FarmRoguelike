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
	[Export] private bool _forceUseDefaultFarmUI = true;

	
	// 当前UI实例
	private Control _currentCombatUI;
	private Control _currentFarmUI;
	private Control _currentMapUI;
	private Control _currentRewardUI;
	private Control _currentMainMenu;
	private Control _currentGameOver;
	private Control _currentPauseMenu;
	private Control _currentEnderChestUI;
	private Control _currentSaveLoadUI;   // 存档/读档界面实例

	
	// 画布层保证渲染在其上
	private CanvasLayer _uiLayer;
	
	// UI元素引用（通过Export在编辑器中设置）
	[Export] private Label _playerHealthLabel;
	private Label _combatPlayerHealthLabel; // 仅在默认战斗UI创建时赋值

	[Export] private Label _playerGoldLabel;
	private Label _combatPlayerShieldLabel; // 显示本回合护甲/护盾
	private Label _combatPlayerEnergyLabel; // 当前战斗能量显示
	[Export] private Label _floorLabel;
	[Export] private Label _actionPointsLabel;
	[Export] private HBoxContainer _handContainer;
	[Export] private ProgressBar _healthBar;
	private ProgressBar _combatPlayerHealthBar;
	private TextureButton _drawPileButton;
	private TextureButton _discardPileButton;
	private Label _drawPileLabel;
	private Label _discardPileLabel;
	[Export] private Control _notificationPanel;
	[Export] private Label _notificationLabel;
	
	// 游戏管理器引用
	private GameManager _gameManager;
	private DataManager _dataManager;
	
	// UI状态
	private bool _isInitialized = false;
	private bool _isSaveMode = true;       // true=存档模式，false=读档模式
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
		
		if (ResourceLoader.Exists("res://UI/MapUI.tscn"))
		{
			_mapUIScene = GD.Load<PackedScene>("res://UI/MapUI.tscn");
		}
		
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
		eventBus.MapVisualsUpdateRequested += OnMapVisualsUpdateRequested;
		
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

			// 场景版地图 UI：绑定暂停按钮、方向键和左侧行动面板按钮
			BindMapUISceneControls();
		}
		else
		{
			
			CreateDefaultMapUI();
			GD.Print("地图UI场景未加载，显示默认UI");
		}
		
		// 注意：这里必须等到场景树更新之后再重新绘制地图，否则刚创建的 MapGrid 节点尚未被
		// Godot 树接管，而前一个 MapUI 的 QueueFree 可能还未被释放，会导致节点冲突
		// 使用 CallDeferred 把地图重绘推迟到下一帧执行
		CallDeferred("_RefreshMapUI");
		ShowNotification("探索地图中...");
	}

	private void BindMapUISceneControls()
	{
		if (_currentMapUI == null)
		{
			return;
		}

		// 绑定场景版暂停按钮，修复点击无响应。
		var pauseButton = _currentMapUI.GetNodeOrNull<Button>("CenterBox/BgPanel/MarginBox/VBoxContainer/TopBar/PauseButton")
			?? FindButtonByName(_currentMapUI, "PauseButton");
		if (pauseButton != null)
		{
			pauseButton.Pressed -= OnMapPauseButtonPressed;
			pauseButton.Pressed += OnMapPauseButtonPressed;
		}

		// 场景里如果没有方向按钮，运行时补齐并复用现有移动逻辑。
		EnsureMapSceneMoveButtons();
		UpdateMapNavigation();
	}

	private void EnsureMapSceneMoveButtons()
	{
		if (_currentMapUI == null)
		{
			return;
		}

		var existing = _currentMapUI.GetNodeOrNull<Control>("CenterBox/BgPanel/MarginBox/VBoxContainer/DPadCenterer/MoveButtons")
			?? _currentMapUI.GetNodeOrNull<Control>("CenterBox/BgPanel/MarginBox/DPadCenterer/MoveButtons")
			?? FindControlByName<Control>(_currentMapUI, "MoveButtons");
		if (existing != null)
		{
			return;
		}

		var viewportSize = GetViewport().GetVisibleRect().Size;
		float navButtonW = Mathf.Clamp(viewportSize.X * 0.055f, 56f, 100f);
		float navButtonH = Mathf.Clamp(viewportSize.Y * 0.05f, 42f, 72f);
		int navGap = (int)Mathf.Clamp(viewportSize.X * 0.008f, 8f, 18f);

		AttachFixedMapDPad(_currentMapUI, navButtonW, navButtonH, navGap);
	}

	private void AttachFixedMapDPad(Control root, float navButtonW, float navButtonH, int navGap)
	{
		if (root == null)
		{
			return;
		}

		var dpadLayer = new Control();
		dpadLayer.Name = "DPadLayer";
		dpadLayer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		dpadLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
		root.AddChild(dpadLayer);

		var dpadCenterer = new CenterContainer();
		dpadCenterer.Name = "DPadCenterer";
		dpadCenterer.AnchorLeft = 0.5f;
		dpadCenterer.AnchorTop = 1.0f;
		dpadCenterer.AnchorRight = 0.5f;
		dpadCenterer.AnchorBottom = 1.0f;
		dpadCenterer.OffsetLeft = -140f;
		dpadCenterer.OffsetTop = -220f;
		dpadCenterer.OffsetRight = 140f;
		dpadCenterer.OffsetBottom = -92f;
		dpadCenterer.MouseFilter = Control.MouseFilterEnum.Ignore;
		dpadLayer.AddChild(dpadCenterer);

		var moveButtons = new GridContainer();
		moveButtons.Name = "MoveButtons";
		moveButtons.Columns = 3;
		moveButtons.MouseFilter = Control.MouseFilterEnum.Stop;
		moveButtons.AddThemeConstantOverride("h_separation", navGap);
		moveButtons.AddThemeConstantOverride("v_separation", navGap);
		dpadCenterer.AddChild(moveButtons);

		moveButtons.AddChild(new Control());
		var upButton = new Button { Name = "UpButton", Text = "上", CustomMinimumSize = new Vector2(navButtonW, navButtonH) };
		upButton.Pressed += () => OnMoveButtonPressed(new Vector2I(0, -1));
		moveButtons.AddChild(upButton);
		moveButtons.AddChild(new Control());

		var leftButton = new Button { Name = "LeftButton", Text = "左", CustomMinimumSize = new Vector2(navButtonW, navButtonH) };
		leftButton.Pressed += () => OnMoveButtonPressed(new Vector2I(-1, 0));
		moveButtons.AddChild(leftButton);
		moveButtons.AddChild(new Control());
		var rightButton = new Button { Name = "RightButton", Text = "右", CustomMinimumSize = new Vector2(navButtonW, navButtonH) };
		rightButton.Pressed += () => OnMoveButtonPressed(new Vector2I(1, 0));
		moveButtons.AddChild(rightButton);

		moveButtons.AddChild(new Control());
		var downButton = new Button { Name = "DownButton", Text = "下", CustomMinimumSize = new Vector2(navButtonW, navButtonH) };
		downButton.Pressed += () => OnMoveButtonPressed(new Vector2I(0, 1));
		moveButtons.AddChild(downButton);
		moveButtons.AddChild(new Control());
	}

	private Button FindButtonByName(Node root, string buttonName)
	{
		if (root == null || string.IsNullOrWhiteSpace(buttonName))
		{
			return null;
		}

		if (root is Button selfButton && selfButton.Name == buttonName)
		{
			return selfButton;
		}

		foreach (Node child in root.GetChildren())
		{
			if (child is Button b && b.Name == buttonName)
			{
				return b;
			}

			var found = FindButtonByName(child, buttonName);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private T FindControlByName<T>(Node root, string nodeName) where T : class
	{
		if (root == null || string.IsNullOrWhiteSpace(nodeName))
		{
			return null;
		}

		if (root is T typedRoot && root.Name == nodeName)
		{
			return typedRoot;
		}

		foreach (Node child in root.GetChildren())
		{
			if (child is T typedChild && child.Name == nodeName)
			{
				return typedChild;
			}

			var found = FindControlByName<T>(child, nodeName);
			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private void _RefreshMapUI()
	{
		UpdateMapNavigation();
		UpdateMapVisuals();
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
		// 如果开启强制默认模式，则无论是否有 FarmUI.tscn 都走代码生成版本。
		if (_farmUIScene != null && !_forceUseDefaultFarmUI)
		{
			_currentFarmUI = _farmUIScene.Instantiate<Control>();
			_uiLayer.AddChild(_currentFarmUI);

			// 如果场景中有关闭按钮，连接其回调到 OnCloseFarmButtonPressed
			var closeBtn = FindCloseButton(_currentFarmUI);
			if (closeBtn != null)
			{
				closeBtn.Pressed += OnCloseFarmButtonPressed;
			}

			// 场景版：绑定节点引用后再初始化，避免 _plotsContainer 等为空
			BindFarmUINodesFromScene(_currentFarmUI);
			InitializeFarmPlotUIs();
		}
		else
		{
			GD.Print("使用代码生成的默认农场UI");
			CreateDefaultFarmUI();
		}
		
		ShowNotification("进入农场");
		SetupFarmUI();
	}

	private void BindFarmUINodesFromScene(Control farmRoot)
	{
		if (farmRoot == null) return;

		_plotsContainer = farmRoot.GetNodeOrNull<GridContainer>("MainContainer/PlotsSection/PlotsContainer");
		_cropSelectionContainer = farmRoot.GetNodeOrNull<VBoxContainer>("MainContainer/LeftPanel/CropSelectionSection/CropSelectionContainer");
		_activeEffectsContainer = farmRoot.GetNodeOrNull<VBoxContainer>("MainContainer/LeftPanel/EffectsSection/ActiveEffectsContainer");
		_resourcesContainer = farmRoot.GetNodeOrNull<VBoxContainer>("MainContainer/LeftPanel/ResourcesSection/ResourcesContainer");
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
		// 如果已存在暂停菜单，先销毁
		if (_currentPauseMenu != null)
		{
			_currentPauseMenu.QueueFree();
			_currentPauseMenu = null;
		}
		
		// 直接调用默认创建方法
		CreateDefaultPauseMenu();
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
		_drawPileButton = null;
		_discardPileButton = null;
		_drawPileLabel = null;
		_discardPileLabel = null;
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
			GD.Print("游戏已暂停，显示暂停菜单");
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
		plotContainer.Alignment = BoxContainer.AlignmentMode.Center;
		plotContainer.AddThemeConstantOverride("separation", 4);

		var plotButton = new TextureButton();
		plotButton.Name = "PlotButton";
		plotButton.CustomMinimumSize = new Vector2(140, 140);
		var soilTexture = TryLoadTexture("res://Assets/UI/Farm/slot_soil.png");
		if (soilTexture != null)
		{
			plotButton.TextureNormal = soilTexture;
			plotButton.StretchMode = TextureButton.StretchModeEnum.KeepAspectCovered;
		}
		plotButton.Pressed += () => OnPlotClicked(plotIndex);

		// 叠加作物贴图（根据生长阶段切换）
		var cropImage = new TextureRect();
		cropImage.Name = "CropImage";
		cropImage.Visible = false;
		cropImage.CustomMinimumSize = new Vector2(76, 76);
		cropImage.SetAnchorsPreset(Control.LayoutPreset.Center);
		cropImage.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		cropImage.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		cropImage.MouseFilter = Control.MouseFilterEnum.Ignore;
		plotButton.AddChild(cropImage);

		plotContainer.AddChild(plotButton);

		var plotIndexLabel = new Label { Name = "PlotIndexLabel", Text = $"地块 {plotIndex + 1}" };
		plotIndexLabel.HorizontalAlignment = HorizontalAlignment.Center;
		plotContainer.AddChild(plotIndexLabel);

		var cropLabel = new Label { Name = "CropLabel", Text = "空" };
		cropLabel.HorizontalAlignment = HorizontalAlignment.Center;
		plotContainer.AddChild(cropLabel);

		var progressBar = new ProgressBar
		{
			Name = "ProgressBar",
			MinValue = 0,
			MaxValue = 1,
			Value = 0
		};
		progressBar.CustomMinimumSize = new Vector2(130, 16);
		progressBar.ShowPercentage = false;
		ApplyFarmProgressBarStyle(progressBar);
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
		var cropImage = plotUI.GetNodeOrNull<TextureRect>("PlotButton/CropImage");
		var harvestButton = plotUI.GetNodeOrNull<Button>("ActionContainer/HarvestButton");
		var useResourceButton = plotUI.GetNodeOrNull<Button>("ActionContainer/UseResourceButton");

		if (plot.IsOccupied && !string.IsNullOrEmpty(plot.CropId))
		{
			var cropData = dataMgr?.GetCrop(plot.CropId);
			if (cropData != null)
			{
				cropLabel.Text = $"{cropData.Name}\n进度: {plot.GrowthProgress * 100:F1}%";
				progressBar.Value = plot.GrowthProgress;
				if (cropImage != null)
				{
					var stageTex = ResolveCropStageTexture(plot.CropId, plot.Stage, plot.GrowthProgress);
					cropImage.Texture = stageTex;
					cropImage.Visible = stageTex != null;
				}
				harvestButton.Visible = plot.IsReady;
				useResourceButton.Visible = !plot.IsReady;
			}
		}
		else
		{
			cropLabel.Text = "空";
			progressBar.Value = 0;
			if (cropImage != null)
			{
				cropImage.Texture = null;
				cropImage.Visible = false;
			}
			harvestButton.Visible = false;
			useResourceButton.Visible = false;
		}
	}

	private Texture2D ResolveCropStageTexture(string cropId, int stageIndexFromData, float growthProgress)
	{
		if (string.IsNullOrWhiteSpace(cropId)) return null;

		var key = cropId.StartsWith("crop_", StringComparison.OrdinalIgnoreCase)
			? cropId.Substring(5)
			: cropId;

		// 固定阈值：0~33% => 1阶段，34~66% => 2阶段，67~100% => 3阶段
		int derivedStage = growthProgress <= 0.33f ? 1 : (growthProgress <= 0.66f ? 2 : 3);
		int dataStage = Mathf.Clamp(stageIndexFromData + 1, 1, 3);

		var candidates = new List<int> { derivedStage, dataStage, 1, 2, 3 };
		foreach (var stage in candidates)
		{
			string stagePath = $"res://Assets/UI/Farm/Crops/{key}{stage}.png";
			if (ResourceLoader.Exists(stagePath))
			{
				return GD.Load<Texture2D>(stagePath);
			}
		}

		// 回退：单图资源
		string fallbackPath = $"res://Assets/UI/Farm/{key}.png";
		return ResourceLoader.Exists(fallbackPath) ? GD.Load<Texture2D>(fallbackPath) : null;
	}

	private void ApplyFarmProgressBarStyle(ProgressBar progressBar)
	{
		if (progressBar == null) return;

		// 约定：进度条(1) 为填充层，进度条2 为底图。
		var fillTex = TryLoadTexture("res://Assets/UI/Farm/进度条(1).png");
		var bgTex = TryLoadTexture("res://Assets/UI/Farm/进度条2.png");

		if (bgTex != null)
		{
			var bgStyle = new StyleBoxTexture { Texture = bgTex };
			progressBar.AddThemeStyleboxOverride("background", bgStyle);
		}

		if (fillTex != null)
		{
			var fillStyle = new StyleBoxTexture { Texture = fillTex };
			progressBar.AddThemeStyleboxOverride("fill", fillStyle);
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
		var allCrops = GameRoot.Instance?.GameManager?.GetUnlockedCropsForPlanting();
		if (allCrops == null || allCrops.Count == 0)
		{
			allCrops = GameRoot.Instance?.DataManager?.GetAllCrops();
		}
		var title = new Label { Text = "选择要种植的作物:" };
		title.AddThemeFontSizeOverride("font_size", 18);
		_cropSelectionContainer.AddChild(title);
		if (allCrops != null)
		{
			foreach (var crop in allCrops)
			{
				var btn = new Button { Text = $"{crop.Name} [{crop.Rarity}]\n{crop.Description}" };
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
		
		// 在开始游戏、加载存档等按钮之后，添加：
		var saveLoadButton = new Button();
		saveLoadButton.Text = "存档/读档";
		saveLoadButton.Pressed += OnOpenSaveLoadUIPressed;
		container.AddChild(saveLoadButton);
		// 原有退出按钮...
		
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
		// 尽量使用已有 UI 素材（存在则使用贴图，否则退回为纯色背景）
		var bgTexture = TryLoadTexture("res://Assets/UI/Combat/Card/战斗背景(1).png")
			?? TryLoadTexture("res://Assets/UI/Combat/战斗背景(1).png");
		Control root;
		if (bgTexture != null)
		{
			var bg = new TextureRect();
			bg.Name = "DefaultCombatUI";
			bg.Texture = bgTexture;
			bg.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
			bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			root = bg;
		}
		else
		{
			var bg = new ColorRect();
			bg.Name = "DefaultCombatUI";
			bg.Color = new Color(0, 0, 0, 0.9f);
			bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			root = bg;
		}
		
		// 主布局改为左右分栏：左侧玩家状态与手牌，右侧敌人信息
		var viewportSize = GetViewport().GetVisibleRect().Size;
		int horizontalMargin = (int)Mathf.Clamp(viewportSize.X * 0.03f, 24f, 72f);
		int verticalMargin = (int)Mathf.Clamp(viewportSize.Y * 0.035f, 20f, 56f);
		int splitSeparation = (int)Mathf.Clamp(viewportSize.X * 0.03f, 24f, 64f);
		int handCardSpacing = (int)Mathf.Clamp(viewportSize.X * 0.014f, 20f, 36f);
		float portraitTopOffset = Mathf.Clamp(viewportSize.Y * 0.24f, 120f, 300f);
		float pileWidth = Mathf.Clamp(viewportSize.X * 0.07f, 72f, 120f);
		float pileHeight = Mathf.Clamp(viewportSize.Y * 0.16f, 92f, 168f);
		float portraitSize = Mathf.Clamp(viewportSize.Y * 0.26f, 160f, 300f);

		var mainMargin = new MarginContainer();
		mainMargin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		mainMargin.AddThemeConstantOverride("margin_left", horizontalMargin);
		mainMargin.AddThemeConstantOverride("margin_right", horizontalMargin);
		mainMargin.AddThemeConstantOverride("margin_top", verticalMargin);
		mainMargin.AddThemeConstantOverride("margin_bottom", verticalMargin);
		root.AddChild(mainMargin);

		var mainSplit = new HBoxContainer();
		mainSplit.Name = "CombatLayout";
		mainSplit.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		mainSplit.AddThemeConstantOverride("separation", splitSeparation);
		mainMargin.AddChild(mainSplit);

		var leftColumn = new VBoxContainer();
		leftColumn.Name = "PlayerColumn";
		leftColumn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		leftColumn.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		leftColumn.SizeFlagsStretchRatio = 1.6f;
		leftColumn.AddThemeConstantOverride("separation", 12);
		mainSplit.AddChild(leftColumn);

		var rightColumn = new VBoxContainer();
		rightColumn.Name = "EnemyColumn";
		rightColumn.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		rightColumn.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		rightColumn.SizeFlagsStretchRatio = 1.0f;
		rightColumn.Alignment = BoxContainer.AlignmentMode.Begin;
		rightColumn.AddThemeConstantOverride("separation", 14);
		mainSplit.AddChild(rightColumn);

		// 统一下移头像行，避免内容过于贴近顶部。
		var leftTopSpacer = new Control();
		leftTopSpacer.CustomMinimumSize = new Vector2(0, portraitTopOffset);
		leftColumn.AddChild(leftTopSpacer);

		var rightTopSpacer = new Control();
		rightTopSpacer.CustomMinimumSize = new Vector2(0, portraitTopOffset);
		rightColumn.AddChild(rightTopSpacer);

		// 左侧头像区：放置玩家图标素材
		var playerIdentity = new HBoxContainer();
		playerIdentity.Name = "PlayerIdentity";
		playerIdentity.Alignment = BoxContainer.AlignmentMode.Begin;
		playerIdentity.AddThemeConstantOverride("separation", 12);
		leftColumn.AddChild(playerIdentity);

		var playerPortrait = new TextureRect();
		playerPortrait.Name = "PlayerPortrait";
		playerPortrait.Texture = TryLoadTexture("res://Assets/UI/Combat/玩家图标.png");
		playerPortrait.CustomMinimumSize = new Vector2(portraitSize, portraitSize);
		playerPortrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		playerPortrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		playerIdentity.AddChild(playerPortrait);
		
		// 左侧：玩家状态条放在头像右侧，形成 RPG 面板布局
		var playerStats = new VBoxContainer();
		playerStats.Name = "PlayerStatsPanel";
		playerStats.Alignment = BoxContainer.AlignmentMode.Center;
		playerStats.AddThemeConstantOverride("separation", 8);
		playerIdentity.AddChild(playerStats);

		// 生命条（使用贴图样式）
		var healthBar = CreateStyledProgressBar(
			"res://Assets/UI/Combat/hp条红.png",
			"res://Assets/UI/Combat/hp条(1).png");
		if (healthBar != null)
		{
			healthBar.CustomMinimumSize = new Vector2(240, 24);
			playerStats.AddChild(healthBar);
			_combatPlayerHealthBar = healthBar;
		}
		else
		{
			_combatPlayerHealthLabel = new Label();
			_combatPlayerHealthLabel.Name = "PlayerHealthLabel";
			_combatPlayerHealthLabel.Text = "生命: --/--";
			playerStats.AddChild(_combatPlayerHealthLabel);
		}

		_combatPlayerShieldLabel = new Label();
		_combatPlayerShieldLabel.Name = "PlayerShieldLabel";
		_combatPlayerShieldLabel.Text = "护甲: 0";
		playerStats.AddChild(_combatPlayerShieldLabel);

		_combatPlayerEnergyLabel = new Label();
		_combatPlayerEnergyLabel.Name = "PlayerEnergyLabel";
		_combatPlayerEnergyLabel.Text = "能量: 3";
		playerStats.AddChild(_combatPlayerEnergyLabel);

		var handLabel = new Label();
		handLabel.Text = "手牌";
		handLabel.AddThemeFontSizeOverride("font_size", 18);
		leftColumn.AddChild(handLabel);
		
		var deckInfoBar = new HBoxContainer();
		deckInfoBar.Name = "DeckInfoBar";
		deckInfoBar.Alignment = BoxContainer.AlignmentMode.Begin;
		deckInfoBar.AddThemeConstantOverride("separation", 18);
		leftColumn.AddChild(deckInfoBar);

		var drawPanel = new VBoxContainer();
		drawPanel.Alignment = BoxContainer.AlignmentMode.Center;
		drawPanel.AddThemeConstantOverride("separation", 4);
		deckInfoBar.AddChild(drawPanel);

		_drawPileButton = new TextureButton();
		_drawPileButton.Name = "DrawPileIcon";
		_drawPileButton.CustomMinimumSize = new Vector2(pileWidth, pileHeight);
		var drawTex = TryLoadTexture("res://Assets/UI/Combat/deck_draw_pile.png");
		if (drawTex != null)
		{
			_drawPileButton.TextureNormal = drawTex;
			_drawPileButton.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
		}
		_drawPileButton.Pressed += OnDrawPilePressed;
		drawPanel.AddChild(_drawPileButton);

		_drawPileLabel = new Label();
		_drawPileLabel.Name = "DrawPileLabel";
		_drawPileLabel.HorizontalAlignment = HorizontalAlignment.Center;
		drawPanel.AddChild(_drawPileLabel);

		var handArea = new VBoxContainer();
		handArea.Name = "HandArea";
		handArea.Alignment = BoxContainer.AlignmentMode.Begin;
		handArea.AddThemeConstantOverride("separation", 8);
		handArea.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		deckInfoBar.AddChild(handArea);

		var handContainer = new HBoxContainer();
		handContainer.Name = "HandContainer";
		handContainer.Alignment = BoxContainer.AlignmentMode.Begin;
		handContainer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		handContainer.AddThemeConstantOverride("separation", handCardSpacing);
		handArea.AddChild(handContainer);

		var discardPanel = new VBoxContainer();
		discardPanel.Alignment = BoxContainer.AlignmentMode.Center;
		discardPanel.AddThemeConstantOverride("separation", 4);
		deckInfoBar.AddChild(discardPanel);

		_discardPileButton = new TextureButton();
		_discardPileButton.Name = "DiscardPileIcon";
		_discardPileButton.CustomMinimumSize = new Vector2(pileWidth, pileHeight);
		if (drawTex != null)
		{
			_discardPileButton.TextureNormal = drawTex;
			_discardPileButton.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
		}
		_discardPileButton.Pressed += OnDiscardPilePressed;
		discardPanel.AddChild(_discardPileButton);

		_discardPileLabel = new Label();
		_discardPileLabel.Name = "DiscardPileLabel";
		_discardPileLabel.HorizontalAlignment = HorizontalAlignment.Center;
		discardPanel.AddChild(_discardPileLabel);

		var endTurnButton = new Button();
		endTurnButton.Text = "结束回合";
		endTurnButton.CustomMinimumSize = new Vector2(160, 46);
		endTurnButton.Pressed += OnEndTurnButtonPressed;
		leftColumn.AddChild(endTurnButton);

		// 右侧：敌人信息区域（先头像，再文字，确保与玩家头像同水平线）
		var enemyPortrait = new TextureRect();
		enemyPortrait.Name = "EnemyPortrait";
		enemyPortrait.Texture = TryLoadTexture("res://Assets/UI/Combat/Enemy/蝗虫幼虫.png");
		enemyPortrait.CustomMinimumSize = new Vector2(portraitSize, portraitSize);
		enemyPortrait.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		enemyPortrait.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		enemyPortrait.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		enemyPortrait.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
		rightColumn.AddChild(enemyPortrait);

		var enemyTitle = new Label();
		enemyTitle.Text = "敌人";
		enemyTitle.AddThemeFontSizeOverride("font_size", 20);
		enemyTitle.HorizontalAlignment = HorizontalAlignment.Center;
		rightColumn.AddChild(enemyTitle);

		var enemyInfo = new Label();
		enemyInfo.Name = "EnemyInfo";
		enemyInfo.Text = "敌人: ???";
		enemyInfo.AutowrapMode = TextServer.AutowrapMode.Word;
		enemyInfo.HorizontalAlignment = HorizontalAlignment.Center;
		rightColumn.AddChild(enemyInfo);
		
		// 将其赋值给全局变量
		_handContainer = handContainer;
		
		_currentCombatUI = root;
		_uiLayer.AddChild(root);
	}

	private void CreateDefaultMapUI()
	{
		var viewportSize = GetViewport().GetVisibleRect().Size;
		float panelWidth = Mathf.Clamp(viewportSize.X * 0.68f, 560f, 1280f);
		float panelHeight = Mathf.Clamp(viewportSize.Y * 0.82f, 420f, 900f);
		int panelPadding = (int)Mathf.Clamp(viewportSize.X * 0.018f, 14f, 28f);
		float navButtonW = Mathf.Clamp(viewportSize.X * 0.055f, 56f, 100f);
		float navButtonH = Mathf.Clamp(viewportSize.Y * 0.05f, 42f, 72f);
		int navGap = (int)Mathf.Clamp(viewportSize.X * 0.008f, 8f, 18f);

		var container = new Control();
		container.Name = "DefaultMapUI";
		container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		
		// 添加背景图片
		var bgTex = TryLoadTexture("res://Assets/UI/Map/地图背景.png")
			?? TryLoadTexture("res://Graphics/combat.png")
			?? TryLoadTexture("res://Assets/UI/Combat/战斗背景(1).png");
		var background = new TextureRect();
		background.Texture = bgTex;
		background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		background.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		// 设置拉伸模式以铺满全屏，可以根据需要调整
		// background.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
		container.AddChild(background);
		
		var centerBox = new CenterContainer();
		centerBox.Name = "CenterBox";
		centerBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		container.AddChild(centerBox);
		
		// 加一个明显的背景底板避免和游戏画面混在一起
		var bgPanel = new ColorRect();
		bgPanel.Name = "BgPanel";
		bgPanel.Color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
		bgPanel.CustomMinimumSize = new Vector2(panelWidth, panelHeight);
		centerBox.AddChild(bgPanel);
		
		var marginBox = new MarginContainer();
		marginBox.Name = "MarginBox";
		marginBox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		marginBox.AddThemeConstantOverride("margin_left", panelPadding);
		marginBox.AddThemeConstantOverride("margin_right", panelPadding);
		marginBox.AddThemeConstantOverride("margin_top", panelPadding);
		marginBox.AddThemeConstantOverride("margin_bottom", panelPadding);
		bgPanel.AddChild(marginBox);
		
		var innerContainer = new VBoxContainer();
		innerContainer.Name = "VBoxContainer";
		innerContainer.Alignment = BoxContainer.AlignmentMode.Begin;
		innerContainer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		innerContainer.AddThemeConstantOverride("separation", 10);
		marginBox.AddChild(innerContainer);

		var topBar = new HBoxContainer();
		topBar.Name = "TopBar";
		topBar.Alignment = BoxContainer.AlignmentMode.Center;
		topBar.AddThemeConstantOverride("separation", 14);
		innerContainer.AddChild(topBar);

		var mapLabel = new Label();
		mapLabel.Text = "地图探索界面";
		mapLabel.AddThemeFontSizeOverride("font_size", (int)Mathf.Clamp(viewportSize.X * 0.016f, 18f, 30f));
		mapLabel.HorizontalAlignment = HorizontalAlignment.Center;
		topBar.AddChild(mapLabel);

		var pauseButton = new Button();
		pauseButton.Text = "暂停";
		pauseButton.CustomMinimumSize = new Vector2(navButtonW * 1.15f, navButtonH);
		pauseButton.Pressed += () => {
		if (_gameManager != null)
		{
			_gameManager.TogglePause();
		}
		};
		topBar.AddChild(pauseButton);
		
		var roomInfo = new Label();
		roomInfo.Name = "RoomInfo";
		roomInfo.Text = "当前房间: ???";
		roomInfo.HorizontalAlignment = HorizontalAlignment.Center;
		roomInfo.AddThemeFontSizeOverride("font_size", (int)Mathf.Clamp(viewportSize.X * 0.012f, 14f, 22f));
		innerContainer.AddChild(roomInfo);
		
		// 地图土壤格子区：始终放在中间并占据主要空间
		var gridCenterer = new CenterContainer();
		gridCenterer.Name = "GridCenterer";
		gridCenterer.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		gridCenterer.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		innerContainer.AddChild(gridCenterer);
		
		// 加个地图网格容器来可视化地图
		var mapGrid = new GridContainer();
		mapGrid.Name = "MapGrid";
		mapGrid.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		mapGrid.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		mapGrid.AddThemeConstantOverride("h_separation", navGap);
		mapGrid.AddThemeConstantOverride("v_separation", navGap);
		gridCenterer.AddChild(mapGrid);

		AttachFixedMapDPad(container, navButtonW, navButtonH, navGap);
		
		_currentMapUI = container;
		_uiLayer.AddChild(container);

		//添加暂停按钮

	}

	private void CreateDefaultFarmUI()
	{
		// 调整布局结构：使用全屏根节点，整体居中显示
		Control rootControl;
		var farmBg = TryLoadTexture("res://Assets/UI/Farm/farm_bg.png");
		if (farmBg != null)
		{
			var bg = new TextureRect();
			bg.Texture = farmBg;
			bg.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
			rootControl = bg;
		}
		else
		{
			var bg = new ColorRect();
			bg.Color = new Color(0, 0, 0, 0.5f);
			rootControl = bg;
		}
		rootControl.Name = "DefaultFarmUI";
		rootControl.SetAnchorsPreset(Control.LayoutPreset.FullRect);

		var mainMargin = new MarginContainer();
		mainMargin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		mainMargin.AddThemeConstantOverride("margin_left", 24);
		mainMargin.AddThemeConstantOverride("margin_right", 24);
		mainMargin.AddThemeConstantOverride("margin_top", 32);
		mainMargin.AddThemeConstantOverride("margin_bottom", 24);
		mainMargin.MouseFilter = Control.MouseFilterEnum.Ignore;
		rootControl.AddChild(mainMargin);

		var centerWrap = new CenterContainer();
		centerWrap.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		mainMargin.AddChild(centerWrap);

		var mainVBox = new VBoxContainer();
		mainVBox.Alignment = BoxContainer.AlignmentMode.Begin;
		mainVBox.AddThemeConstantOverride("separation", 14);
		centerWrap.AddChild(mainVBox);

		var farmTitleTop = new Label();
		farmTitleTop.Text = "农场";
		farmTitleTop.HorizontalAlignment = HorizontalAlignment.Center;
		farmTitleTop.AddThemeFontSizeOverride("font_size", 40);
		mainVBox.AddChild(farmTitleTop);

		var mainHBox = new HBoxContainer();
		mainHBox.AddThemeConstantOverride("separation", 50);
		mainHBox.CustomMinimumSize = new Vector2(1180, 640);
		mainVBox.AddChild(mainHBox);

		// 左侧工具列：关闭按钮
		var leftCol = new VBoxContainer();
		leftCol.Alignment = BoxContainer.AlignmentMode.Center;
		leftCol.AddThemeConstantOverride("separation", 12);
		leftCol.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		leftCol.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;
		mainHBox.AddChild(leftCol);

		var closeBtn = new Button();
		closeBtn.Text = "关闭";
		closeBtn.CustomMinimumSize = new Vector2(120, 44);
		closeBtn.Pressed += OnCloseFarmButtonPressed;
		leftCol.AddChild(closeBtn);

		// 中央地块列：保证地块网格始终位于界面中心
		var centerCol = new VBoxContainer();
		centerCol.Name = "CenterPlotsColumn";
		centerCol.Alignment = BoxContainer.AlignmentMode.Center;
		centerCol.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		centerCol.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		centerCol.AddThemeConstantOverride("separation", 10);
		mainHBox.AddChild(centerCol);

		var plotsTitle = new Label();
		plotsTitle.Text = "地块";
		plotsTitle.AddThemeFontSizeOverride("font_size", 22);
		plotsTitle.HorizontalAlignment = HorizontalAlignment.Center;
		centerCol.AddChild(plotsTitle);

		_plotsContainer = new GridContainer();
		_plotsContainer.Name = "PlotsContainer";
		_plotsContainer.Columns = 3;
		_plotsContainer.AddThemeConstantOverride("h_separation", 16);
		_plotsContainer.AddThemeConstantOverride("v_separation", 16);
		_plotsContainer.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
		centerCol.AddChild(_plotsContainer);

		// 右侧信息列：选择作物、物资效果、激活库存
		var rightCol = new VBoxContainer();
		rightCol.Alignment = BoxContainer.AlignmentMode.Begin;
		rightCol.AddThemeConstantOverride("separation", 20);
		rightCol.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
		rightCol.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
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
		 // 全屏半透明黑色背景层，阻止点击穿透
		var overlay = new ColorRect();
		overlay.Name = "PauseOverlay";
		overlay.Color = new Color(0, 0, 0, 0.8f);
		overlay.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		overlay.MouseFilter = Control.MouseFilterEnum.Stop;   // 阻止穿透
		overlay.ProcessMode = Node.ProcessModeEnum.Always;    // 确保暂停时仍处理输入

		// 居中容器用于放置菜单面板
		var centerContainer = new CenterContainer();
		centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		centerContainer.MouseFilter = Control.MouseFilterEnum.Pass; // 传递事件给子节点
		overlay.AddChild(centerContainer);

		// 菜单面板（白色圆角，带阴影和边框）
		var menuPanel = new PanelContainer();
		menuPanel.CustomMinimumSize = new Vector2(450, 350);
		
		var styleBox = new StyleBoxFlat();
		styleBox.BgColor = new Color(0.98f, 0.98f, 0.98f, 1);
		styleBox.SetCornerRadiusAll(12);
		styleBox.ShadowColor = new Color(0, 0, 0, 0.6f);
		styleBox.ShadowSize = 12;
		styleBox.ShadowOffset = new Vector2(3, 3);
		styleBox.BorderWidthTop = 2;
		styleBox.BorderWidthBottom = 2;
		styleBox.BorderWidthLeft = 2;
		styleBox.BorderWidthRight = 2;
		styleBox.BorderColor = new Color(0.4f, 0.4f, 0.4f, 1);
		menuPanel.AddThemeStyleboxOverride("panel", styleBox);

		centerContainer.AddChild(menuPanel);

		// 垂直布局放置菜单内容
		var vbox = new VBoxContainer();
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 20);
		vbox.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		vbox.CustomMinimumSize = new Vector2(300, 0);
		menuPanel.AddChild(vbox);

		// 标题
		var titleLabel = new Label();
		titleLabel.Text = "游戏暂停";
		titleLabel.AddThemeFontSizeOverride("font_size", 36);
		titleLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.2f, 0.2f));
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		vbox.AddChild(titleLabel);

		vbox.AddChild(new MarginContainer { CustomMinimumSize = new Vector2(0, 10) });

		// 按钮
		var resumeBtn = CreateMenuButton("继续游戏");
		resumeBtn.Pressed += OnResumeButtonPressed;
		vbox.AddChild(resumeBtn);

		var settingsBtn = CreateMenuButton("设置");
		settingsBtn.Pressed += OnPauseSettingsButtonPressed;
		vbox.AddChild(settingsBtn);

		var saveLoadBtn = CreateMenuButton("存档/读档");
		saveLoadBtn.Pressed += OnOpenSaveLoadUIPressed;
		vbox.AddChild(saveLoadBtn);

		var mainMenuBtn = CreateMenuButton("返回主菜单");
		mainMenuBtn.Pressed += OnPauseMainMenuButtonPressed;
		vbox.AddChild(mainMenuBtn);

		// 保存引用并添加到UI层
		_currentPauseMenu = overlay;
		_uiLayer.AddChild(overlay); 
	}

	private Button CreateMenuButton(string text)
	{
		var btn = new Button();
		btn.Text = text;
		btn.CustomMinimumSize = new Vector2(220, 50);
		btn.AddThemeFontSizeOverride("font_size", 18);
		return btn;
	}

	/// <summary>
	/// 创建默认存档/读档界面
	/// </summary>
	private void CreateDefaultSaveLoadUI()
	{
		var root = new ColorRect();
		root.Name = "DefaultSaveLoadUI";
		root.Color = new Color(0, 0, 0, 0.9f);
		root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		root.ProcessMode = Node.ProcessModeEnum.Always;

		// 主容器
		var mainVBox = new VBoxContainer();
		mainVBox.SetAnchorsPreset(Control.LayoutPreset.Center);
		mainVBox.GrowHorizontal = Control.GrowDirection.Both;
		mainVBox.GrowVertical = Control.GrowDirection.Both;
		mainVBox.Alignment = BoxContainer.AlignmentMode.Center;
		root.AddChild(mainVBox);

		// 标题
		var titleLabel = new Label();
		titleLabel.Text = _isSaveMode ? "保存游戏" : "加载游戏";
		titleLabel.AddThemeFontSizeOverride("font_size", 28);
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		mainVBox.AddChild(titleLabel);

		// 模式切换按钮组
		var modeHBox = new HBoxContainer();
		modeHBox.Alignment = BoxContainer.AlignmentMode.Center;
		modeHBox.AddThemeConstantOverride("separation", 20);
		mainVBox.AddChild(modeHBox);

		var saveModeBtn = new Button();
		saveModeBtn.Text = "存档";
		saveModeBtn.Disabled = _isSaveMode;   // 当前模式禁用对应按钮
		saveModeBtn.Pressed += () => SwitchSaveLoadMode(true);
		modeHBox.AddChild(saveModeBtn);

		var loadModeBtn = new Button();
		loadModeBtn.Text = "读档";
		loadModeBtn.Disabled = !_isSaveMode;
		loadModeBtn.Pressed += () => SwitchSaveLoadMode(false);
		modeHBox.AddChild(loadModeBtn);

		// 存档槽列表
		var slotGrid = new GridContainer();
		slotGrid.Columns = 1;   // 纵向排列
		slotGrid.AddThemeConstantOverride("v_separation", 10);
		mainVBox.AddChild(slotGrid);

		var saveManager = GameRoot.Instance?.SaveManager;
		var slotInfos = saveManager?.GetAllSlotInfos() ?? new List<SaveFileInfo>();

		for (int i = 0; i < 3; i++)
		{
			int slot = i + 1;
			var info = (slotInfos.Count > i) ? slotInfos[i] : null;

			var slotPanel = new PanelContainer();
			var styleBox = new StyleBoxFlat();
			styleBox.BgColor = new Color(0.2f, 0.2f, 0.2f, 1);
			styleBox.SetCornerRadiusAll(5);
			slotPanel.AddThemeStyleboxOverride("panel", styleBox);
			slotPanel.CustomMinimumSize = new Vector2(400, 80);
			slotPanel.MouseFilter = Control.MouseFilterEnum.Stop;

			var slotHBox = new HBoxContainer();
			slotHBox.AddThemeConstantOverride("separation", 20);
			slotPanel.AddChild(slotHBox);

			// 槽位编号
			var slotNumberLabel = new Label();
			slotNumberLabel.Text = $"槽位 {slot}";
			slotNumberLabel.AddThemeFontSizeOverride("font_size", 18);
			slotHBox.AddChild(slotNumberLabel);

			// 存档信息
			var infoLabel = new Label();
			if (info != null)
			{
				infoLabel.Text = $"{info.SaveTime:yyyy-MM-dd HH:mm}\n楼层: {info.Floor}  版本: {info.Version}";
			}
			else
			{
				infoLabel.Text = "空存档";
				infoLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
			}
			infoLabel.AddThemeFontSizeOverride("font_size", 14);
			slotHBox.AddChild(infoLabel);

			// 操作按钮
			var actionBtn = new Button();
			if (_isSaveMode)
			{
				actionBtn.Text = "保存";
				actionBtn.Pressed += () => OnSaveSlotPressed(slot);
			}
			else
			{
				actionBtn.Text = info != null ? "加载" : "无存档";
				actionBtn.Disabled = info == null;
				actionBtn.Pressed += () => OnLoadSlotPressed(slot);
			}
			slotHBox.AddChild(actionBtn);

			// 如果是存档模式且已有存档，添加“覆盖”提示（可选）
			// 这里保持简单

			slotGrid.AddChild(slotPanel);
		}

		// 关闭按钮
		var closeBtn = new Button();
		closeBtn.Text = "返回";
		closeBtn.Pressed += OnCloseSaveLoadUIPressed;
		mainVBox.AddChild(closeBtn);

		_currentSaveLoadUI = root;
		_uiLayer.AddChild(root);
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
		if (IsInstanceValid(_healthBar))
		{
			_healthBar.MaxValue = playerData.MaxHealth;
			_healthBar.Value = playerData.CurrentHealth;
		}
		
		if (IsInstanceValid(_combatPlayerHealthBar))
		{
			_combatPlayerHealthBar.MaxValue = playerData.MaxHealth;
			_combatPlayerHealthBar.Value = playerData.CurrentHealth;
		}
		
		// 更新金币显示
		if (_playerGoldLabel != null)
		{
			_playerGoldLabel.Text = $"金币: {playerData.Gold}";
		}
		
		// 更新能量显示
		if (_actionPointsLabel != null)
		{
			_actionPointsLabel.Text = $"能量: {playerData.Energy}";
		}

		// 更新默认战斗UI上的玩家生命/护盾显示（如果创建了默认战斗UI）
		// 必须用 IsInstanceValid 而不是 != null，否则在 ClearAllUI 后访问已被销毁的节点会抛异常
		var combatSys = GameRoot.Instance?.CombatSystem;
		if (IsInstanceValid(_combatPlayerHealthLabel) && playerData != null)
		{
			_combatPlayerHealthLabel.Text = $"生命: {playerData.CurrentHealth}/{playerData.MaxHealth}";
		}
		else
		{
			_combatPlayerHealthLabel = null; // 清除失效引用
		}
		if (IsInstanceValid(_combatPlayerShieldLabel) && combatSys != null)
		{
			_combatPlayerShieldLabel.Text = $"护甲: {combatSys.GetPlayerDefense()}";
		}
		else
		{
			_combatPlayerShieldLabel = null; // 清除失效引用
		}
		if (IsInstanceValid(_combatPlayerEnergyLabel) && combatSys != null)
		{
			string statusText = "";
			
			// 新增的特殊机制Buff优先显示
			int curse = combatSys.GetPlayerStatusStacksPublic("curse");
			if (curse > 0) statusText += $" 诅咒:{curse}层";
			int frenzy = combatSys.GetPlayerStatusStacksPublic("frenzy");
			if (frenzy > 0) statusText += $" 狂暴:{frenzy}层";
			int focus = combatSys.GetPlayerStatusStacksPublic("focus");
			if (focus > 0) statusText += $" 专注:{focus}层";
			int elementalAffinity = combatSys.GetPlayerStatusStacksPublic("elemental_affinity");
			if (elementalAffinity > 0) statusText += $" 元素亲和:{elementalAffinity}层";
			int comboStance = combatSys.GetPlayerStatusStacksPublic("combo_stance");
			if (comboStance > 0) statusText += $" 连击架势:{comboStance}层";
			int extraTurn = combatSys.GetPlayerStatusStacksPublic("extra_turn");
			if (extraTurn > 0) statusText += $" 时光沙漏发动!";

			int poison = combatSys.GetPlayerStatusStacksPublic("poison");
			if (poison > 0) statusText += $" 中毒:{poison}层";
			int freeze = combatSys.GetPlayerStatusStacksPublic("freeze");
			if (freeze > 0) statusText += $" 冰冻:{freeze}层";
			int burn = combatSys.GetPlayerStatusStacksPublic("burn");
			if (burn > 0) statusText += $" 燃烧:{burn}层";
			int stun = combatSys.GetPlayerStatusStacksPublic("stun");
			if (stun > 0) statusText += $" 眩晕:{stun}层";
			int vulnerable = combatSys.GetPlayerStatusStacksPublic("vulnerable");
			if (vulnerable > 0) statusText += $" 破甲:{vulnerable}层";
			int strength = combatSys.GetPlayerStatusStacksPublic("strength");
			if (strength > 0) statusText += $" 力量:{strength}层";
			int precision = combatSys.GetPlayerStatusStacksPublic("precision");
			if (precision > 0) statusText += $" 精准:{precision}层";
			int lifesteal = combatSys.GetPlayerStatusStacksPublic("lifesteal");
			if (lifesteal > 0) statusText += $" 嗜血:{lifesteal}层";
			int vigor = combatSys.GetPlayerStatusStacksPublic("vigor");
			if (vigor > 0) statusText += $" 活力:{vigor}层";
			int weak = combatSys.GetPlayerStatusStacksPublic("weak");
			if (weak > 0) statusText += $" 虚弱:{weak}层";
			int ironwall = combatSys.GetPlayerStatusStacksPublic("ironwall");
			if (ironwall > 0) statusText += $" 铁壁:{ironwall}层";
			int reflect = combatSys.GetPlayerStatusStacksPublic("reflect");
			if (reflect > 0) statusText += $" 反射:{reflect}层";
			int inspire = combatSys.GetPlayerStatusStacksPublic("inspire");
			if (inspire > 0) statusText += $" 振奋:{inspire}层";
			int disarm = combatSys.GetPlayerStatusStacksPublic("disarm");
			if (disarm > 0) statusText += $" 缴械:{disarm}层";
			int silence = combatSys.GetPlayerStatusStacksPublic("silence");
			if (silence > 0) statusText += $" 沉默:{silence}层";
			int pacifist = combatSys.GetPlayerStatusStacksPublic("pacifist");
			if (pacifist > 0) statusText += $" 和平:{pacifist}层";
			
			if (statusText != "") statusText = "\n状态:" + statusText;
			
			_combatPlayerEnergyLabel.Text = $"能量: {combatSys.GetPlayerEnergy()}{statusText}";
		}
		else
		{
			_combatPlayerEnergyLabel = null; // 清除失效引用
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

		UpdateDeckInfo();
		
		GD.Print($"手牌更新: {cardCount} 张");
	}

	private void UpdateDeckInfo()
	{
		var combatSys = GameRoot.Instance?.CombatSystem;
		if (combatSys == null) return;

		if (IsInstanceValid(_drawPileLabel))
		{
			_drawPileLabel.Text = $"牌堆 {combatSys.GetDrawPileCount()}";
		}

		if (IsInstanceValid(_discardPileLabel))
		{
			_discardPileLabel.Text = $"弃牌堆 {combatSys.GetDiscardPileCount()}";
		}
	}

	private void OnDrawPilePressed()
	{
		var combatSys = GameRoot.Instance?.CombatSystem;
		if (combatSys == null) return;
		ShowNotification($"当前牌堆剩余: {combatSys.GetDrawPileCount()}");
	}

	private void OnDiscardPilePressed()
	{
		var combatSys = GameRoot.Instance?.CombatSystem;
		if (combatSys == null) return;
		ShowNotification($"当前弃牌堆数量: {combatSys.GetDiscardPileCount()}");
	}

	private void InitializeCombatUI()
	{
		// 初始化战斗UI的特定元素
		UpdateHandCards();
		UpdateDeckInfo();
		
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
		popupContainer.ZIndex = 100; // 确保置于顶层
		
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
				var enemyPortrait = _currentCombatUI.FindChild("EnemyPortrait", true, false) as TextureRect;
				if (enemyPortrait != null)
				{
					enemyPortrait.Texture = ResolveEnemyPortraitTexture(enemyId);
				}

				string statusText = "";
				int poison = combatSys.GetEnemyStatusStacksPublic("poison");
				if (poison > 0) statusText += $" 中毒:{poison}层";
				int freeze = combatSys.GetEnemyStatusStacksPublic("freeze");
				if (freeze > 0) statusText += $" 冰冻:{freeze}层";
				int burn = combatSys.GetEnemyStatusStacksPublic("burn");
				if (burn > 0) statusText += $" 燃烧:{burn}层";
				int stun = combatSys.GetEnemyStatusStacksPublic("stun");
				if (stun > 0) statusText += $" 眩晕:{stun}层";
				int vulnerable = combatSys.GetEnemyStatusStacksPublic("vulnerable");
				if (vulnerable > 0) statusText += $" 破甲:{vulnerable}层";
				int weak = combatSys.GetEnemyStatusStacksPublic("weak");
				if (weak > 0) statusText += $" 虚弱:{weak}层";
				int disarm = combatSys.GetEnemyStatusStacksPublic("disarm");
				if (disarm > 0) statusText += $" 缴械:{disarm}层";
				int silence = combatSys.GetEnemyStatusStacksPublic("silence");
				if (silence > 0) statusText += $" 沉默:{silence}层";
				
				if (statusText != "") statusText = "\n状态:" + statusText;
				
				enemyInfo.Text = $"敌人: {enemyData.Name} (生命: {combatSys.CurrentEnemyHealth}/{combatSys.CurrentEnemyMaxHealth}) \n攻击: {enemyData.Attack} 防御: {enemyData.Defense}{statusText}";
			}
		}
	}

	private Texture2D ResolveEnemyPortraitTexture(string enemyId)
	{
		if (string.IsNullOrWhiteSpace(enemyId))
		{
			return TryLoadTexture("res://Assets/UI/Combat/Enemy/蝗虫幼虫.png");
		}

		string texturePath = enemyId switch
		{
			"enemy_slime" => "res://Assets/UI/Combat/Enemy/蝗虫幼虫.png",
			"enemy_goblin" => "res://Assets/UI/Combat/Enemy/工蝗.png",
			"enemy_green_locust" => "res://Assets/UI/Combat/Enemy/蝗虫幼虫.png",
			"enemy_locust_warrior" => "res://Assets/UI/Combat/Enemy/蝗虫武士.png",
			"enemy_venom_locust" => "res://Assets/UI/Combat/Enemy/毒液蝗.png",
			"enemy_sample_boss" => "res://Assets/UI/Combat/Enemy/蝗虫母体幼体.png",
			"enemy_locust_matriarch_juvenile" => "res://Assets/UI/Combat/Enemy/蝗虫母体幼体.png",
			"enemy_locust_matriarch_adult" => "res://Assets/UI/Combat/Enemy/蝗虫母体幼体.png",
			"enemy_dark_knight" => "res://Assets/UI/Combat/Enemy/蝗虫母体主宰.png",
			"enemy_locust_matriarch_overlord" => "res://Assets/UI/Combat/Enemy/蝗虫母体主宰.png",
			_ => "res://Assets/UI/Combat/Enemy/蝗虫幼虫.png"
		};

		return TryLoadTexture(texturePath) ?? TryLoadTexture("res://Assets/UI/Combat/Enemy/蝗虫幼虫.png");
	}

	private void OnCardPlayed(string cardId, string targetId)
	{
		ShowNotification($"使用卡牌: {cardId}");
		UpdateHandCards();
		// 卡牌可能会改变玩家的临时护甲/护盾，更新相关显示
		UpdatePlayerStats();
		UpdateCombatEnemyInfo();
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
				
				// 直接通过通用方法更新战斗信息UI，包含血量和附加状态字样显示
				UpdateCombatEnemyInfo(enemyId);
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
		// 更新地图UI中的房间信息
		if (IsInstanceValid(_currentMapUI))
		{
			var roomInfo = _currentMapUI.GetNodeOrNull<Label>("CenterBox/BgPanel/MarginBox/VBoxContainer/RoomInfo");
			if (roomInfo != null)
			{
				roomInfo.Text = $"当前房间: {room.Type}";
			}
		}
		
		// 延迟执行地图高亮，确保当前帧所有 Free() 操作完成后再重绘地图
		CallDeferred("_RefreshMapUI");
	}
	
	private void OnNotificationRequested(string message)
	{
		ShowNotification(message);
	}
	
	private void OnMapVisualsUpdateRequested()
	{
		// 延迟执行确保当前帧内所有 Free/QueueFree 操作已完成
		CallDeferred("_RefreshMapUI");
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
		// 楼层推进由 GameManager 自动处理，不再弹出“返回农场/地图”分支对话。
		_pendingBossDefeatedEnemyId = null;
		ShowNotification("Boss已击败，正在结算并准备进入下一层...");
	}

	private void OnEnderChestClosed()
	{
		// 保持接口以兼容事件回调；楼层推进已自动化。
		_pendingBossDefeatedEnemyId = null;
		
		ShowFloorStatisticsUI();
	}
	
	private void ShowFloorStatisticsUI()
	{
		if (_gameManager == null) return;
		
		var popupRoot = new Control();
		popupRoot.Name = "FloorStatisticsUI";
		popupRoot.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		
		var panel = new ColorRect();
		panel.Color = new Color(0, 0, 0, 1f); // 纯黑不透明背景
		panel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		popupRoot.AddChild(panel);
		
		var centerContainer = new CenterContainer();
		centerContainer.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		popupRoot.AddChild(centerContainer);
		
		var vbox = new VBoxContainer();
		vbox.Alignment = BoxContainer.AlignmentMode.Center;
		vbox.AddThemeConstantOverride("separation", 24);
		centerContainer.AddChild(vbox);
		
		var titleLabel = new Label();
		// 因为 OnFloorCompleted 已经提前使 CurrentFloor += 1，这里显示 CurrentFloor - 1 作为刚完成的层数
		int floorNum = Math.Max(1, _gameManager.CurrentFloor - 1);
		titleLabel.Text = $"第 {floorNum} 层 统计数据";
		titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
		titleLabel.AddThemeFontSizeOverride("font_size", 36);
		vbox.AddChild(titleLabel);
		
		// 金币获取
		var goldLabel = new Label();
		goldLabel.Text = $"获得的金币：{_gameManager.CurrentFloorGoldObtained}";
		goldLabel.HorizontalAlignment = HorizontalAlignment.Center;
		goldLabel.AddThemeFontSizeOverride("font_size", 24);
		vbox.AddChild(goldLabel);
		
		// 造成伤害
		var damageLabel = new Label();
		damageLabel.Text = $"造成的伤害：{_gameManager.CurrentFloorDamageDealt}";
		damageLabel.HorizontalAlignment = HorizontalAlignment.Center;
		damageLabel.AddThemeFontSizeOverride("font_size", 24);
		vbox.AddChild(damageLabel);
		
		// 花费时间 (格式化 mm:ss)
		var timeLabel = new Label();
		TimeSpan t = TimeSpan.FromSeconds(_gameManager.CurrentFloorTime);
		timeLabel.Text = string.Format("花费的时间：{0:D2}:{1:D2}", t.Minutes, t.Seconds);
		timeLabel.HorizontalAlignment = HorizontalAlignment.Center;
		timeLabel.AddThemeFontSizeOverride("font_size", 24);
		vbox.AddChild(timeLabel);
		
		var confirmButton = new Button();
		confirmButton.Text = "确认并进入下一层";
		confirmButton.CustomMinimumSize = new Vector2(240, 60);
		confirmButton.AddThemeFontSizeOverride("font_size", 24);
		confirmButton.Pressed += () => {
			_gameManager.ProceedToNextFloor();
			popupRoot.QueueFree();
		};
		
		var buttonBox = new CenterContainer();
		buttonBox.AddChild(confirmButton);
		vbox.AddChild(buttonBox);
		
		if (_uiLayer != null)
		{
			_uiLayer.AddChild(popupRoot);
		}
		else
		{
			AddChild(popupRoot);
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
		
		var moveButtons = _currentMapUI.GetNodeOrNull<Control>("CenterBox/BgPanel/MarginBox/VBoxContainer/DPadCenterer/MoveButtons")
			?? _currentMapUI.GetNodeOrNull<Control>("CenterBox/BgPanel/MarginBox/DPadCenterer/MoveButtons")
			?? FindControlByName<Control>(_currentMapUI, "MoveButtons");
		if (moveButtons != null)
		{
			var upButton = moveButtons.GetNodeOrNull<Button>("UpButton");
			var leftButton = moveButtons.GetNodeOrNull<Button>("LeftButton");
			var rightButton = moveButtons.GetNodeOrNull<Button>("RightButton");
			var downButton = moveButtons.GetNodeOrNull<Button>("DownButton");

			SetMoveButtonState(upButton, availableMoves.Contains(currentRoom.Position + new Vector2I(0, -1)));
			SetMoveButtonState(leftButton, availableMoves.Contains(currentRoom.Position + new Vector2I(-1, 0)));
			SetMoveButtonState(rightButton, availableMoves.Contains(currentRoom.Position + new Vector2I(1, 0)));
			SetMoveButtonState(downButton, availableMoves.Contains(currentRoom.Position + new Vector2I(0, 1)));
		}
	}

	private void SetMoveButtonState(Button button, bool canMove)
	{
		if (button == null)
		{
			return;
		}

		// 保持按钮恒定占位，避免因 Visible 切换导致布局抖动。
		button.Visible = true;
		button.Disabled = !canMove;
		button.Modulate = canMove ? Colors.White : new Color(1f, 1f, 1f, 0.45f);
	}
	
	private void UpdateMapVisuals()
	{
		if (_currentMapUI == null) return;
		var mapSystem = GameRoot.Instance?.MapSystem;
		if (mapSystem == null)
			return;

		var currentRoom = mapSystem.CurrentRoom;
		if (currentRoom == null)
			return;

		var mapGrid = _currentMapUI.GetNodeOrNull<GridContainer>("CenterBox/BgPanel/MarginBox/VBoxContainer/GridCenterer/MapGrid")
			?? FindControlByName<GridContainer>(_currentMapUI, "MapGrid");
		if (mapGrid == null)
			return;

		var mapSizeVec = mapSystem.GetMapSize();
		if (mapSizeVec == Vector2I.Zero)
		{
			return;
		}

		int mapSize = mapSizeVec.X;
		var viewportSize = GetViewport().GetVisibleRect().Size;
		int cellGap = (int)Mathf.Clamp(viewportSize.X * 0.006f, 6f, 12f);
		float gridSpan = Mathf.Min(viewportSize.X * 0.34f, viewportSize.Y * 0.36f);
		float cellSize = Mathf.Clamp((gridSpan - (mapSize - 1) * cellGap) / mapSize, 48f, 96f);

		mapGrid.Columns = mapSize;
		mapGrid.AddThemeConstantOverride("h_separation", cellGap);
		mapGrid.AddThemeConstantOverride("v_separation", cellGap);

		// 预加载格子图标（允许为空，作为纯色+文字兜底）
		var texCurrent = TryLoadTexture("res://Assets/UI/Map/icon_current.png");
		var texVisited = TryLoadTexture("res://Assets/UI/Map/icon_visited.png");
		var texBoss = TryLoadTexture("res://Assets/UI/Map/icon_boss.png");
		var texReward = TryLoadTexture("res://Assets/UI/Map/icon_reward.png");
		var texTrap = TryLoadTexture("res://Assets/UI/Map/icon_trap.png");
		var texReachable = TryLoadTexture("res://Assets/UI/Map/icon_reachable.png");

		// 必须立即释放节点，否则 GridContainer 会因为 QueueFree 的延迟导致布局完全错乱并且把新节点挤出屏幕外
		foreach (Node child in mapGrid.GetChildren())
		{
			mapGrid.RemoveChild(child);
			child.Free();
		}

		// 重新生成格子 (GridContainer 的子节点按行依次排列，所以外侧循环 Y，内侧循环 X)
		for (int y = 0; y < mapSize; y++)
		{
			for (int x = 0; x < mapSize; x++)
			{
				var pos = new Vector2I(x, y);

				var room = mapSystem.GetRoomAt(pos);

				var cell = new ColorRect();
				cell.CustomMinimumSize = new Vector2(cellSize, cellSize);

				// 图标节点：覆盖整个格子，用来显示当前位置/已访问/特殊房间的图标
				var icon = new TextureRect();
				icon.Name = "Icon";
				icon.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
				icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
				cell.AddChild(icon);

				// 文字节点：作为兜底/调试用标记
				var label = new Label();
				label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
				label.HorizontalAlignment = HorizontalAlignment.Center;
				label.VerticalAlignment = VerticalAlignment.Center;
				label.AddThemeFontSizeOverride("font_size", 15);
				cell.AddChild(label);

				if (room == null)
				{
					// 越界或者无效
					cell.Color = new Color(0, 0, 0, 0);
				}
				else
				{
					// 默认底色
					cell.Color = new Color(0.3f, 0.3f, 0.3f, 1f); // 暗黑色

					// 玩家当前所在
					if (pos == currentRoom.Position)
					{
						cell.Color = new Color(0.8f, 0.8f, 0.2f, 1f); // 亮黄色
						label.Text = "You";
						label.AddThemeColorOverride("font_color", new Color(0, 0, 0, 1));
						if (texCurrent != null)
							icon.Texture = texCurrent;
					}
					// 已访问
					else if (room.IsVisited)
					{
						cell.Color = new Color(0.2f, 0.6f, 0.2f, 1f); // 绿色
						label.Text = "√";
						if (texVisited != null)
							icon.Texture = texVisited;
					}
					// 当前节点的相邻可访问节点
					else if (currentRoom.Connections.Contains(pos))
					{
						cell.Color = new Color(0.6f, 0.6f, 0.6f, 1f); // 亮灰色
						label.Text = "?";
						if (texReachable != null)
							icon.Texture = texReachable;
					}

					// 如果是特殊房间特殊标记（在“当前位置/已访问/邻接可走”逻辑基础上再叠加）
					if (room.Type == GameEnums.RoomType.Boss)
					{
						label.Text = "B";
						if (texBoss != null)
							icon.Texture = texBoss;

						if (pos != currentRoom.Position && !room.IsVisited)
						{
							cell.Color = new Color(0.5f, 0.1f, 0.1f, 1f); // 暗红
						}
					}
					else if (room.Type == GameEnums.RoomType.Trap)
					{
						if (pos != currentRoom.Position && !room.IsVisited)
						{
							label.Text = "T";
							if (texTrap != null)
								icon.Texture = texTrap;
							cell.Color = new Color(0.5f, 0.3f, 0.1f, 1f); // 橘黑/暗橙
						}
					}
					else if (room.Type == GameEnums.RoomType.Reward)
					{
						if (pos != currentRoom.Position && !room.IsVisited)
						{
							label.Text = "R";
							if (texReward != null)
								icon.Texture = texReward;
							cell.Color = new Color(0.1f, 0.5f, 0.5f, 1f); // 蓝绿
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
		GD.Print("Resume button pressed");
		if (_gameManager != null)
		{
			_gameManager.TogglePause();
		}
		else
		{
			GD.PrintErr("_gameManager is null in OnResumeButtonPressed");
		}
	}

	private void OnPauseSettingsButtonPressed()
	{
		ShowNotification("暂停菜单设置");
		// 这里可以打开设置界面
	}

	private void OnPauseMainMenuButtonPressed()
	{
		if(_gameManager != null)
		{
			if (!_gameManager.IsPaused)
			_gameManager.TogglePause(); // 先取消暂停状态，确保状态切换逻辑正常执行
		}
		OnMainMenuButtonPressed();
	}

	private void OnMapPauseButtonPressed()
	{
		if (_gameManager != null)
		{
			_gameManager.TogglePause();
		}
	}

	private void OnMapInspectButtonPressed()
	{
		var mapSystem = GameRoot.Instance?.MapSystem;
		var room = mapSystem?.CurrentRoom;
		if (room == null)
		{
			ShowNotification("当前没有房间信息");
			return;
		}

		var pos = room.Position;
		var cleared = room.IsCleared ? "是" : "否";
		ShowNotification($"房间[{pos.X},{pos.Y}] 类型: {room.Type} 已清理: {cleared}");
	}

	private void OnMapBackToFarmButtonPressed()
	{
		if (_gameManager != null)
		{
			_gameManager.ChangeState(GameEnums.GameState.Farming);
		}
	}

	private void OnMapBackToDeckButtonPressed()
	{
		ShowNotification("返回牌组界面功能暂未实现");
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

	 /// <summary>
	/// 切换存档/读档模式
	/// </summary>
	private void SwitchSaveLoadMode(bool isSave)
	{
		if (_isSaveMode == isSave) return;
		_isSaveMode = isSave;
		// 重新创建界面
		if (_currentSaveLoadUI != null)
		{
			_currentSaveLoadUI.QueueFree();
			_currentSaveLoadUI = null;
		}
		CreateDefaultSaveLoadUI();
	}

	/// <summary>
	/// 打开存档/读档界面
	/// </summary>
	private void OnOpenSaveLoadUIPressed()
	{
		// 如果已经在存档界面，先关闭
		if (_currentSaveLoadUI != null)
		{
			_currentSaveLoadUI.QueueFree();
			_currentSaveLoadUI = null;
		}
		_isSaveMode = true;
		CreateDefaultSaveLoadUI();
	}

	/// <summary>
	/// 保存到指定槽位
	/// </summary>
	private void OnSaveSlotPressed(int slot)
	{
		var saveManager = GameRoot.Instance?.SaveManager;
		if (saveManager != null)
		{
			bool success = saveManager.SaveToSlot(slot);
			if (success)
			{
				ShowNotification($"存档成功 (槽位 {slot})");
				// 刷新界面以显示更新的时间戳
				OnCloseSaveLoadUIPressed();   // 关闭界面，也可以直接刷新
			}
			else
			{
				ShowNotification($"存档失败 (槽位 {slot})");
			}
		}
	}

	/// <summary>
	/// 从指定槽位加载
	/// </summary>
	private void OnLoadSlotPressed(int slot)
	{
		var saveManager = GameRoot.Instance?.SaveManager;
		if (saveManager != null)
		{
			bool success = saveManager.LoadFromSlot(slot);
			if (success)
			{
				ShowNotification($"读档成功 (槽位 {slot})");
				OnCloseSaveLoadUIPressed();   // 关闭存档界面
				// 游戏状态会自动切换，UIManager会响应 GameStateChanged 更新界面
			}
			else
			{
				ShowNotification($"读档失败 (槽位 {slot})");
			}
		}
	}

	/// <summary>
	/// 关闭存档/读档界面
	/// </summary>
	private void OnCloseSaveLoadUIPressed()
	{
		if (_currentSaveLoadUI != null)
		{
			_currentSaveLoadUI.QueueFree();
			_currentSaveLoadUI = null;
		}
		// 根据当前游戏状态决定返回主菜单还是暂停菜单
		if (_gameManager.CurrentState == GameEnums.GameState.MainMenu)
		{
			// 已经处于主菜单，无需额外操作
		}
		else if (_gameManager.IsPaused)
		{
			// 如果是从暂停菜单打开的，可能需要重新显示暂停菜单
			ShowPauseMenu();   // 假设已有此方法
		}
		// 否则返回到当前状态界面，不需要额外处理
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

	private Texture2D TryLoadTexture(string path)
	{
		if (string.IsNullOrWhiteSpace(path)) return null;
		if (!ResourceLoader.Exists(path)) return null;
		return GD.Load<Texture2D>(path);
	}

	private ProgressBar CreateStyledProgressBar(string fullPath, string emptyPath)
	{
		var fullTex = TryLoadTexture(fullPath);
		var emptyTex = TryLoadTexture(emptyPath);
		if (fullTex == null && emptyTex == null) return null;

		var bar = new ProgressBar();
		bar.MinValue = 0;
		bar.MaxValue = 1;
		bar.Value = 1;
		bar.ShowPercentage = false;

		if (emptyTex != null)
		{
			var emptyStyle = new StyleBoxTexture();
			emptyStyle.Texture = emptyTex;
			bar.AddThemeStyleboxOverride("background", emptyStyle);
		}
		if (fullTex != null)
		{
			var fullStyle = new StyleBoxTexture();
			fullStyle.Texture = fullTex;
			bar.AddThemeStyleboxOverride("fill", fullStyle);
		}
		return bar;
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
	
	public CardUI(CardData cardData)
	{
		CardData = cardData;
		InitializeUI();
	}
	
	private void InitializeUI()
	{
		CustomMinimumSize = new Vector2(160, 240);
		
		// 仅显示卡牌素材，不再叠加文字。
		var cardTex = LoadCardTexture();
		if (cardTex != null)
		{
			var bg = new TextureRect();
			bg.Texture = cardTex;
			bg.StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered;
			bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			AddChild(bg);
		}
		else
		{
			var fallback = new ColorRect();
			fallback.Color = new Color(0.08f, 0.08f, 0.12f, 0.95f);
			fallback.SetAnchorsPreset(Control.LayoutPreset.FullRect);
			AddChild(fallback);
		}
		
		// 点击事件
		Pressed += OnCardPressed;
	}

	private Texture2D LoadCardTexture()
	{
		var safeName = (CardData?.Name ?? string.Empty).Trim();
		if (safeName.Length == 0)
		{
			return null;
		}

		// 个别卡名与文件名存在差异，先做别名归一化。
		if (safeName == "荆棘毒刃")
		{
			safeName = "荆棘毒刀";
		}

		var candidates = new List<string>
		{
			$"res://Assets/UI/Combat/Card/{safeName}.png",
			$"res://Assets/UI/Combat/{safeName}.png"
		};

		foreach (var path in candidates)
		{
			if (ResourceLoader.Exists(path))
			{
				return GD.Load<Texture2D>(path);
			}
		}

		return null;
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
