// MapUITestHelper.cs - 专门用于单独测试地图探索界面
using Godot;

public partial class MapUITestHelper : Node
{
	private GameManager _gameManager;
	private MapSystem _mapSystem;

	public override void _Ready()
	{
		_gameManager = GameRoot.Instance?.GameManager;
		_mapSystem = GameRoot.Instance?.MapSystem;

		GD.Print("MapUITestHelper 初始化完成。");
		GD.Print("按 F4 启动地图探索界面测试（调用 GameManager.StartNewGame）。");
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey key && key.Pressed && !key.Echo)
		{
			if (key.Keycode == Key.F4)
			{
				StartMapUITest();
			}
		}
	}

	/// <summary>
	/// 直接走标准新游戏流程：初始化玩家数据 + 生成地图 + 进入 MapExploration，
	/// UIManager 会自动切换到你刚做好的地图探索界面。
	/// </summary>
	public void StartMapUITest()
	{
		if (_gameManager == null || _mapSystem == null)
		{
			GD.PrintErr("MapUITestHelper: GameManager 或 MapSystem 未初始化");
			return;
		}

		_gameManager.StartNewGame();
	}
}

