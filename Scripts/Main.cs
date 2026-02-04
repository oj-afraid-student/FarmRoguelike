using Godot;

public partial class Main : Node
{
    public override void _Ready()
    {
        GD.Print("游戏启动...");
        
        // 验证所有管理器已加载
        PrintManagersStatus();
        
        // 启动游戏
        StartGame();
    }
    
    private void PrintManagersStatus()
    {
        GD.Print("=== 管理器状态 ===");
        
        var managers = new Node[]
        {
            GetNode("/root/EventBus"),
            GetNode("/root/DataManager"),
            GetNode("/root/GameManager"),
            // GetNode("/root/SaveManager"),
            // GetNode("/root/AudioManager"),
            GetNode("/root/UIManager")
        };
        
        foreach (var manager in managers)
        {
            if (manager != null)
            {
                GD.Print($"✓ {manager.Name} 已加载");
            }
            else
            {
                GD.PrintErr($"✗ 管理器未找到: {manager?.Name}");
            }
        }
    }
    
    private void StartGame()
    {
        var gameManager = GetNode<GameManager>("/root/GameManager");
        if (gameManager != null)
        {
            gameManager.StartNewGame();
            GD.Print("新游戏开始");
        }
        else
        {
            GD.PrintErr("GameManager 未找到，无法启动游戏");
        }
    }
    
    public override void _Process(double delta)
    {
        // 测试按键
        HandleTestInput();
    }
    
    private void HandleTestInput()
    {
        if (Input.IsActionJustPressed("ui_accept"))
        {
            GD.Print("空格键按下 - 模拟游戏事件");
            
            var gameManager = GetNode<GameManager>("/root/GameManager");
            if (gameManager != null)
            {
                // 模拟玩家受伤
                gameManager.ApplyDamageToPlayer(8);
                GD.Print($"玩家生命: {gameManager.PlayerData.CurrentHealth}/{gameManager.PlayerData.MaxHealth}");
                
                // 模拟获得金币
                gameManager.PlayerData.Gold += 5;
                GD.Print($"获得5金币，当前: {gameManager.PlayerData.Gold}");
                
                // 触发事件测试
                EventBus.Instance?.EmitPlayerDamaged(8);
            }
        }
        
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            var gameManager = GetNode<GameManager>("/root/GameManager");
            gameManager?.TogglePause();
        }
        
     
    }
}