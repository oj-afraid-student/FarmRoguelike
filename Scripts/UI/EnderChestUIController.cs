// EnderChestUIController.cs - 末影箱UI控制器
using System;
using System.Collections.Generic;
using Godot;

public partial class EnderChestUIController : Control
{
    // UI元素引用（需要在编辑器中设置）
    [Export] private VBoxContainer _cropsContainer; // 作物选项容器
    [Export] private Label _titleLabel; // 标题标签
    [Export] private Label _descriptionLabel; // 描述标签
    [Export] private Button _closeButton; // 关闭按钮（跳过）
    [Export] private Label _playerGoldLabel; // 玩家金币标签
    [Export] private Label _playerHealthLabel; // 玩家生命标签
    
    // 管理器引用
    private GameManager _gameManager;
    private DataManager _dataManager;
    private EnderChestSystem _enderChestSystem;
    private EventBus _eventBus;
    
    // 当前末影箱数据
    private EnderChestData _currentChest;
    
    public override void _Ready()
    {
        InitializeManagers();
        SetupUI();
        SubscribeToEvents();
        
        GD.Print("EnderChestUIController 初始化完成");
    }
    
    private void InitializeManagers()
    {
        _gameManager = GameRoot.Instance?.GameManager;
        _dataManager = GameRoot.Instance?.DataManager;
        _enderChestSystem = GameRoot.Instance?.EnderChestSystem;
        _eventBus = GameRoot.Instance?.EventBus;
        
        if (_gameManager == null || _enderChestSystem == null)
        {
            GD.PrintErr("EnderChestUIController: 管理器未找到");
        }
    }
    
    private void SetupUI()
    {
        // 设置标题
        if (_titleLabel != null)
        {
            _titleLabel.Text = "末影箱";
        }
        
        if (_descriptionLabel != null)
        {
            _descriptionLabel.Text = "选择你想要的作物，但需要付出代价...";
        }
        
        // 连接关闭按钮（若 Export 未绑定，尝试在场景中查找）
        if (_closeButton == null)
        {
            _closeButton = GetNodeOrNull<Button>("CenterContainer/MainContainer/CloseButton")
                ?? GetNodeOrNull<Button>("CenterContainer/CloseButton")
                ?? GetNodeOrNull<Button>("CenterContainer/MainContainer/PlayerInfo/CloseButton")
                ?? FindCloseButtonLocal(this);
        }
        if (_closeButton != null)
        {
            _closeButton.Pressed += OnCloseButtonPressed;
            GD.Print("EnderChestUIController: 关闭按钮已连接");
        }
        else
        {
            GD.PrintErr("EnderChestUIController: 关闭按钮未找到，请检查场景中是否存在 CloseButton");
        }
        
        // 更新玩家信息
        UpdatePlayerInfo();
    }
    
    private void SubscribeToEvents()
    {
        if (_eventBus == null)
            return;
        
        _eventBus.EnderChestOpened += OnEnderChestOpened;
        _eventBus.CropSelectedFromChest += OnCropSelectedFromChest;
        _eventBus.StatUpdated += OnStatUpdated;
    }
    
    /// <summary>
    /// 显示末影箱内容
    /// </summary>
    public void ShowEnderChest(EnderChestData chestData)
    {
        _currentChest = chestData;
        
        if (_currentChest == null)
        {
            GD.PrintErr("末影箱数据为空");
            return;
        }
        
        UpdateCropsUI();
        UpdatePlayerInfo();
        
        // 显示UI
        Visible = true;
    }
    
    private void UpdateCropsUI()
    {
        // 若 Export 未解析到（如场景结构变更后），尝试按路径查找
        var container = _cropsContainer
            ?? GetNodeOrNull<VBoxContainer>("CenterContainer/MainContainer/ScrollContainer/CropsContainer")
            ?? GetNodeOrNull<VBoxContainer>("CenterContainer/MainContainer/PlayerInfo/ScrollContainer/CropsContainer");
        if (container == null || _currentChest == null)
        {
            if (container == null)
                GD.PrintErr("EnderChestUIController: CropsContainer 未找到，请检查场景中 MainContainer/ScrollContainer/CropsContainer 是否存在");
            return;
        }
        
        // 清除现有选项
        foreach (var child in container.GetChildren())
        {
            child.QueueFree();
        }
        
        // 添加标题
        var titleLabel = new Label();
        titleLabel.Text = $"第 {_currentChest.FloorNumber} 层奖励";
        titleLabel.AddThemeFontSizeOverride("font_size", 20);
        container.AddChild(titleLabel);
        
        // 添加作物选项
        foreach (var option in _currentChest.AvailableCrops)
        {
            CreateCropOptionUI(option, container);
        }
        
        // 如果没有可用选项
        if (_currentChest.AvailableCrops.Count == 0)
        {
            var emptyLabel = new Label();
            emptyLabel.Text = "没有可用作物";
            emptyLabel.Modulate = new Color(0.7f, 0.7f, 0.7f);
            container.AddChild(emptyLabel);
        }
    }
    
    private void CreateCropOptionUI(EnderChestCropOption option, VBoxContainer container)
    {
        if (option.CropData == null)
            return;
        
        // 创建选项容器
        var optionContainer = new VBoxContainer();
        optionContainer.AddThemeConstantOverride("separation", 5);
        
        // 作物信息容器
        var infoContainer = new HBoxContainer();
        
        // 作物名称和描述
        var cropInfo = new VBoxContainer();
        var nameLabel = new Label();
        nameLabel.Text = option.CropData.Name;
        nameLabel.AddThemeFontSizeOverride("font_size", 16);
        cropInfo.AddChild(nameLabel);
        
        var descLabel = new Label();
        descLabel.Text = option.CropData.Description;
        descLabel.AddThemeFontSizeOverride("font_size", 12);
        descLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        cropInfo.AddChild(descLabel);
        
        // 显示效果信息
        if (option.CropData.EffectData != null)
        {
            var effectLabel = new Label();
            effectLabel.Text = GetEffectDescription(option.CropData);
            effectLabel.AddThemeFontSizeOverride("font_size", 11);
            effectLabel.Modulate = new Color(0.8f, 0.9f, 1.0f);
            cropInfo.AddChild(effectLabel);
        }
        
        infoContainer.AddChild(cropInfo);
        
        // 代价信息
        var costContainer = new VBoxContainer();
        costContainer.Alignment = BoxContainer.AlignmentMode.End;
        
        var costLabel = new Label();
        costLabel.Text = GetCostDisplayText(option.Cost);
        costLabel.AddThemeFontSizeOverride("font_size", 14);
        
        // 根据可用性设置颜色
        if (!option.IsAvailable)
        {
            costLabel.Modulate = new Color(1.0f, 0.5f, 0.5f);
        }
        else
        {
            costLabel.Modulate = new Color(0.8f, 1.0f, 0.8f);
        }
        
        costContainer.AddChild(costLabel);
        infoContainer.AddChild(costContainer);
        
        optionContainer.AddChild(infoContainer);
        
        // 选择按钮
        var selectButton = new Button();
        selectButton.Text = option.IsAvailable ? "选择" : "资源不足";
        selectButton.Disabled = !option.IsAvailable;
        
        if (option.IsAvailable)
        {
            selectButton.Pressed += () => OnCropOptionSelected(option);
        }
        
        optionContainer.AddChild(selectButton);
        
        // 添加到容器
        container.AddChild(optionContainer);
        
        // 添加分隔线
        var separator = new HSeparator();
        container.AddChild(separator);
    }
    
    private string GetEffectDescription(CropData cropData)
    {
        if (cropData.EffectData == null)
            return "";
        
        switch (cropData.EffectType)
        {
            case GameEnums.CropEffectType.StatBoost:
                var boosts = new List<string>();
                foreach (var boost in cropData.EffectData.StatBoosts)
                {
                    boosts.Add($"{boost.Key} +{boost.Value}");
                }
                return $"效果: {string.Join(", ", boosts)}";
                
            case GameEnums.CropEffectType.CurseTrade:
                var curses = new List<string>();
                foreach (var curse in cropData.EffectData.CurseModifiers)
                {
                    curses.Add($"{curse.Key} {curse.Value * 100}%");
                }
                var trades = new List<string>();
                foreach (var trade in cropData.EffectData.TradeModifiers)
                {
                    trades.Add($"{trade.Key} +{trade.Value * 100}%");
                }
                return $"效果: {string.Join(", ", curses)} / {string.Join(", ", trades)}";
                
            case GameEnums.CropEffectType.Forget:
                return $"效果: 删除 {cropData.EffectData.CardsToRemove} 张卡牌";
                
            default:
                return "";
        }
    }
    
    private string GetCostDisplayText(EnderChestCost cost)
    {
        switch (cost.Type)
        {
            case EnderChestCostType.Gold:
                return $"💰 {cost.Value} 金币";
            case EnderChestCostType.Health:
                return $"❤️ {cost.Value} 生命值";
            case EnderChestCostType.Card:
                return $"🃏 删除1张卡牌";
            default:
                return cost.Description;
        }
    }
    
    private void UpdatePlayerInfo()
    {
        if (_gameManager == null || _gameManager.PlayerData == null)
            return;
        
        var playerData = _gameManager.PlayerData;
        
        if (_playerGoldLabel != null)
        {
            _playerGoldLabel.Text = $"金币: {playerData.Gold}";
        }
        
        if (_playerHealthLabel != null)
        {
            _playerHealthLabel.Text = $"生命: {playerData.CurrentHealth}/{playerData.MaxHealth}";
        }
    }
    
    private void OnCropOptionSelected(EnderChestCropOption option)
    {
        if (_gameManager == null)
            return;
        
        // 显示确认对话框
        var dialog = new ConfirmationDialog();
        dialog.Title = "确认选择";
        dialog.DialogText = $"确定要选择 {option.CropData.Name} 吗？\n代价: {GetCostDisplayText(option.Cost)}";
        dialog.Confirmed += () => {
            bool success = _gameManager.SelectCropFromEnderChest(option.CropId);
            if (success)
            {
                GD.Print($"成功选择作物: {option.CropData.Name}");
                // 更新UI
                UpdateCropsUI();
                UpdatePlayerInfo();
                
                // 可以选择关闭末影箱或继续选择
                // OnCloseButtonPressed();
            }
            else
            {
                GD.PrintErr("选择作物失败");
                // 显示错误提示
                ShowErrorDialog("选择失败！可能是资源不足或没有空余地块。");
            }
        };
        
        AddChild(dialog);
        dialog.PopupCentered();
    }
    
    private void ShowErrorDialog(string message)
    {
        var dialog = new AcceptDialog();
        dialog.Title = "错误";
        dialog.DialogText = message;
        AddChild(dialog);
        dialog.PopupCentered();
    }
    
    private void OnCloseButtonPressed()
    {
        GD.Print("关闭末影箱UI");
        // 关闭末影箱
        if (_enderChestSystem != null)
        {
            _enderChestSystem.CloseEnderChest();
        }
        
        // 隐藏UI
        Visible = false;
        
        // 通知末影箱已关闭（供上层系统决定后续流程，例如在击败Boss后弹出返回农场对话）
        _eventBus?.EmitEnderChestClosed();
        
        // 可以选择不销毁，以便下次使用
        // QueueFree();
    }



    // 事件处理

    private Button FindCloseButtonLocal(Node root)
    {
        if (root == null) return null;
        foreach (Node child in root.GetChildren())
        {
            if (child is Button b)
            {
                if (!string.IsNullOrEmpty(b.Text) && (b.Text.Contains("关") || b.Text.Equals("Close", StringComparison.OrdinalIgnoreCase)))
                    return b;
                var btnName = b.Name.ToString();
                if (!string.IsNullOrEmpty(btnName) && (btnName.Contains("Close") || btnName.Contains("关闭")))
                    return b;
            }
            else
            {
                var found = FindCloseButtonLocal(child);
                if (found != null) return found;
            }
        }
        return null;
    }

    private void OnEnderChestOpened(EnderChestData chestData)
    {
        ShowEnderChest(chestData);
    }
    
    private void OnCropSelectedFromChest(string cropId, float costValue)
    {
        GD.Print($"从末影箱选择作物: {cropId}，代价: {costValue}");
        UpdatePlayerInfo();
    }
    
    private void OnStatUpdated(GameEnums.PlayerStatType statType, float value)
    {
        UpdatePlayerInfo();
    }
}
