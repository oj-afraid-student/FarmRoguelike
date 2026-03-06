using Godot;

public partial class CombatUITestHelper : Node
{
    private GameManager _gameManager;
    private CombatSystem _combatSystem;

    public override void _Ready()
    {
        GD.Print("CombatUITestHelper: 准备测试战斗界面...");

        // 1. 拿到全局 GameManager（在 project.godot 里是 Autoload）
        _gameManager = GetNodeOrNull<GameManager>("/root/GameManager");
        if (_gameManager == null)
        {
            GD.PrintErr("CombatUITestHelper: /root/GameManager 未找到");
            return;
        }

        // 2. 启动一局新游戏（会初始化玩家数据、卡组等）
        _gameManager.StartNewGame();

        // 3. 切换到战斗状态，让 UIManager 显示 CombatUI
        _gameManager.ChangeState(GameEnums.GameState.Combat);

        // 4. 找到 CombatSystem（挂在 Main 场景的 GameRoot 下面）
        _combatSystem = GetNodeOrNull<CombatSystem>("/root/Main/GameRoot/CombatSystem");
        if (_combatSystem == null)
        {
            GD.PrintErr("CombatUITestHelper: CombatSystem 未找到（/root/Main/GameRoot/CombatSystem）");
            return;
        }

        // 5. 开始一场测试战斗，敌人 Id 用你 DataManager 里的 enemy_slime
        _combatSystem.StartCombat("enemy_slime");

        GD.Print("CombatUITestHelper: 已切换到战斗并启动测试战斗。");
        GD.Print("CombatUITestHelper: 按 F5 查看当前手牌，按 F6 模拟打一整回合。");
    }

    public override void _Process(double delta)
    {
        if (_combatSystem == null)
            return;

        // F5：打印当前手牌（按一下再松开，避免连触发）
        if (Input.IsKeyPressed(Key.F5))
        {
            PrintCurrentHand();
        }

        // F6：模拟玩家打一整回合（把当前行动点用完）
        if (Input.IsKeyPressed(Key.F6))
        {
            SimulateOnePlayerTurn();
        }
    }

    private void PrintCurrentHand()
    {
        var hand = _combatSystem.GetPlayerHand();
        GD.Print($"当前手牌数量: {hand.Count}");
        for (int i = 0; i < hand.Count; i++)
        {
            GD.Print($"  [{i}] {hand[i]}");
        }
    }

    private void SimulateOnePlayerTurn()
    {
        if (!_combatSystem.IsPlayerTurn())
        {
            GD.Print("现在不是玩家回合，无法测试出牌。");
            return;
        }

        var hand = _combatSystem.GetPlayerHand();
        if (hand.Count == 0)
        {
            GD.Print("手牌为空，无法测试出牌。");
            return;
        }

        int ap = _combatSystem.GetPlayerEnergy();
        GD.Print($"模拟一个玩家回合：当前行动点 {ap}，手牌 {hand.Count} 张。");

        // 依次打出手牌，直到行动点用完或没有牌
        for (int i = 0; i < ap; i++)
        {
            hand = _combatSystem.GetPlayerHand(); // 每次重新取，避免索引越界
            if (hand.Count == 0)
            {
                GD.Print("手牌用完了。");
                break;
            }

            var cardId = hand[0];
            GD.Print($"  打出卡牌: {cardId}");
            _combatSystem.PlayCard(cardId);
        }
    }
}