using Godot;

/// <summary>
/// 简单的牌堆测试逻辑：
/// - 初始有 6 种卡牌名字
/// - 每次点击牌堆（DrawPileIcon）随机抽 1 张，放到手牌列中
/// - 最多抽 5 张，之后不能继续抽，牌堆变半透明
/// 仅用于 UI 演示，不影响正式战斗系统。
/// </summary>
public partial class CombatUIDeckTest : Control
{
	[Export] private TextureRect _drawPileIcon;
	[Export] private Label _drawPileLabel;
	[Export] private TextureRect _discardPileIcon;
	[Export] private Label _discardPileLabel;
	[Export] private HBoxContainer _handContainer;
	[Export] private Texture2D[] _cardTextures;

	// 六张初始卡牌名字
	private readonly string[] _cardNames =
	{
		"镰刀挥砍",
		"锄头猛击",
		"架起农具",
		"紧急包扎",
		"喷农药",
        "观察弱点"
	};

	private const int MaxDrawsPerRound = 5;
	private int _drawCount = 0;
	private int _discardCount = 0;

	/// <summary>为 true 时由 CombatSystem + UIManager 驱动手牌与弃牌，本脚本不再响应点击抽牌。</summary>
	private bool _realCombatMode;

	private readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

	/// <summary>
	/// 给真实战斗 UI 使用：根据卡牌名字返回你在 CombatUI.tscn 里绑定的贴图。
	/// </summary>
	public Texture2D GetCardTextureByName(string cardName)
	{
		if (string.IsNullOrEmpty(cardName) || _cardTextures == null)
			return null;

		for (int i = 0; i < _cardNames.Length; i++)
		{
			if (_cardNames[i] == cardName)
			{
				if (_cardTextures.Length > i)
					return _cardTextures[i];
				return null;
			}
		}

		return null;
	}

	public override void _Ready()
	{
		_rng.Randomize();

		// 若场景中存在 CombatSystem（如从 Main 进入战斗），则使用真实战斗流程：手牌与出牌由 UIManager 驱动
		var combatSystem = GetNodeOrNull<CombatSystem>("/root/Main/GameRoot/CombatSystem");
		_realCombatMode = combatSystem != null;

		// 如果没在编辑器里拖 Export，就按当前 CombatUI 场景结构自动查找
		_drawPileIcon ??=
			GetNodeOrNull<TextureRect>("MainVBox/BottomPanel/DeckInfoBar/DrawPileIcon");
		_drawPileLabel ??=
			GetNodeOrNull<Label>("MainVBox/BottomPanel/DeckInfoBar/DrawPileLabel");
		_discardPileIcon ??=
			GetNodeOrNull<TextureRect>("MainVBox/BottomPanel/DeckInfoBar/DiscardPileIcon");
		_discardPileLabel ??=
			GetNodeOrNull<Label>("MainVBox/BottomPanel/DeckInfoBar/DiscardPileLabel");
		// HandArea 已经移动到 DeckInfoBar 下，所以这里的路径也要跟着改
		_handContainer ??=
			GetNodeOrNull<HBoxContainer>("MainVBox/BottomPanel/DeckInfoBar/HandArea/HandContainer");

		if (!_realCombatMode && _drawPileIcon != null)
		{
			_drawPileIcon.GuiInput += OnDrawPileGuiInput;
		}

		if (!_realCombatMode)
		{
			UpdateDrawPileLabel();
			UpdateDiscardPileLabel();
		}
		// 真实战斗模式下牌堆/弃牌堆数量由 UIManager.UpdateDeckInfo() 根据 CombatSystem 刷新
	}

	private void OnDrawPileGuiInput(InputEvent @event)
	{
		if (_realCombatMode || _drawCount >= MaxDrawsPerRound)
			return;

		if (@event is InputEventMouseButton mouseEvent &&
			mouseEvent.ButtonIndex == MouseButton.Left &&
			mouseEvent.Pressed)
		{
			DrawOneCard();
		}
	}

	private void DrawOneCard()
	{
		if (_handContainer == null)
			return;

		_drawCount++;

		// 随机选一张卡牌
		var index = _rng.RandiRange(0, _cardNames.Length - 1);

		// 如果有绑定对应的贴图，就用原始尺寸的图片显示卡牌
		if (_cardTextures != null &&
			_cardTextures.Length > index &&
			_cardTextures[index] != null)
		{
			var cardSprite = new TextureRect
			{
				Texture = _cardTextures[index],
				// 按原图尺寸显示，只在控件区域内居中，不拉伸
				StretchMode = TextureRect.StretchModeEnum.KeepCentered
			};
			
			// 点击手牌：从手牌移除并加入弃牌堆计数
			cardSprite.GuiInput += e => OnCardInHandGuiInput(cardSprite, e);
			_handContainer.AddChild(cardSprite);
		}
		else
		{
			// 没有贴图时，用文字作为占位，方便调试
			var cardLabel = new Label
			{
				Text = _cardNames[index],
				AutowrapMode = TextServer.AutowrapMode.Word
			};
			cardLabel.CustomMinimumSize = new Vector2(160, 80);
			
			cardLabel.GuiInput += e => OnCardInHandGuiInput(cardLabel, e);
			_handContainer.AddChild(cardLabel);
		}

		UpdateDrawPileLabel();

		// 抽满 5 张后，灰掉牌堆
		if (_drawCount >= MaxDrawsPerRound && _drawPileIcon != null)
		{
			_drawPileIcon.Modulate = new Color(1f, 1f, 1f, 0.5f);
		}
	}

	private void UpdateDrawPileLabel()
	{
		if (_drawPileLabel == null)
			return;

		int remaining = MaxDrawsPerRound - _drawCount;
		_drawPileLabel.Text = $"牌堆{remaining}";
	}

	/// <summary>
	/// 更新弃牌堆数量显示。
	/// </summary>
	private void UpdateDiscardPileLabel()
	{
		if (_discardPileLabel == null)
			return;

		_discardPileLabel.Text = $"弃牌堆{_discardCount}";
	}

	/// <summary>
	/// 点击手牌：从手牌区域移除该卡，并计入弃牌堆。
	/// </summary>
	private void OnCardInHandGuiInput(Control cardNode, InputEvent @event)
	{
		if (!(@event is InputEventMouseButton mouseEvent) ||
			mouseEvent.ButtonIndex != MouseButton.Left ||
			!mouseEvent.Pressed)
			return;

		if (_handContainer != null && cardNode.GetParent() == _handContainer)
		{
			_handContainer.RemoveChild(cardNode);
			cardNode.QueueFree();
		}

		_discardCount++;
		UpdateDiscardPileLabel();

		// 弃牌堆有牌时，让图标稍微亮一点（可选）
		if (_discardPileIcon != null)
		{
			_discardPileIcon.Modulate = new Color(1f, 1f, 1f, 0.9f);
		}
	}
}
