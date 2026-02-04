# 游戏名称（待定） - 团队协作项目

## 项目简介
一款融合种地元素的卡牌回合制肉鸽游戏，玩家需要抵御蝗虫入侵并保护农场。

游戏名称（待定）/
├── .git/                    # Git目录（自动生成）
├── .gitignore              # Git忽略文件
├── project.godot           # Godot项目配置
├── 蝗灾农场防御战.csproj   # C#项目文件
├── Scenes/                 # 场景文件
│   ├── Main.tscn
│   └── DebugScene.tscn
├── Scripts/                # 脚本文件
│   ├── DataTypes/
│   │   ├── GameEnums.cs
│   │   └── GameData.cs
│   ├── Managers/
│   │   ├── EventBus.cs
│   │   ├── GameManager.cs
│   │   ├── DataManager.cs
│   │   ├── SaveManager.cs
│   │   ├── AudioManager.cs
│   │   └── UIManager.cs
│   └── Main.cs
├── UI/                     # UI场景
│   ├── CombatUI.tscn
│   ├── FarmUI.tscn
│   ├── MapUI.tscn
│   ├── RewardUI.tscn
│   ├── MainMenuUI.tscn
│   ├── GameOverUI.tscn
│   └── PauseMenuUI.tscn
├── Audio/                  # 音频文件（暂时为空）
├── Assets/                 # 美术资源（暂时为空）
└── README.md              # 项目说明文档