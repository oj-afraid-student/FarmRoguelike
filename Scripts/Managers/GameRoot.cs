// GameRoot.cs - 主入口点
using Godot;
using System;

public partial class GameRoot : Node
{
    public static GameRoot Instance { get; private set; }
    
    public EventBus EventBus { get; private set; }
    public GameManager GameManager { get; private set; }
    public DataManager DataManager { get; private set; }
    public CropEffectSystem CropEffectSystem { get; private set; }
    public FarmingSystem FarmingSystem { get; private set; }
    public EnderChestSystem EnderChestSystem { get; private set; }
    public CombatResourceSystem CombatResourceSystem { get; private set; }
    public CombatSystem CombatSystem { get; private set; }
    public MapSystem MapSystem { get; private set; }
    public SaveManager SaveManager { get; private set; }

    // 把所有装载工作提前到 _EnterTree，确保 UI 启动时 100% 能拿到数据！
    public override void _EnterTree()
    {
        Instance = this;
        
        // 获取 Godot 的全局 Autoload 单例
        EventBus = GetNode<EventBus>("/root/EventBus");
        DataManager = GetNode<DataManager>("/root/DataManager");
        GameManager = GetNode<GameManager>("/root/GameManager");
        
        // 动态创建子系统
        CropEffectSystem = new CropEffectSystem();
        FarmingSystem = new FarmingSystem();
        EnderChestSystem = new EnderChestSystem();
        CombatResourceSystem = new CombatResourceSystem();
        CombatSystem = new CombatSystem();
        MapSystem = new MapSystem();
        SaveManager = new SaveManager();
        
        // 挂载到树上
        AddChild(CropEffectSystem);
        AddChild(FarmingSystem);
        AddChild(EnderChestSystem);
        AddChild(CombatResourceSystem);
        AddChild(CombatSystem);
        AddChild(MapSystem);
        AddChild(SaveManager);
    }

    public override void _Ready()
    {
        GD.Print("GameRoot完成，所有系统已就绪");
    }
}