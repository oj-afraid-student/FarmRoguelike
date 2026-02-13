# UI场景创建指南

## 📋 概述

本文档说明如何在Godot编辑器中创建作物系统和末影箱系统的UI场景。所有UI控制器脚本已经完成，您只需要在编辑器中创建场景并绑定脚本。

---

## 🌾 1. 农场UI场景 (FarmUI.tscn)

### 场景结构

```
FarmUI (Control)
├── TitleLabel (Label) - 标题："农场"
├── MainContainer (VBoxContainer)
│   ├── PlotsSection (VBoxContainer)
│   │   ├── SectionTitle (Label) - "地块"
│   │   └── PlotsContainer (GridContainer) - 6列，自动创建地块UI
│   ├── CropSelectionSection (VBoxContainer)
│   │   ├── SectionTitle (Label) - "选择作物"
│   │   └── CropSelectionContainer (VBoxContainer) - 动态显示作物选项
│   ├── EffectsSection (VBoxContainer)
│   │   ├── SectionTitle (Label) - "激活效果"
│   │   └── ActiveEffectsContainer (VBoxContainer) - 显示激活的作物效果
│   └── ResourcesSection (VBoxContainer)
│       ├── SectionTitle (Label) - "物资库存"
│       └── ResourcesContainer (VBoxContainer) - 显示玩家物资
└── CloseButton (Button) - "关闭"
```

### 创建步骤

1. **创建根节点**
   - 在场景树中创建 `Control` 节点，命名为 `FarmUI`
   - 设置锚点：全屏（Stretch/Rect）
   - 设置大小：1920x1080（或您的游戏分辨率）

2. **绑定脚本**
   - 选择 `FarmUI` 节点
   - 在检查器中点击"脚本"图标
   - 选择 `Scripts/UI/FarmUIController.cs`

3. **创建UI元素**
   
   **标题标签**
   - 创建 `Label` 节点，命名为 `TitleLabel`
   - 设置文本："农场"
   - 设置字体大小：24
   - 在脚本的 Export 变量中连接：`_titleLabel`

   **主容器**
   - 创建 `VBoxContainer` 节点，命名为 `MainContainer`
   - 设置布局：垂直排列
   - 设置大小：填充父节点

   **地块容器**
   - 在 `MainContainer` 下创建 `VBoxContainer`，命名为 `PlotsSection`
   - 添加 `Label` 子节点："地块"
   - 添加 `GridContainer` 子节点，命名为 `PlotsContainer`
   - 设置列数：3（2行3列显示6个地块）
   - 在脚本中连接：`_plotsContainer`

   **作物选择容器**
   - 在 `MainContainer` 下创建 `VBoxContainer`，命名为 `CropSelectionSection`
   - 添加 `Label` 子节点："选择作物"
   - 添加 `VBoxContainer` 子节点，命名为 `CropSelectionContainer`
   - 在脚本中连接：`_cropSelectionContainer`

   **效果容器**
   - 在 `MainContainer` 下创建 `VBoxContainer`，命名为 `EffectsSection`
   - 添加 `Label` 子节点："激活效果"
   - 添加 `VBoxContainer` 子节点，命名为 `ActiveEffectsContainer`
   - 在脚本中连接：`_activeEffectsContainer`

   **物资容器**
   - 在 `MainContainer` 下创建 `VBoxContainer`，命名为 `ResourcesSection`
   - 添加 `Label` 子节点："物资库存"
   - 添加 `VBoxContainer` 子节点，命名为 `ResourcesContainer`
   - 在脚本中连接：`_resourcesContainer`

   **关闭按钮**
   - 创建 `Button` 节点，命名为 `CloseButton`
   - 设置文本："关闭"
   - 设置位置：右下角
   - 在脚本中连接：`_closeButton`

4. **设置Export变量**
   - 选择 `FarmUI` 节点
   - 在检查器的脚本部分，找到所有 Export 变量
   - 将对应的UI节点拖拽到变量中

### 样式建议

- 使用 `Panel` 或 `PanelContainer` 作为背景
- 地块可以使用 `Panel` + `Button` 组合
- 添加适当的间距和边距
- 使用 `HSeparator` 分隔不同区域

---

## 📦 2. 末影箱UI场景 (EnderChestUI.tscn)

### 场景结构

```
EnderChestUI (Control)
├── Background (Panel) - 半透明背景
├── MainContainer (VBoxContainer)
│   ├── Header (HBoxContainer)
│   │   ├── TitleLabel (Label) - "末影箱"
│   │   └── CloseButton (Button) - "X"
│   ├── DescriptionLabel (Label) - 描述文本
│   ├── PlayerInfo (HBoxContainer)
│   │   ├── PlayerGoldLabel (Label) - "金币: 100"
│   │   └── PlayerHealthLabel (Label) - "生命: 80/100"
│   └── CropsContainer (VBoxContainer) - 动态显示作物选项
└── ScrollContainer (ScrollContainer) - 可选，如果选项太多
    └── CropsContainer (VBoxContainer) - 放在ScrollContainer中
```

### 创建步骤

1. **创建根节点**
   - 创建 `Control` 节点，命名为 `EnderChestUI`
   - 设置锚点：全屏
   - 设置大小：1920x1080

2. **绑定脚本**
   - 选择 `EnderChestUI` 节点
   - 绑定 `Scripts/UI/EnderChestUIController.cs`

3. **创建背景**
   - 创建 `Panel` 节点，命名为 `Background`
   - 设置颜色：半透明黑色 (0, 0, 0, 0.7)
   - 设置锚点：全屏

4. **创建主容器**
   - 创建 `VBoxContainer`，命名为 `MainContainer`
   - 设置位置：居中
   - 设置大小：800x600（或根据需要调整）

5. **创建标题栏**
   - 在 `MainContainer` 下创建 `HBoxContainer`，命名为 `Header`
   - 添加 `Label` 子节点，命名为 `TitleLabel`
   - 设置文本："末影箱"
   - 设置字体大小：24
   - 添加 `Button` 子节点，命名为 `CloseButton`
   - 设置文本："X" 或 "关闭"
   - 在脚本中连接：`_titleLabel`, `_closeButton`

6. **创建描述标签**
   - 添加 `Label` 节点，命名为 `DescriptionLabel`
   - 设置文本："选择你想要的作物，但需要付出代价..."
   - 在脚本中连接：`_descriptionLabel`

7. **创建玩家信息**
   - 创建 `HBoxContainer`，命名为 `PlayerInfo`
   - 添加两个 `Label` 节点：
     - `PlayerGoldLabel` - "金币: 100"
     - `PlayerHealthLabel` - "生命: 80/100"
   - 在脚本中连接：`_playerGoldLabel`, `_playerHealthLabel`

8. **创建作物容器**
   - 创建 `VBoxContainer`，命名为 `CropsContainer`
   - 如果需要滚动，可以放在 `ScrollContainer` 中
   - 在脚本中连接：`_cropsContainer`

9. **设置Export变量**
   - 将所有UI节点连接到脚本的Export变量

### 样式建议

- 使用 `Panel` 作为主容器背景
- 作物选项使用 `Panel` + `VBoxContainer` 组合
- 代价信息使用不同颜色区分（可用/不可用）
- 添加适当的间距和边距

---

## 🎨 3. UI样式建议

### 颜色方案

- **背景**: 深色半透明 (0, 0, 0, 0.8)
- **面板**: 深灰色 (0.2, 0.2, 0.2, 1.0)
- **文本**: 白色或浅灰色
- **按钮**: 蓝色或绿色
- **禁用按钮**: 灰色
- **警告/错误**: 红色

### 字体大小

- **标题**: 20-24
- **副标题**: 16-18
- **正文**: 12-14
- **小字**: 10-11

### 间距

- 容器间距：10-20像素
- 元素间距：5-10像素
- 边距：10-20像素

---

## 🔗 4. 集成到UIManager

### 修改UIManager.cs

在 `UIManager.cs` 的 `ShowFarmUI()` 方法中：

```csharp
private void ShowFarmUI()
{
    if (_farmUIScene != null)
    {
        _currentFarmUI = _farmUIScene.Instantiate<Control>();
        AddChild(_currentFarmUI);
    }
    else
    {
        GD.Print("农场UI场景未加载");
        // 可以创建默认UI或显示错误
    }
}
```

### 显示末影箱UI

在 `UIManager.cs` 中添加：

```csharp
private Control _currentEnderChestUI;

private void ShowEnderChestUI(EnderChestData chestData)
{
    // 加载末影箱UI场景
    if (ResourceLoader.Exists("res://UI/EnderChestUI.tscn"))
    {
        var scene = GD.Load<PackedScene>("res://UI/EnderChestUI.tscn");
        _currentEnderChestUI = scene.Instantiate<EnderChestUIController>();
        AddChild(_currentEnderChestUI);
        
        // 显示末影箱内容
        var controller = _currentEnderChestUI as EnderChestUIController;
        controller?.ShowEnderChest(chestData);
    }
}

// 在事件订阅中添加
private void SubscribeToEvents()
{
    // ... 其他事件
    _eventBus.EnderChestOpened += OnEnderChestOpened;
}

private void OnEnderChestOpened(EnderChestData chestData)
{
    ShowEnderChestUI(chestData);
}
```

---

## ✅ 5. 测试检查清单

### FarmUI测试

- [ ] 场景可以正常加载
- [ ] 6个地块正确显示
- [ ] 点击空地块显示作物选择
- [ ] 点击有作物的地块显示信息
- [ ] 收获按钮在作物成熟时显示
- [ ] 使用物资按钮正常工作
- [ ] 激活效果列表正确显示
- [ ] 物资列表正确显示
- [ ] 关闭按钮正常工作

### EnderChestUI测试

- [ ] 场景可以正常加载
- [ ] 末影箱打开时正确显示
- [ ] 作物选项正确显示
- [ ] 代价信息正确显示
- [ ] 资源不足的选项被禁用
- [ ] 选择作物后正确扣除代价
- [ ] 玩家信息正确更新
- [ ] 关闭按钮正常工作

---

## 🐛 6. 常见问题

### 问题1: Export变量无法连接

**解决方案**: 
- 确保节点名称与脚本中的变量名匹配（不区分大小写）
- 确保节点类型正确（Label, Button等）
- 在检查器的脚本部分手动拖拽节点

### 问题2: UI不显示

**解决方案**:
- 检查节点的 `Visible` 属性
- 检查节点的 `ZIndex` 或 `ZAsRelative` 属性
- 检查父节点的可见性
- 检查场景是否正确添加到场景树

### 问题3: 事件不触发

**解决方案**:
- 确保EventBus正确初始化
- 确保事件订阅在 `_Ready()` 中完成
- 检查事件名称是否正确

### 问题4: 管理器引用为null

**解决方案**:
- 确保GameRoot正确初始化
- 确保所有管理器在GameRoot中注册
- 检查节点路径是否正确

---

## 📝 7. 下一步

完成UI场景创建后：

1. 测试所有功能
2. 调整UI样式和布局
3. 添加动画效果（可选）
4. 添加音效（可选）
5. 优化性能

---

## 🎯 快速参考

### FarmUI Export变量列表
- `_plotsContainer` → GridContainer
- `_cropSelectionContainer` → VBoxContainer
- `_activeEffectsContainer` → VBoxContainer
- `_resourcesContainer` → VBoxContainer
- `_closeButton` → Button
- `_titleLabel` → Label

### EnderChestUI Export变量列表
- `_cropsContainer` → VBoxContainer
- `_titleLabel` → Label
- `_descriptionLabel` → Label
- `_closeButton` → Button
- `_playerGoldLabel` → Label
- `_playerHealthLabel` → Label
