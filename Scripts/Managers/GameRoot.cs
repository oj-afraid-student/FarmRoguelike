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
    public SaveManager SaveManager { get; private set; }
    // public AudioManager AudioManager { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        
        // 初始化管理器
        EventBus = new EventBus();
        GameManager = new GameManager();
        DataManager = new DataManager();
        CropEffectSystem = new CropEffectSystem();
        FarmingSystem = new FarmingSystem();
        EnderChestSystem = new EnderChestSystem();
        CombatResourceSystem = new CombatResourceSystem();
        SaveManager = new SaveManager();
        // AudioManager = new AudioManager();
        
        // 添加到场景树
        AddChild(EventBus);
        AddChild(GameManager);
        AddChild(DataManager);
        AddChild(CropEffectSystem);
        AddChild(FarmingSystem);
        AddChild(EnderChestSystem);
        AddChild(CombatResourceSystem);
        AddChild(SaveManager);
        // AddChild(AudioManager);
        
        GD.Print("GameRoot initialized");
    }
}