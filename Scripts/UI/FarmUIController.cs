// FarmUIController.cs - 作物UI控制器
using System;
using System.Collections.Generic;
using Godot;

public partial class FarmUIController : Control
{
    // UI元素引用（需要在编辑器中设置）
    [Export] private GridContainer _plotsContainer; // 地块容器
    [Export] private VBoxContainer _cropSelectionContainer; // 作物选择容器
    [Export] private VBoxContainer _activeEffectsContainer; // 激活效果容器
    [Export] private VBoxContainer _resourcesContainer; // 物资容器
    [Export] private Button _closeButton; // 关闭按钮
    [Export] private Label _titleLabel; // 标题标签
    
    // 地块UI预制体（可选，如果使用动态创建）
    [Export] private PackedScene _plotUIScene;
    
    // 管理器引用
    private GameManager _gameManager;
    private DataManager _dataManager;
    private FarmingSystem _farmingSystem;
    private CropEffectSystem _cropEffectSystem;
    private CombatResourceSystem _resourceSystem;
    private EventBus _eventBus;
    
    // UI状态
    private Dictionary<int, Control> _plotUIs = new(); // 地块索引 -> UI控件
    private int? _selectedPlotIndex = null; // 当前选中的地块
    
    public override void _Ready()
    {
        InitializeManagers();
        SetupUI();
        SubscribeToEvents();
        
        GD.Print("FarmUIController 初始化完成");
    }
    
    private void InitializeManagers()
    {
        _gameManager = GameRoot.Instance?.GameManager;
        _dataManager = GameRoot.Instance?.DataManager;
        _farmingSystem = GameRoot.Instance?.FarmingSystem;
        _cropEffectSystem = GameRoot.Instance?.CropEffectSystem;
        _resourceSystem = GameRoot.Instance?.CombatResourceSystem;
        _eventBus = GameRoot.Instance?.EventBus;
        
        if (_gameManager == null || _farmingSystem == null)
        {
            GD.PrintErr("FarmUIController: 管理器未找到");
        }
    }
    
    private void SetupUI()
    {
        // 设置标题
        if (_titleLabel != null)
        {
            _titleLabel.Text = "农场";
        }
        
        // 连接关闭按钮
        if (_closeButton != null)
        {
            _closeButton.Pressed += OnCloseButtonPressed;
        }
        
        // 初始化地块UI
        InitializePlotUIs();
        
        // 更新UI
        UpdateAllUI();
    }
    
    private void InitializePlotUIs()
    {
        if (_plotsContainer == null || _farmingSystem == null)
            return;
        
        // 清除现有UI
        foreach (var child in _plotsContainer.GetChildren())
        {
            child.QueueFree();
        }
        _plotUIs.Clear();
        
        // 为每个地块创建UI
        var plots = _farmingSystem.Plots;
        foreach (var plot in plots.Values)
        {
            CreatePlotUI(plot.Index);
        }
    }
    
    private void CreatePlotUI(int plotIndex)
    {
        // 创建地块UI容器
        var plotContainer = new VBoxContainer();
        plotContainer.Name = $"Plot_{plotIndex}";
        
        // 地块按钮
        var plotButton = new Button();
        plotButton.Text = $"地块 {plotIndex + 1}";
        plotButton.Pressed += () => OnPlotClicked(plotIndex);
        plotContainer.AddChild(plotButton);
        
        // 作物信息标签
        var cropLabel = new Label();
        cropLabel.Name = "CropLabel";
        cropLabel.Text = "空";
        plotContainer.AddChild(cropLabel);
        
        // 生长进度条
        var progressBar = new ProgressBar();
        progressBar.Name = "ProgressBar";
        progressBar.MinValue = 0;
        progressBar.MaxValue = 1;
        progressBar.Value = 0;
        plotContainer.AddChild(progressBar);
        
        // 操作按钮容器
        var actionContainer = new HBoxContainer();
        actionContainer.Name = "ActionContainer";
        
        // 收获按钮
        var harvestButton = new Button();
        harvestButton.Text = "收获";
        harvestButton.Name = "HarvestButton";
        harvestButton.Visible = false;
        harvestButton.Pressed += () => OnHarvestClicked(plotIndex);
        actionContainer.AddChild(harvestButton);
        
        // 使用物资按钮
        var useResourceButton = new Button();
        useResourceButton.Text = "使用物资";
        useResourceButton.Name = "UseResourceButton";
        useResourceButton.Visible = false;
        useResourceButton.Pressed += () => OnUseResourceClicked(plotIndex);
        actionContainer.AddChild(useResourceButton);
        
        plotContainer.AddChild(actionContainer);
        
        // 添加到容器
        _plotsContainer.AddChild(plotContainer);
        _plotUIs[plotIndex] = plotContainer;
        
        // 更新地块UI
        UpdatePlotUI(plotIndex);
    }
    
    private void UpdatePlotUI(int plotIndex)
    {
        if (!_plotUIs.ContainsKey(plotIndex) || _farmingSystem == null)
            return;
        
        var plotUI = _plotUIs[plotIndex];
        var plots = _farmingSystem.Plots;
        
        if (!plots.ContainsKey(plotIndex))
            return;
        
        var plot = plots[plotIndex];
        
        // 更新作物信息
        var cropLabel = plotUI.GetNodeOrNull<Label>("CropLabel");
        var progressBar = plotUI.GetNodeOrNull<ProgressBar>("ProgressBar");
        var harvestButton = plotUI.GetNodeOrNull<Button>("ActionContainer/HarvestButton");
        var useResourceButton = plotUI.GetNodeOrNull<Button>("ActionContainer/UseResourceButton");
        
        if (plot.IsOccupied && !string.IsNullOrEmpty(plot.CropId))
        {
            var cropData = _dataManager?.GetCrop(plot.CropId);
            if (cropData != null)
            {
                if (cropLabel != null)
                {
                    cropLabel.Text = $"{cropData.Name}\n进度: {plot.GrowthProgress * 100:F1}%";
                }
                
                if (progressBar != null)
                {
                    progressBar.Value = plot.GrowthProgress;
                }
                
                if (harvestButton != null)
                {
                    harvestButton.Visible = plot.IsReady;
                }
                
                if (useResourceButton != null)
                {
                    useResourceButton.Visible = !plot.IsReady; // 只有未成熟才能使用物资
                }
            }
        }
        else
        {
            if (cropLabel != null)
            {
                cropLabel.Text = "空";
            }
            
            if (progressBar != null)
            {
                progressBar.Value = 0;
            }
            
            if (harvestButton != null)
            {
                harvestButton.Visible = false;
            }
            
            if (useResourceButton != null)
            {
                useResourceButton.Visible = false;
            }
        }
    }
    
    private void UpdateAllUI()
    {
        if (_farmingSystem == null)
            return;
        
        // 更新所有地块UI
        var plots = _farmingSystem.Plots;
        foreach (var plot in plots.Values)
        {
            UpdatePlotUI(plot.Index);
        }
        
        // 更新激活效果列表
        UpdateActiveEffectsUI();
        
        // 更新物资列表
        UpdateResourcesUI();
    }
    
    private void UpdateActiveEffectsUI()
    {
        if (_activeEffectsContainer == null || _cropEffectSystem == null)
            return;
        
        // 清除现有效果UI
        foreach (var child in _activeEffectsContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        // 添加激活的效果
        var activeEffects = _cropEffectSystem.GetActiveEffects();
        foreach (var effect in activeEffects)
        {
            var effectLabel = new Label();
            effectLabel.Text = $"✓ {effect.CropName} ({effect.EffectType})";
            _activeEffectsContainer.AddChild(effectLabel);
        }
        
        if (activeEffects.Count == 0)
        {
            var emptyLabel = new Label();
            emptyLabel.Text = "暂无激活效果";
            emptyLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
            _activeEffectsContainer.AddChild(emptyLabel);
        }
    }
    
    private void UpdateResourcesUI()
    {
        if (_resourcesContainer == null || _resourceSystem == null)
            return;
        
        // 清除现有物资UI
        foreach (var child in _resourcesContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        // 添加玩家拥有的物资
        var resources = _resourceSystem.GetPlayerResources();
        if (resources.Count == 0)
        {
            var emptyLabel = new Label();
            emptyLabel.Text = "暂无物资";
            emptyLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
            _resourcesContainer.AddChild(emptyLabel);
        }
        else
        {
            foreach (var resource in resources)
            {
                var resourceData = _resourceSystem.GetResourceData(resource.Key);
                if (resourceData != null)
                {
                    var resourceLabel = new Label();
                    resourceLabel.Text = $"{resourceData.Name} x{resource.Value}";
                    _resourcesContainer.AddChild(resourceLabel);
                }
            }
        }
    }
    
    private void SubscribeToEvents()
    {
        if (_eventBus == null)
            return;
        
        _eventBus.CropPlanted += OnCropPlanted;
        _eventBus.CropGrowthUpdated += OnCropGrowthUpdated;
        _eventBus.CropHarvested += OnCropHarvested;
        _eventBus.CropEffectApplied += OnCropEffectApplied;
        _eventBus.ResourceAppliedToCrop += OnResourceAppliedToCrop;
        _eventBus.CombatResourcesGenerated += OnCombatResourcesGenerated;
    }
    
    private void UnsubscribeFromEvents()
    {
        if (_eventBus == null)
            return;
            
        _eventBus.CropPlanted -= OnCropPlanted;
        _eventBus.CropGrowthUpdated -= OnCropGrowthUpdated;
        _eventBus.CropHarvested -= OnCropHarvested;
        _eventBus.CropEffectApplied -= OnCropEffectApplied;
        _eventBus.ResourceAppliedToCrop -= OnResourceAppliedToCrop;
        _eventBus.CombatResourcesGenerated -= OnCombatResourcesGenerated;
    }
    
    public override void _ExitTree()
    {
        base._ExitTree();
        UnsubscribeFromEvents();
    }
    
    // UI事件处理
    private void OnPlotClicked(int plotIndex)
    {
        _selectedPlotIndex = plotIndex;
        
        var plots = _farmingSystem?.Plots;
        if (plots == null || !plots.ContainsKey(plotIndex))
            return;
        
        var plot = plots[plotIndex];
        
        if (!plot.IsOccupied)
        {
            // 显示作物选择界面
            ShowCropSelection(plotIndex);
        }
        else
        {
            // 显示作物信息
            ShowCropInfo(plotIndex);
        }
    }
    
    private void OnHarvestClicked(int plotIndex)
    {
        if (_farmingSystem == null)
            return;
        
        var reward = _farmingSystem.TryHarvestCrop(plotIndex);
        if (reward != null)
        {
            GD.Print($"收获成功！获得奖励");
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
    
    private void OnCloseButtonPressed()
    {
        // 关闭农场UI，返回地图
        if (_gameManager != null)
        {
            _gameManager.ChangeState(GameEnums.GameState.MapExploration);
        }
        
        QueueFree();
    }
    
    // 显示作物选择界面
    private void ShowCropSelection(int plotIndex)
    {
        if (_cropSelectionContainer == null || _dataManager == null)
            return;
        
        // 清除现有选项
        foreach (var child in _cropSelectionContainer.GetChildren())
        {
            child.QueueFree();
        }
        
        // 获取所有可用作物
        var allCrops = _dataManager.GetAllCrops();
        
        var titleLabel = new Label();
        titleLabel.Text = "选择要种植的作物:";
        titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _cropSelectionContainer.AddChild(titleLabel);
        
        foreach (var crop in allCrops)
        {
            var cropButton = new Button();
            cropButton.Text = $"{crop.Name}\n{crop.Description}";
            cropButton.Pressed += () => OnCropSelected(plotIndex, crop.Id);
            _cropSelectionContainer.AddChild(cropButton);
        }
        
        var cancelButton = new Button();
        cancelButton.Text = "取消";
        cancelButton.Pressed += () => {
            foreach (var child in _cropSelectionContainer.GetChildren())
            {
                child.QueueFree();
            }
        };
        _cropSelectionContainer.AddChild(cancelButton);
    }
    
    private void OnCropSelected(int plotIndex, string cropId)
    {
        if (_farmingSystem == null)
            return;
        
        bool success = _farmingSystem.TryPlantCrop(plotIndex, cropId);
        if (success)
        {
            GD.Print($"成功种植 {cropId} 到地块 {plotIndex}");
            UpdatePlotUI(plotIndex);
            
            // 清除选择界面
            if (_cropSelectionContainer != null)
            {
                foreach (var child in _cropSelectionContainer.GetChildren())
                {
                    child.QueueFree();
                }
            }
        }
        else
        {
            GD.PrintErr($"种植失败: {cropId}");
        }
    }
    
    // 显示物资选择界面
    private void ShowResourceSelection(int plotIndex)
    {
        if (_resourceSystem == null || _gameManager == null)
            return;
        
        // 创建物资选择对话框
        var dialog = new AcceptDialog();
        dialog.Title = "选择要使用的物资";
        dialog.Size = new Vector2I(400, 300);
        
        var container = new VBoxContainer();
        dialog.AddChild(container);
        
        var resources = _resourceSystem.GetPlayerResources();
        if (resources.Count == 0)
        {
            var label = new Label();
            label.Text = "没有可用物资";
            container.AddChild(label);
        }
        else
        {
            foreach (var resource in resources)
            {
                var resourceData = _resourceSystem.GetResourceData(resource.Key);
                if (resourceData != null)
                {
                    var button = new Button();
                    button.Text = $"{resourceData.Name} x{resource.Value}\n{resourceData.Description}";
                    button.Pressed += () => {
                        bool success = _gameManager.UseResourceOnCrop(resource.Key, plotIndex);
                        if (success)
                        {
                            UpdatePlotUI(plotIndex);
                            UpdateResourcesUI();
                            dialog.QueueFree();
                        }
                        else
                        {
                            GD.PrintErr("使用物资失败");
                        }
                    };
                    container.AddChild(button);
                }
            }
        }
        
        var cancelButton = new Button();
        cancelButton.Text = "取消";
        cancelButton.Pressed += () => dialog.QueueFree();
        container.AddChild(cancelButton);
        
        AddChild(dialog);
        dialog.PopupCentered();
    }
    
    private void ShowCropInfo(int plotIndex)
    {
        // 可以显示详细的作物信息对话框
        GD.Print($"显示作物信息: 地块 {plotIndex}");
    }
    
    // 事件处理
    private void OnCropPlanted(string cropId, int plotIndex)
    {
        UpdatePlotUI(plotIndex);
    }
    
    private void OnCropGrowthUpdated(string cropId, int plotIndex, float progress)
    {
        UpdatePlotUI(plotIndex);
    }
    
    private void OnCropHarvested(string cropId, int plotIndex, CropReward reward)
    {
        UpdatePlotUI(plotIndex);
    }
    
    private void OnCropEffectApplied(string cropId)
    {
        UpdateActiveEffectsUI();
    }
    
    private void OnResourceAppliedToCrop(int plotIndex, string resourceId)
    {
        UpdatePlotUI(plotIndex);
        UpdateResourcesUI();
    }
    
    private void OnCombatResourcesGenerated(Dictionary<string, int> resources)
    {
        UpdateResourcesUI();
    }
}
