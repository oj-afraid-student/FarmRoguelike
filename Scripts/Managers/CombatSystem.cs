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
	
	private List<string> _playerHand = new List<string>();
	private List<string> _playerDrawPile = new List<string>();
	private List<string> _playerDiscardPile = new List<string>();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_gameManager = GetNode<GameManager>("/root/GameRoot/GameManager");
		_eventBus = GetNode<EventBus>("/root/GameRoot/EventBus");
		
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
		_playerActionPoints = 3;
		_playerDefenseThisTurn = 0;
		
		// 初始化敌人属性（这里简单设置，实际应从数据加载）
		_currentEnemyHealth = 100;
		_currentEnemyMaxHealth = 100;
		_currentEnemyAttack = 10;
		
		// 初始化玩家卡组
		InitializePlayerDeck();
		
		// 抽起始手牌
		DrawStartingHand();
		
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
	
	private void DrawStartingHand()
	{
		int cardsToDraw = Math.Min(5, _playerDrawPile.Count);
		
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
			case "card_attack_basic":
				int damage = 8 + (_playerData?.Attack ?? 0);
				ApplyDamageToEnemy(damage);
				break;
				
			case "card_defend_basic":
				int defense = 5 + (_playerData?.Defense ?? 0);
				_playerDefenseThisTurn += defense;
				GD.Print($"获得 {defense} 点护甲，当前护甲: {_playerDefenseThisTurn}");
				break;
				
			case "card_skill_weakness":
				// 假设这是削弱敌人攻击的技能
				_currentEnemyAttack = Math.Max(5, _currentEnemyAttack - 3);
				GD.Print($"敌人攻击力降低，当前攻击力: {_currentEnemyAttack}");
				break;
				
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
		_playerActionPoints = 3; // 重置行动点
		
		// 抽牌
		DrawCard();
		
		_eventBus.EmitPlayerTurnStarted();
		GD.Print("玩家回合开始，抽1张牌");
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
