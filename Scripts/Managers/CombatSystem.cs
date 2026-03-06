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
	
	private string _currentEnemyId;
	private int _currentEnemyHealth = 100;
	private int _currentEnemyMaxHealth = 100;
	private int _currentEnemyAttack = 10;
	
	private bool _isPlayerTurn = true;
	private int _playerActionPoints = 3;
	private int _playerDefenseThisTurn = 0;

	// 抽牌 / 手牌相关
	private const int MaxHandSize = 5;
	private const int DrawsPerTurn = 4; // 每回合最多主动抽牌次数
	private int _drawsRemainingThisTurn = 0;

	// 持续效果
	private int _nextAttackBonusDamage = 0;     // 下一个攻击牌额外伤害
	private int _enemyPoisonStacks = 0;         // 中毒层数
	private int _enemyPoisonTurnsRemaining = 0; // 中毒剩余回合数
	
	private List<string> _playerHand = new List<string>();
	private List<string> _playerDrawPile = new List<string>();
	private List<string> _playerDiscardPile = new List<string>();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// 优先从 GameRoot 单例获取管理器，其次从 Autoload 根节点获取
		_gameManager = GameRoot.Instance?.GameManager 
					   ?? GetNodeOrNull<GameManager>("/root/GameManager");
		_eventBus = GameRoot.Instance?.EventBus 
					?? GetNodeOrNull<EventBus>("/root/EventBus");
		
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
		_currentEnemyId = enemyId;
		_isPlayerTurn = true;
		_playerActionPoints = _playerData?.ActionPoints ?? 3;
		_playerDefenseThisTurn = 0;
		_drawsRemainingThisTurn = DrawsPerTurn;
		
		// 初始化敌人属性（这里简单设置，实际应从数据加载）
		_currentEnemyHealth = 100;
		_currentEnemyMaxHealth = 100;
		_currentEnemyAttack = 10;
		
		// 初始化玩家卡组
		InitializePlayerDeck();
		
		EmitSignal(SignalName.CombatStarted, enemyId);
		_eventBus.EmitCombatStarted(enemyId);
		
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
		if (!_isPlayerTurn || _playerActionPoints <= 0)
		{
			GD.Print("现在不能出牌");
			return;
		}
		
		if (!_playerHand.Contains(cardId))
		{
			GD.Print($"手牌中没有卡牌: {cardId}");
			return;
		}
		
		_playerHand.Remove(cardId);
		_playerDiscardPile.Add(cardId);
		_playerActionPoints--;
		
		// 处理卡牌效果
		ProcessCardEffect(cardId, targetId);
		
		_eventBus.EmitCardPlayed(cardId, targetId);
		
		GD.Print($"玩家打出卡牌: {cardId}，剩余行动点: {_playerActionPoints}");
		
		// 如果玩家没有行动点了，结束回合
		if (_playerActionPoints <= 0)
		{
			EndPlayerTurn();
		}
	}
	
	private void ProcessCardEffect(string cardId, string targetId)
	{
		// 根据卡牌ID处理不同效果
		switch (cardId)
		{
			case "card_scythe_slash": // 镰刀挥砍：造成 7 点伤害（目前不叠加攻击力，数值与策划一致）
			{
				int baseDamage = 7;
				int damage = baseDamage + _nextAttackBonusDamage;
				_nextAttackBonusDamage = 0;
				ApplyDamageToEnemy(damage);
				break;
			}

			case "card_hoe_smash": // 锄头猛击：12 伤害 + 获得 1 行动点
			{
				int baseDamage = 12;
				int damage = baseDamage + _nextAttackBonusDamage;
				_nextAttackBonusDamage = 0;
				ApplyDamageToEnemy(damage);
				_playerActionPoints += 1;
				GD.Print("锄头猛击：获得 1 点行动力");
				break;
			}

			case "card_raise_tools": // 架起农具：获得 6 点护盾（本回合）
			{
				int block = 6;
				_playerDefenseThisTurn += block;
				GD.Print($"获得 {block} 点护盾，当前护盾: {_playerDefenseThisTurn}");
				break;
			}

			case "card_emergency_bandage": // 紧急包扎：回复 4 点生命
			{
				if (_playerData != null)
				{
					int heal = 4;
					_playerData.CurrentHealth = Math.Min(_playerData.MaxHealth, _playerData.CurrentHealth + heal);
					GD.Print($"紧急包扎：回复 {heal} 点生命，当前生命: {_playerData.CurrentHealth}/{_playerData.MaxHealth}");
				}
				break;
			}

			case "card_spray_pesticide": // 喷农药：施加 1 层中毒，持续 2 回合
			{
				_enemyPoisonStacks += 1;
				_enemyPoisonTurnsRemaining = Math.Max(_enemyPoisonTurnsRemaining, 2);
				GD.Print($"喷农药：敌人获得 1 层中毒，持续 {_enemyPoisonTurnsRemaining} 回合");
				break;
			}

			case "card_observe_weakness": // 观察弱点：抽 1 张牌，下张攻击牌伤害 +4
			{
				DrawCard();
				_nextAttackBonusDamage += 4;
				GD.Print("观察弱点：抽 1 张牌，下一个攻击牌伤害 +4");
				break;
			}

			// 兼容旧测试卡牌 ID（如果其它地方还在用）
			case "card_attack_basic":
			{
				int damage = 6;
				ApplyDamageToEnemy(damage);
				break;
			}
			case "card_defend_basic":
			{
				int defense = 5 + (_playerData?.Defense ?? 0);
				_playerDefenseThisTurn += defense;
				GD.Print($"获得 {defense} 点护甲，当前护甲: {_playerDefenseThisTurn}");
				break;
			}
			case "card_skill_weakness":
			{
				_currentEnemyAttack = Math.Max(5, _currentEnemyAttack - 3);
				GD.Print($"敌人攻击力降低，当前攻击力: {_currentEnemyAttack}");
				break;
			}
				
			default:
				GD.Print($"未知卡牌效果: {cardId}");
				break;
		}
	}
	
	private void ApplyDamageToEnemy(int damage)
	{
		_currentEnemyHealth -= damage;
		
		if (_currentEnemyHealth <= 0)
		{
			_currentEnemyHealth = 0;
			EnemyDefeated();
		}
		
		_eventBus.EmitEnemyDamaged(_currentEnemyId, damage);
		GD.Print($"对敌人造成 {damage} 点伤害，敌人剩余生命: {_currentEnemyHealth}/{_currentEnemyMaxHealth}");
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
		// 简单奖励：金币和经验
		if (_playerData != null)
		{
			int goldReward = 20;
			_playerData.Gold += goldReward;
			GD.Print($"获得 {goldReward} 金币");
		}
	}
	
	private void EndPlayerTurn()
	{
		_isPlayerTurn = false;
		_eventBus.EmitTurnEnded();
		GD.Print("玩家回合结束");
		
		// 开始敌人回合
		StartEnemyTurn();
	}
	
	private void StartEnemyTurn()
	{
		GD.Print("敌人回合开始");

		// 中毒在敌人回合开始时结算
		if (_enemyPoisonStacks > 0 && _enemyPoisonTurnsRemaining > 0)
		{
			int poisonDamage = _enemyPoisonStacks * 2; // 每层每回合 2 点伤害，可根据策划调整
			ApplyDamageToEnemy(poisonDamage);
			_enemyPoisonTurnsRemaining--;
			GD.Print($"中毒结算：对敌人造成 {poisonDamage} 点伤害，还剩 {_enemyPoisonTurnsRemaining} 回合");

			if (_enemyPoisonTurnsRemaining <= 0)
			{
				_enemyPoisonStacks = 0;
				GD.Print("中毒效果结束");
			}
		}
		
		// 敌人攻击玩家
		EnemyAttack();
		
		// 敌人回合结束后开始玩家回合
		StartPlayerTurn();
	}
	
	private void EnemyAttack()
	{
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
		
		// 重置本回合护甲
		_playerDefenseThisTurn = 0;
	}
	
	private void StartPlayerTurn()
	{
		_isPlayerTurn = true;
		_playerActionPoints = _playerData?.ActionPoints ?? 3; // 重置行动点
		_drawsRemainingThisTurn = DrawsPerTurn;               // 重置本回合可抽牌次数

		// 若抽牌堆已空，将弃牌堆洗回抽牌堆，保证本回合能抽牌
		if (_playerDrawPile.Count == 0 && _playerDiscardPile.Count > 0)
		{
			foreach (var card in _playerDiscardPile)
				_playerDrawPile.Add(card);
			_playerDiscardPile.Clear();
			ShuffleDeck(_playerDrawPile);
			GD.Print("弃牌堆洗入抽牌堆");
		}
		
		_eventBus.EmitPlayerTurnStarted();
		GD.Print("玩家回合开始，可以通过点击牌堆抽牌");
	}
	
	public void EndCombat(bool playerWon)
	{
		EmitSignal(SignalName.CombatEnded, playerWon);
		GD.Print($"战斗结束，玩家胜利: {playerWon}");
	}
	
	// 事件处理
	private void OnCombatStarted(string enemyId)
	{
		// 如果这不是当前战斗系统触发的战斗，可以重新初始化
		if (_currentEnemyId != enemyId)
		{
			StartCombat(enemyId);
		}
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

	/// <summary>玩家尝试通过点击牌堆抽 1 张牌（受每回合次数与手牌上限限制）</summary>
	public bool TryDrawCard()
	{
		if (!_isPlayerTurn)
		{
			GD.Print("现在不是玩家回合，不能抽牌");
			return false;
		}

		if (_drawsRemainingThisTurn <= 0)
		{
			GD.Print("本回合抽牌次数已用完");
			return false;
		}

		if (_playerHand.Count >= MaxHandSize)
		{
			GD.Print("手牌已达上限，不能继续抽牌");
			return false;
		}

		// 牌堆为 0 时不允许抽牌（不在这里洗入弃牌堆，回合开始时再洗）
		if (_playerDrawPile.Count == 0)
		{
			GD.Print("牌堆已空，无法抽牌");
			return false;
		}

		DrawCard();
		_drawsRemainingThisTurn--;
		return true;
	}

	/// <summary>抽牌堆剩余张数（供战斗UI显示）</summary>
	public int GetDrawPileCount()
	{
		return _playerDrawPile.Count;
	}

	/// <summary>弃牌堆张数（供战斗UI显示）</summary>
	public int GetDiscardPileCount()
	{
		return _playerDiscardPile.Count;
	}
	
	public int GetPlayerActionPoints()
	{
		return _playerActionPoints;
	}
	
	public (int current, int max) GetEnemyHealth()
	{
		return (_currentEnemyHealth, _currentEnemyMaxHealth);
	}
	
	public bool IsPlayerTurn()
	{
		return _isPlayerTurn;
	}
}
