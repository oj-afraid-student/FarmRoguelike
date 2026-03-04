using Godot;
using System;
using System.Collections.Generic;

public partial class CombatSystem : Node2D
{
	[Signal]
	public delegate void CombatStartedEventHandler(string enemyId);
	[Signal]
	public delegate void CombatEndedEventHandler(bool playerWon);
	
	private GameManager _gameManager;
	private EventBus _eventBus;
	private PlayerData _playerData;
	
	public string CurrentEnemyId => _currentEnemyId;
	public int CurrentEnemyHealth => _currentEnemyHealth;
	public int CurrentEnemyMaxHealth => _currentEnemyMaxHealth;
	public IReadOnlyList<string> PlayerHand => _playerHand;
	
	private string _currentEnemyId;
	private int _currentEnemyHealth = 100;
	private int _currentEnemyMaxHealth = 100;
	private int _currentEnemyAttack = 10;
	private int _currentEnemyDefense = 0;
	private int _enemyAiStateIndex = 0;
	
	private int _enemyPoisonStacks = 0;
	private int _enemyPoisonDuration = 0;
	private int _nextAttackDamageBonus = 0;
	
	private bool _isPlayerTurn = true;
	private int _playerEnergy = 3;
	private int _playerDefenseThisTurn = 0;
	
	private List<string> _playerHand = new List<string>();
	private List<string> _playerDrawPile = new List<string>();
	private List<string> _playerDiscardPile = new List<string>();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_gameManager = GameRoot.Instance?.GameManager;
		_eventBus = GameRoot.Instance?.EventBus;
		
		if (_gameManager != null)
		{
			_playerData = _gameManager.PlayerData;
		}
		
		SubscribeToEvents();
	}
	
	private void SubscribeToEvents()
	{
		if (_eventBus == null) return;
		
		_eventBus.CombatStarted += OnCombatStarted;
		_eventBus.CardPlayed += OnCardPlayed;
		_eventBus.PlayerDamaged += OnPlayerDamaged;
		_eventBus.EnemyDamaged += OnEnemyDamaged;
	}
	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void StartCombat(string enemyId)
	{
		if (_gameManager != null)
		{
			_playerData = _gameManager.PlayerData;
		}
		
		_currentEnemyId = enemyId;
		_isPlayerTurn = true;
		_playerEnergy = 3;
		_playerDefenseThisTurn = 0;
		_enemyAiStateIndex = 0;
		_enemyPoisonStacks = 0;
		_enemyPoisonDuration = 0;
		_nextAttackDamageBonus = 0;
		
		var dataManager = GetNodeOrNull<DataManager>("/root/DataManager");
		if (dataManager != null)
		{
			var enemyData = dataManager.GetEnemy(enemyId);
			if (enemyData != null)
			{
				_currentEnemyMaxHealth = enemyData.Health;
				_currentEnemyHealth = enemyData.Health;
				_currentEnemyAttack = enemyData.Attack;
				_currentEnemyDefense = enemyData.Defense;
			}
			else
			{
				GD.PrintErr($"未能找到敌人数据: {enemyId}，使用默认回退数值");
				_currentEnemyHealth = 100;
				_currentEnemyMaxHealth = 100;
				_currentEnemyAttack = 10;
				_currentEnemyDefense = 0;
			}
		}
		else
		{
			GD.PrintErr("未找到 DataManager 节点。使用默认配置。");
			_currentEnemyHealth = 100;
			_currentEnemyMaxHealth = 100;
			_currentEnemyAttack = 10;
			_currentEnemyDefense = 0;
		}
		
		// 初始化玩家卡组
		InitializePlayerDeck();
		
		// 抽起始手牌
		DrawStartingHand();
		
		EmitSignal(SignalName.CombatStarted, enemyId);
		
		GD.Print($"战斗开始！敌人: {enemyId}");
	}
	
	private void InitializePlayerDeck()
	{
		_playerDrawPile.Clear();
		_playerHand.Clear();
		_playerDiscardPile.Clear();
		
		if (_playerData != null && _playerData.Deck != null)
		{
			foreach (var cardId in _playerData.Deck)
			{
				_playerDrawPile.Add(cardId);
			}
			
			// 洗牌
			ShuffleDeck(_playerDrawPile);
		}
	}
	
	private void ShuffleDeck(List<string> deck)
	{
		var random = new Random();
		for (int i = deck.Count - 1; i > 0; i--)
		{
			int j = random.Next(i + 1);
			var temp = deck[i];
			deck[i] = deck[j];
			deck[j] = temp;
		}
	}
	
	private void DrawStartingHand()
	{
		int cardsToDraw = 4;
		
		for (int i = 0; i < cardsToDraw; i++)
		{
			DrawCard();
		}
	}
	
	private void DrawCard()
	{
		if (_playerDrawPile.Count == 0)
		{
			// 如果抽牌堆为空，将弃牌堆洗入抽牌堆
			foreach (var card in _playerDiscardPile)
			{
				_playerDrawPile.Add(card);
			}
			_playerDiscardPile.Clear();
			ShuffleDeck(_playerDrawPile);
		}
		
		if (_playerDrawPile.Count > 0)
		{
			var card = _playerDrawPile[0];
			_playerDrawPile.RemoveAt(0);
			_playerHand.Add(card);
			
			GD.Print($"抽到卡牌: {card}");
		}
	}
	
	public void PlayCard(string cardId, string targetId = "")
	{
		if (!_isPlayerTurn || _playerEnergy <= 0)
		{
			GD.Print("现在不能出牌");
			return;
		}
		
		if (!_playerHand.Contains(cardId))
		{
			GD.Print($"手牌中没有卡牌: {cardId}");
			return;
		}

		var dataManager = GetNodeOrNull<DataManager>("/root/DataManager");
		if (dataManager == null) return;
		var cardData = dataManager.GetCard(cardId);
		if (cardData == null) return;

		if (_playerEnergy < cardData.Cost)
		{
			GD.Print($"能量不足，需要 {cardData.Cost} 能量");
			return;
		}
		
		_playerHand.Remove(cardId);
		_playerDiscardPile.Add(cardId);
		_playerEnergy -= cardData.Cost;
		
		// 处理卡牌效果
		ProcessCardEffect(cardId, targetId);
		
		_eventBus.EmitCardPlayed(cardId, targetId);
		
		GD.Print($"玩家打出卡牌: {cardId}，剩余能量: {_playerEnergy}");
		
		// 如果玩家没有能量了，结束回合
		if (_playerEnergy <= 0)
		{
			EndPlayerTurn();
		}
	}
	
	private void ProcessCardEffect(string cardId, string targetId)
	{
		var dataManager = GetNodeOrNull<DataManager>("/root/DataManager");
		if (dataManager == null)
		{
			GD.PrintErr("处理卡牌效果失败：未找到 DataManager");
			return;
		}

		var cardData = dataManager.GetCard(cardId);
		if (cardData == null)
		{
			GD.PrintErr($"未能找到卡牌数据: {cardId}");
			return;
		}

		if (cardData.Effects == null || cardData.Effects.Count == 0)
		{
			GD.Print($"卡牌 {cardData.Name} 没有配置任何效果。");
			return;
		}

		foreach (var effect in cardData.Effects)
		{
			switch (effect.Key.ToLower())
			{
				case "damage":
					int damage = (int)effect.Value + (_playerData?.Attack ?? 0) + _nextAttackDamageBonus;
					_nextAttackDamageBonus = 0; // 消耗Buff
					ApplyDamageToEnemy(damage);
					break;
					
				case "defense":
				case "block":
					int defense = (int)effect.Value + (_playerData?.Defense ?? 0);
					_playerDefenseThisTurn += defense;
					GD.Print($"获得 {defense} 点护甲，当前护甲: {_playerDefenseThisTurn}");
					break;
					
				case "weakness":
					int weakAmount = (int)effect.Value;
					_currentEnemyAttack = Math.Max(0, _currentEnemyAttack - weakAmount);
					GD.Print($"敌人攻击力降低了 {weakAmount}点，当前攻击力: {_currentEnemyAttack}");
					break;
					
				case "restore_energy":
					_playerEnergy += (int)effect.Value;
					GD.Print($"回复了 {(int)effect.Value} 点能量，当前能量: {_playerEnergy}");
					break;
					
				case "heal":
					if (_gameManager != null)
					{
						_gameManager.HealPlayer((int)effect.Value);
						GD.Print($"恢复了 {(int)effect.Value} 点生命值");
					}
					break;
					
				case "poison_stacks":
					_enemyPoisonStacks += (int)effect.Value;
					GD.Print($"施加了 {(int)effect.Value} 层中毒，当前层数: {_enemyPoisonStacks}");
					break;
					
				case "poison_duration":
					_enemyPoisonDuration = Math.Max(_enemyPoisonDuration, (int)effect.Value);
					GD.Print($"中毒持续时间更新为: {_enemyPoisonDuration} 回合");
					break;
					
				case "draw":
					for (int i = 0; i < (int)effect.Value; i++)
					{
						DrawCard();
					}
					break;
					
				case "buff_next_attack":
					_nextAttackDamageBonus += (int)effect.Value;
					GD.Print($"获得下一击伤害Buff: +{(int)effect.Value}");
					break;
					
				default:
					GD.Print($"未处理的卡牌效果键名: {effect.Key}");
					break;
			}
		}
	}
	
	private void ApplyDamageToEnemy(int damage)
	{
		int actualDamage = Math.Max(1, damage - _currentEnemyDefense);
		_currentEnemyHealth -= actualDamage;
		
		if (_currentEnemyHealth <= 0)
		{
			_currentEnemyHealth = 0;
			EnemyDefeated();
		}
		
		_eventBus.EmitEnemyDamaged(_currentEnemyId, actualDamage);
		GD.Print($"对敌人造成 {actualDamage} 点伤害（包含护甲减免），敌人剩余生命: {_currentEnemyHealth}/{_currentEnemyMaxHealth}");
	}
	
	private void ApplyPoisonDamageToEnemy(int damage)
	{
		_currentEnemyHealth -= damage;
		
		if (_currentEnemyHealth <= 0)
		{
			_currentEnemyHealth = 0;
			EnemyDefeated();
		}
		
		_eventBus.EmitEnemyDamaged(_currentEnemyId, damage);
		GD.Print($"毒素对敌人造成 {damage} 点真实伤害，敌人剩余生命: {_currentEnemyHealth}/{_currentEnemyMaxHealth}");
	}
	
	private void EnemyDefeated()
	{
		GD.Print($"敌人 {_currentEnemyId} 被击败！");
		
		_eventBus.EmitEnemyDefeated(_currentEnemyId);
		_eventBus.EmitCombatEnded(true);
		EmitSignal(SignalName.CombatEnded, true);
		
		// 给予玩家奖励
		GiveCombatReward();
	}
	
	private void GiveCombatReward()
	{
		if (_playerData == null) return;

		var dataManager = GetNodeOrNull<DataManager>("/root/DataManager");
		if (dataManager != null)
		{
			var enemyData = dataManager.GetEnemy(_currentEnemyId);
			if (enemyData != null)
			{
				int goldReward = enemyData.RewardGold;
				_playerData.Gold += goldReward;
				
				GD.Print($"战斗胜利！获得 {goldReward} 金币，{enemyData.RewardExp} 经验。");
				
				foreach(var item in enemyData.RewardItems)
				{
					GD.Print($"掉落物品: {item}");
					// 如果Inventory有实现Add接口即可类似处理，暂打印：
					// if (_playerData.Inventory.ContainsKey(item)) _playerData.Inventory[item]++; 
					// else _playerData.Inventory[item] = 1;
				}
			}
			else
			{
				// 未获取到敌人数据，保底奖励
				_playerData.Gold += 20;
				GD.Print("获得 20 金币 (Fallback)");
			}
		}
		else
		{
			// GameManager的 fallback
			_playerData.Gold += 20;
			GD.Print("获得 20 金币 (Fallback)");
		}
	}
	
	public void EndPlayerTurnEarly()
	{
		if (_isPlayerTurn)
		{
			EndPlayerTurn();
		}
	}
	
	private void EndPlayerTurn()
	{
		_isPlayerTurn = false;
		_eventBus.EmitTurnEnded();
		GD.Print("玩家回合结束");
		
		// 回合结束，弃掉所有手牌
		foreach (var card in _playerHand)
		{
			_playerDiscardPile.Add(card);
		}
		_playerHand.Clear();
		GD.Print("弃置手牌完毕");
		// 呼叫一次UI刷新，让结束回合后的手牌消失
		_eventBus.EmitCardPlayed("", ""); // 借用CardPlayed或者直接写一个UpdateHand的信号，或者直接等待敌人回合结束重新抽牌
		
		// 开始敌人回合
		StartEnemyTurn();
	}
	
	private void StartEnemyTurn()
	{
		GD.Print("敌人回合开始");
		
		if (_enemyPoisonDuration > 0 && _enemyPoisonStacks > 0)
		{
			int poisonDamage = _enemyPoisonStacks;
			GD.Print($"目标中毒，受到 {poisonDamage} 点伤害！");
			ApplyPoisonDamageToEnemy(poisonDamage);
			_enemyPoisonDuration--;
			if (_enemyPoisonDuration <= 0)
			{
				_enemyPoisonStacks = 0;
			}
		}

		if (_currentEnemyHealth <= 0)
		{
			return; // 敌人被毒死，回合中止
		}
		
		// 敌人攻击玩家
		EnemyAttack();
		
		// 敌人回合结束后开始玩家回合
		StartPlayerTurn();
	}
	
	private void EnemyAttack()
	{
		var dataManager = GetNodeOrNull<DataManager>("/root/DataManager");
		List<string> aiPattern = null;

		if (dataManager != null)
		{
			var enemyData = dataManager.GetEnemy(_currentEnemyId);
			if (enemyData != null)
			{
				aiPattern = enemyData.AiPattern;
			}
		}

		if (aiPattern == null || aiPattern.Count == 0)
		{
			// 如果没有AI列表，默认只攻击
			aiPattern = new List<string> { "Attack" };
		}
		
		// 重置当前怪物的护甲值(可调整为不每回合重置，由具体游戏设计决定，此处配合防御动作通常只在本回合生效处理)
		_currentEnemyDefense = 0;

		string currentAction = aiPattern[_enemyAiStateIndex % aiPattern.Count];
		_enemyAiStateIndex++;

		GD.Print($"敌人执行动作: {currentAction}");

		switch (currentAction.ToLower())
		{
			case "attack":
				int damage = _currentEnemyAttack;
				// 应用玩家护甲
				int actualDamage = Math.Max(0, damage - _playerDefenseThisTurn);
				
				if (actualDamage > 0)
				{
					if (_gameManager != null)
					{
						_gameManager.ApplyDamageToPlayer(actualDamage);
					}
					else
					{
						GD.Print($"敌人对玩家造成 {actualDamage} 点伤害");
					}
				}
				else
				{
					GD.Print("玩家的护甲完全抵挡了敌人的攻击");
				}
				break;
			
			case "defend":
				// 假设敌防时获得额外防御值
				_currentEnemyDefense += 5;
				GD.Print($"敌人采取防御姿态，获得 {_currentEnemyDefense} 点护甲。");
				break;

			case "debuff":
				// 对玩家施加减益(这里简化为减少1手牌或直接清空本回合部分护甲状态等)
				GD.Print("敌人施放减益魔法，干扰玩家。");
				// 具体debuff可直接抛出事件由 GameManager/Player 处理，例如 _eventBus.EmitPlayerDebuffed()
				break;

			default:
				GD.Print($"未知的敌人行动: {currentAction}");
				break;
		}
		
		// 重置玩家本回合护甲
		_playerDefenseThisTurn = 0;
	}
	
	private void StartPlayerTurn()
	{
		_isPlayerTurn = true;
		_playerEnergy = 3; // 重置能量
		
		// 抽牌 改为每回合抽 4 张
		for (int i = 0; i < 4; i++)
		{
			DrawCard();
		}
		
		_eventBus.EmitPlayerTurnStarted();
		GD.Print("玩家回合开始，抽4张牌");
	}
	
	public void EndCombat(bool playerWon)
	{
		EmitSignal(SignalName.CombatEnded, playerWon);
		GD.Print($"战斗结束，玩家胜利: {playerWon}");
	}
	
	// 事件处理
	private void OnCombatStarted(string enemyId)
	{
		// 每次进入战斗都必须重新初始化，即使是同一种敌人 (否则上一次归零的血量保留)
		StartCombat(enemyId);
	}
	
	private void OnCardPlayed(string cardId, string targetId)
	{
		// 可以处理其他系统打出的卡牌（如果有多个战斗系统实例）
	}
	
	private void OnPlayerDamaged(int damage)
	{
		// 检查玩家是否死亡
		if (_playerData != null && _playerData.CurrentHealth <= 0)
		{
			EndCombat(false);
		}
	}
	
	private void OnEnemyDamaged(string enemyId, int damage)
	{
		// 如果受伤的是当前敌人，更新UI等
		if (enemyId == _currentEnemyId)
		{
			// 可以在这里更新敌人血条UI
		}
	}
	
	// 公开方法供UI调用
	public List<string> GetPlayerHand()
	{
		return new List<string>(_playerHand);
	}
	
	public int GetPlayerEnergy()
	{
		return _playerEnergy;
	}
	
	public (int current, int max) GetEnemyHealth()
	{
		return (_currentEnemyHealth, _currentEnemyMaxHealth);
	}
	
    // 返回玩家本回合的护甲/防御值（UI 显示用）
    public int GetPlayerDefense()
    {
        return _playerDefenseThisTurn;
    }

	public bool IsPlayerTurn()
	{
		return _isPlayerTurn;
	}
}
