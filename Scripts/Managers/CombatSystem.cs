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
	private int _currentEnemyBaseAttack = 10;
	private int _currentEnemyDefense = 0;
	private int _currentEnemyBaseDefense = 0;
	private int _enemyAiStateIndex = 0;
	private int _enemySummonedCount = 0;
	private bool _enemyEnraged = false;
	private int _enemyPendingAttackBonus = 0;
	private int _overlordEvolutionHitCounter = 0;
	private int _overlordEvolutionStacks = 0;
	
	private int _enemyPoisonStacks = 0;

	private Dictionary<string, CombatStatusRuntime> _playerStatuses = new();
	private Dictionary<string, CombatStatusRuntime> _enemyStatuses = new();
	private int _playerExciteDrawCountThisTurn = 0;
	
	private bool _isPlayerTurn = true;
	private int _playerEnergy = 3;
	private int _playerDefenseThisTurn = 0;
	private int _cardsPlayedThisTurn = 0;
	private int _skillsPlayedThisTurn = 0;
	private int _nextAttackDamageBonus = 0;
	private bool _extraTurnRequested = false;
	
	private List<string> _playerHand = new List<string>();
	private List<string> _playerDrawPile = new List<string>();
	private List<string> _playerDiscardPile = new List<string>();

	private class CombatStatusRuntime
	{
		public string Id;
		public int Stacks;
		public int RemainingTurns;
	}
	
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
		_playerEnergy = _playerData?.Energy ?? 3; // Use player's base energy
		_playerDefenseThisTurn = 0;
		_enemyAiStateIndex = 0;
		_enemySummonedCount = 0;
		_enemyEnraged = false;
		_enemyPendingAttackBonus = 0;
		_overlordEvolutionHitCounter = 0;
		_overlordEvolutionStacks = 0;
		_enemyPoisonStacks = 0;
		_nextAttackDamageBonus = 0;
		_playerStatuses.Clear();
		_enemyStatuses.Clear();
		_playerExciteDrawCountThisTurn = 0;
		_cardsPlayedThisTurn = 0;
		_skillsPlayedThisTurn = 0;
		_extraTurnRequested = false;
		
		var dataManager = GetNodeOrNull<DataManager>("/root/DataManager");
		if (dataManager != null)
		{
			var enemyData = dataManager.GetEnemy(enemyId);
			if (enemyData != null)
			{
				_currentEnemyMaxHealth = enemyData.Health;
				_currentEnemyHealth = enemyData.Health;
				_currentEnemyAttack = enemyData.Attack;
				_currentEnemyBaseAttack = enemyData.Attack;
				_currentEnemyDefense = enemyData.Defense;
				_currentEnemyBaseDefense = enemyData.Defense;
			}
			else
			{
				GD.PrintErr($"未能找到敌人数据: {enemyId}，使用默认回退数值");
				_currentEnemyHealth = 100;
				_currentEnemyMaxHealth = 100;
				_currentEnemyAttack = 10;
				_currentEnemyBaseAttack = 10;
				_currentEnemyDefense = 0;
				_currentEnemyBaseDefense = 0;
			}
		}
		else
		{
			GD.PrintErr("未找到 DataManager 节点。使用默认配置。");
			_currentEnemyHealth = 100;
			_currentEnemyMaxHealth = 100;
			_currentEnemyAttack = 10;
			_currentEnemyBaseAttack = 10;
			_currentEnemyDefense = 0;
			_currentEnemyBaseDefense = 0;
		}
		
		// 初始化玩家卡组
		InitializePlayerDeck();
		
		// 抽起始手牌
		DrawStartingHand();

		ApplyNextBattleStatusFromPermanentUpgrades();
		
		EmitSignal(SignalName.CombatStarted, enemyId);
		
		GD.Print($"战斗开始！敌人: {enemyId}");
	}
	
	private void InitializePlayerDeck()
	{
		_playerDrawPile.Clear();
		_playerHand.Clear();
		_playerDiscardPile.Clear();
		
		if (_playerData != null)
		{
			List<string> sourceDeck = _playerData.CurrentCombatDeck != null && _playerData.CurrentCombatDeck.Count > 0 
				? _playerData.CurrentCombatDeck 
				: _playerData.Deck;
				
			if (sourceDeck != null)
			{
				foreach (var cardId in sourceDeck)
				{
					_playerDrawPile.Add(cardId);
				}
				
				// 洗牌
				ShuffleDeck(_playerDrawPile);
			}
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

		if (HasStatus(_playerStatuses, "silence") && cardData.Type != GameEnums.CardType.Attack)
		{
			GD.Print("沉默状态下不能使用技能/能力牌");
			return;
		}

		if (HasStatus(_playerStatuses, "disarm") && cardData.Type == GameEnums.CardType.Attack)
		{
			GD.Print("缴械状态下不能使用攻击牌");
			return;
		}
		
		_playerHand.Remove(cardId);
		_playerDiscardPile.Add(cardId);
		_playerEnergy -= cardData.Cost;
		_cardsPlayedThisTurn++;
		
		if (cardData.Type == GameEnums.CardType.Skill)
		{
			_skillsPlayedThisTurn++;
			// 专注：每回合首次使用技能牌时，抽 1 张牌
			if (_skillsPlayedThisTurn == 1 && HasStatus(_playerStatuses, "focus"))
			{
				int focusMuls = GetStatusStacks(_playerStatuses, "focus");
				for (int i = 0; i < focusMuls; i++) DrawCard();
				GD.Print($"专注效果触发，抽取了 {focusMuls} 张牌");
			}
		}

		if (cardData.Type == GameEnums.CardType.Attack)
		{
			// 狂暴：每使用一张攻击牌，获得1层力量
			if (HasStatus(_playerStatuses, "frenzy"))
			{
				int frenzyStacks = GetStatusStacks(_playerStatuses, "frenzy");
				ApplyStatus(_playerStatuses, "strength", frenzyStacks, 1);
				GD.Print($"狂暴效果触发，获得了 {frenzyStacks} 层力量");
			}
			// 连击架势：每用一张攻击牌，下张攻击牌伤害 +2
			if (HasStatus(_playerStatuses, "combo_stance"))
			{
				int comboStacks = 2 * GetStatusStacks(_playerStatuses, "combo_stance");
				_nextAttackDamageBonus += comboStacks;
				GD.Print($"连击架势触发，下一次攻击附加 {comboStacks} 点伤害");
			}
		}

		// 检查这张牌是否有净化效果
		bool hasPurify = cardData.Effects != null && cardData.Effects.ContainsKey("purify");

		// 处理诅咒：每打出一张牌受到伤害
		if (HasStatus(_playerStatuses, "curse") && !hasPurify)
		{
			int curseDmg = 2 * GetStatusStacks(_playerStatuses, "curse");
			GD.Print($"诅咒发作！玩家受到 {curseDmg} 点伤害");
			ApplyDamageToPlayer(curseDmg);
		}
		else if (HasStatus(_playerStatuses, "curse") && hasPurify)
		{
			GD.Print("打出了净化牌，本次诅咒发作被免疫，准备清除诅咒");
		}
		
		// 处理卡牌效果
		ProcessCardEffect(cardId, targetId);

		ApplyStatusAfterPlayCard();
		
		// 振奋：每次出牌回血
		if (HasStatus(_playerStatuses, "inspire"))
		{
			int healAmount = 1 * GetStatusStacks(_playerStatuses, "inspire");
			_gameManager?.HealPlayer(healAmount);
			GD.Print($"振奋效果触发，回复了 {healAmount} 点生命");
		}
		
		_eventBus.EmitCardPlayed(cardId, targetId);
		
		GD.Print($"玩家打出卡牌: {cardId}，剩余能量: {_playerEnergy}");
		
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
					int damage = (int)effect.Value + _nextAttackDamageBonus;
					_nextAttackDamageBonus = 0; // 消耗Buff

					if (cardData.Effects.TryGetValue("debuff_bonus_damage", out float bonusPerStack))
					{
						int debuffStacks = GetStatusStacks(_enemyStatuses, "poison") +
										   GetStatusStacks(_enemyStatuses, "burn") +
										   GetStatusStacks(_enemyStatuses, "freeze") + 
										   GetStatusStacks(_enemyStatuses, "weak") +
										   GetStatusStacks(_enemyStatuses, "vulnerable") +
										   GetStatusStacks(_enemyStatuses, "stun");
						damage += debuffStacks * (int)bonusPerStack;
					}

					if (cardData.Effects.TryGetValue("heavy_strike_bonus", out float heavyBonus))
					{
						if (_cardsPlayedThisTurn == 1) // Only this card played
						{
							damage += (int)heavyBonus;
						}
					}

					ApplyDamageToEnemy(damage);

					if (cardData.Effects.TryGetValue("multi_hit", out float hits))
					{
						for (int i = 1; i < (int)hits; i++)
						{
							int multiDamage = (int)effect.Value;
							ApplyDamageToEnemy(multiDamage);
						}
					}
					break;
					
				case "defense":
				case "block":
				case "armor":
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
				ApplyStatus(_enemyStatuses, "poison", (int)effect.Value, 3);
				_enemyPoisonStacks = GetStatusStacks(_enemyStatuses, "poison");
				GD.Print($"施加了 {(int)effect.Value} 层中毒，当前层数: {_enemyPoisonStacks}");
				break;

			case "strength":
				ApplyStatus(_playerStatuses, "strength", (int)effect.Value, 1);
				break;

			case "ironwall":
				ApplyStatus(_playerStatuses, "ironwall", (int)effect.Value, 2);
				break;

			case "lifesteal":
				ApplyStatus(_playerStatuses, "lifesteal", 1, 2);
				break;
				
			case "lose_hp":
				if (_gameManager != null)
				{
					_gameManager.ApplyDamageToPlayer((int)effect.Value);
					GD.Print($"失去 {(int)effect.Value} 点生命值");
				}
				break;
				
			case "burn_stacks":
				ApplyStatus(_enemyStatuses, "burn", (int)effect.Value, 3);
				GD.Print($"施加了 {(int)effect.Value} 层燃烧");
				break;
				
			case "freeze_stacks":
				ApplyStatus(_enemyStatuses, "freeze", (int)effect.Value, 2);
				GD.Print($"施加了 {(int)effect.Value} 层冰冻");
				break;
				
			case "stun_stacks":
				ApplyStatus(_enemyStatuses, "stun", (int)effect.Value, 1);
				GD.Print($"施加了 {(int)effect.Value} 层眩晕");
				break;
				
			case "vulnerable_stacks":
				ApplyStatus(_enemyStatuses, "vulnerable", (int)effect.Value, 2);
				GD.Print($"施加了 {(int)effect.Value} 层易伤(破甲)");
				break;
				
			case "precision_stacks":
				ApplyStatus(_playerStatuses, "precision", (int)effect.Value, 1);
				GD.Print($"获得了 {(int)effect.Value} 层精准");
				break;
				
			case "strength_stacks":
				ApplyStatus(_playerStatuses, "strength", (int)effect.Value, 3);
				GD.Print($"获得了 {(int)effect.Value} 层力量");
				break;
				
			case "lifesteal_stacks":
				ApplyStatus(_playerStatuses, "lifesteal", 1, (int)effect.Value);
				GD.Print($"获得了嗜血效果，持续 {(int)effect.Value} 回合");
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
					
				case "pacifist_stacks":
					ApplyStatus(_playerStatuses, "pacifist", (int)effect.Value, 1);
					GD.Print($"获得了和平(禁止攻击) {(int)effect.Value} 层");
					break;
					
				case "reflect_stacks":
					ApplyStatus(_playerStatuses, "reflect", (int)effect.Value, 1);
					GD.Print($"获得了反伤特效 {(int)effect.Value} 层");
					break;
					
				case "inspire_stacks":
					ApplyStatus(_playerStatuses, "inspire", (int)effect.Value, 1);
					GD.Print($"获得了振奋特效 {(int)effect.Value} 层");
					break;

				case "weak_stacks":
					ApplyStatus(_enemyStatuses, "weak", (int)effect.Value, 2);
					GD.Print($"施加了虚弱 {(int)effect.Value} 层");
					break;
					
				case "disarm_stacks":
					ApplyStatus(_enemyStatuses, "disarm", (int)effect.Value, 1);
					GD.Print($"缴械敌人 {(int)effect.Value} 层");
					break;
					
				case "silence_stacks":
					ApplyStatus(_enemyStatuses, "silence", (int)effect.Value, 1);
					GD.Print($"沉默敌人 {(int)effect.Value} 层");
					break;

				case "purify":
					// 解除自身所有的负面状态 (弱化, 易伤, 中毒, 燃烧, 晕眩等)
					_playerStatuses.Remove("poison");
					_playerStatuses.Remove("burn");
					_playerStatuses.Remove("weak");
					_playerStatuses.Remove("vulnerable");
					_playerStatuses.Remove("freeze");
					_playerStatuses.Remove("stun");
					_playerStatuses.Remove("curse");
					GD.Print("玩家触发了净化，移除了所有负面状态");
					break;
					
				case "draw_cards":
					for (int i = 0; i < (int)effect.Value; i++)
					{
						DrawCard();
					}
					GD.Print($"战术效果: 抽了 {(int)effect.Value} 张牌");
					break;
					
				case "discard_cards":
					// 暂时实现为随机丢弃，如果是特定UI选取丢牌需要更复杂的交互
					for (int i = 0; i < (int)effect.Value; i++)
					{
						if (_playerHand.Count > 0)
						{
							var rdn = new Random();
							int discardIdx = rdn.Next(_playerHand.Count);
							string discardedId = _playerHand[discardIdx];
							_playerHand.RemoveAt(discardIdx);
							_playerDiscardPile.Add(discardedId);
							GD.Print($"战术效果: 丢弃了手牌 {discardedId}");
						}
					}
					break;
					
				case "gain_energy":
					_playerEnergy += (int)effect.Value;
					GD.Print($"临时获得了 {(int)effect.Value} 能量");
					break;
					
				case "frenzy_stacks":
					ApplyStatus(_playerStatuses, "frenzy", (int)effect.Value, -1);
					GD.Print($"获得了狂暴特效 {(int)effect.Value} 层");
					break;

				case "focus_stacks":
					ApplyStatus(_playerStatuses, "focus", (int)effect.Value, -1);
					GD.Print($"获得了专注特效 {(int)effect.Value} 层");
					break;

				case "elemental_affinity_duration":
					ApplyStatus(_playerStatuses, "elemental_affinity", 1, (int)effect.Value);
					GD.Print($"获得了元素亲和，持续 {(int)effect.Value} 回合");
					break;

				case "combo_stance_stacks":
					ApplyStatus(_playerStatuses, "combo_stance", (int)effect.Value, 1);
					GD.Print($"获得了连击架势 {(int)effect.Value} 层");
					break;

				case "curse_stacks":
					ApplyStatus(_playerStatuses, "curse", (int)effect.Value, 1);
					GD.Print($"玩家受到了诅咒 {(int)effect.Value} 层");
					break;

				case "extra_turn_stacks":
					_extraTurnRequested = true;
					GD.Print("时光沙漏发动！回合结束时将发生时光倒流。");
					break;
					
				default:
					GD.Print($"未处理的卡牌效果键名: {effect.Key}");
					break;
			}
		}
	}
	
	private void ApplyDamageToEnemy(int damage)
	{
		int modifiedDamage = ApplyOutgoingDamageModifiers(damage, _playerStatuses);
		modifiedDamage = ApplyIncomingDamageModifiers(modifiedDamage, _enemyStatuses);

		bool ignoreDefense = HasStatus(_playerStatuses, "precision");
		int enemyDefense = GetEffectiveEnemyDefense();
		int actualDamage = ignoreDefense ? Math.Max(1, modifiedDamage) : Math.Max(1, modifiedDamage - enemyDefense);
		_currentEnemyHealth -= actualDamage;

		ApplyLifestealIfAny(actualDamage);
		
		_eventBus.EmitEnemyDamaged(_currentEnemyId, actualDamage);
		GD.Print($"对敌人造成 {actualDamage} 点伤害（包含护甲减免），敌人预期生命: {_currentEnemyHealth}/{_currentEnemyMaxHealth}");

		if (_currentEnemyHealth <= 0)
		{
			_currentEnemyHealth = 0;
			EnemyDefeated();
		}

		TryApplyOverlordEvolution();
	}

	private void ApplyBurnDamageToEnemy(int stacks)
	{
		int burnDamage = 1 * stacks; // 1点固定伤害每层
		if (HasStatus(_playerStatuses, "elemental_affinity"))
		{
			burnDamage += 3 * GetStatusStacks(_playerStatuses, "elemental_affinity");
			GD.Print("元素亲和增幅了燃烧伤害");
		}
		
		_currentEnemyHealth -= burnDamage;
		
		_eventBus.EmitEnemyDamaged(_currentEnemyId, burnDamage);
		GD.Print($"燃烧对敌人造成 {burnDamage} 点真实伤害，敌人预期生命: {_currentEnemyHealth}/{_currentEnemyMaxHealth}");

		if (_currentEnemyHealth <= 0)
		{
			_currentEnemyHealth = 0;
			EnemyDefeated();
		}
	}
	
	// This method was missing from the provided context, adding it based on the instruction's implied structure.
	public void ApplyDamageToPlayer(int damage)
	{
		if (_playerData == null) return;

		int modifiedDamage = ApplyIncomingDamageModifiers(damage, _playerStatuses);
		int actualDamage = Math.Max(0, modifiedDamage - _playerDefenseThisTurn);
		
		_playerData.CurrentHealth -= actualDamage;
		if (_playerData.CurrentHealth <= 0)
		{
			_playerData.CurrentHealth = 0;
			_eventBus.EmitCombatEnded(false);
			EmitSignal(SignalName.CombatEnded, false);
			GD.Print("玩家被击败！");
		}
		// 发送受伤事件
		_eventBus.EmitPlayerDamaged(actualDamage);
		
		GD.Print($"玩家受到了 {actualDamage} 伤害，剩余生命值: {_playerData.CurrentHealth}/{_playerData.MaxHealth}");
		
		if (actualDamage > 0 && HasStatus(_playerStatuses, "reflect"))
		{
			int reflectDamage = 3 * GetStatusStacks(_playerStatuses, "reflect");
			GD.Print($"玩家触发了反伤！对敌人造成 {reflectDamage} 点反射伤害。");
			ApplyDamageToEnemy(reflectDamage);
		}
	}
	
	private void ApplyPoisonDamageToEnemy(int damage)
	{
		damage = ApplyIncomingDamageModifiers(damage, _enemyStatuses);
		if (HasStatus(_playerStatuses, "elemental_affinity"))
		{
			damage += 3 * GetStatusStacks(_playerStatuses, "elemental_affinity");
			GD.Print("元素亲和增幅了毒素伤害");
		}
		
		_currentEnemyHealth -= damage;
		
		_eventBus.EmitEnemyDamaged(_currentEnemyId, damage);
		GD.Print($"毒素对敌人造成 {damage} 点真实伤害，敌人预期生命: {_currentEnemyHealth}/{_currentEnemyMaxHealth}");

		if (_currentEnemyHealth <= 0)
		{
			_currentEnemyHealth = 0;
			EnemyDefeated();
		}
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
		ProcessTurnEndStatuses(isPlayer: true);
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

		if (_extraTurnRequested)
		{
			_extraTurnRequested = false;
			GD.Print("【额外回合】触发！强制开启下一次玩家回合，跳过敌人行动。");
			StartPlayerTurn();
			return;
		}
		
		// 开始敌人回合
		StartEnemyTurn();
	}
	
	private void StartEnemyTurn()
	{
		GD.Print("敌人回合开始");

		ProcessTurnStartStatuses(isPlayer: false);

		if (_currentEnemyHealth <= 0)
		{
			return; // 敌人被毒死，回合中止
		}

		if (TryConsumeSkipTurn(_enemyStatuses))
		{
			GD.Print("敌人因控制效果跳过行动");
			ProcessTurnEndStatuses(isPlayer: false);
			StartPlayerTurn();
			return;
		}
		
		// 敌人攻击玩家
		EnemyAttack();
		ProcessTurnEndStatuses(isPlayer: false);
		
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
		
		// 每回合开始重置为基础防御值（可被防御动作临时提高）
		_currentEnemyDefense = _currentEnemyBaseDefense;
		TryApplyEnemyRagePassive();

		string currentAction = aiPattern[_enemyAiStateIndex % aiPattern.Count];
		_enemyAiStateIndex++;

		GD.Print($"敌人执行动作: {currentAction}");

		switch (currentAction.ToLower())
		{
			case "bite":
			case "attack":
				ExecuteEnemyAttack(_currentEnemyAttack);
				break;

			case "poisonbite":
				ExecuteEnemyAttack(_currentEnemyAttack);
				ApplyStatus(_playerStatuses, "poison", 1, 2);
				GD.Print("毒牙命中：玩家获得 1 层中毒(2回合)");
				break;

			case "toxicmist":
				ApplyStatus(_playerStatuses, "poison", 2, 2);
				GD.Print("毒雾扩散：玩家获得 2 层中毒(2回合)");
				break;

			case "photosynthesis":
				EnemyHeal(5);
				GD.Print("光合作用：敌人回复 5 点生命，本回合不攻击");
				break;
			
			case "defend":
				// 假设敌防时获得额外防御值
				_currentEnemyDefense += 5;
				GD.Print($"敌人采取防御姿态，获得 {_currentEnemyDefense} 点护甲。");
				break;

			case "harden":
				_currentEnemyDefense += 6;
				_enemyPendingAttackBonus += 2;
				GD.Print("固化：获得 6 点护甲，下次攻击提升 2 点");
				break;

			case "roarweak":
				ExecuteEnemyAttack(4);
				ApplyStatus(_playerStatuses, "weak", 1, 1);
				GD.Print("咆哮：对玩家施加 1 层虚弱");
				break;

			case "corrosiveacid":
				ExecuteEnemyAttack(6);
				ApplyStatus(_playerStatuses, "vulnerable", 2, 2);
				GD.Print("腐蚀酸液：施加 2 层破甲(易伤)");
				break;

			case "injectvenom":
				ExecuteEnemyAttack(5);
				ApplyStatus(_playerStatuses, "poison", 2, 3);
				ApplyStatus(_playerStatuses, "weak", 1, 3);
				GD.Print("注入毒液：施加 2 层中毒与 1 层虚弱(3回合)");
				break;

			case "summonlarva":
			case "summonworker":
			case "summonwarrior":
				if (_enemyEnraged && string.Equals(_currentEnemyId, "enemy_locust_matriarch_juvenile", StringComparison.OrdinalIgnoreCase))
				{
					GD.Print("幼体狂暴后不再召唤，改为啃咬。");
					ExecuteEnemyAttack(_currentEnemyAttack);
					break;
				}
				ApplySummonBuff();
				break;

			case "swarmstorm":
				ExecuteEnemyAttack(5);
				ApplySummonBuff();
				GD.Print("虫族风暴：造成伤害并触发召唤增益");
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
		_currentEnemyAttack = _currentEnemyBaseAttack;
	}

	private void ExecuteEnemyAttack(int baseDamage)
	{
		if (_enemyPendingAttackBonus > 0)
		{
			baseDamage += _enemyPendingAttackBonus;
			GD.Print($"敌人获得固化加成，本次额外 +{_enemyPendingAttackBonus} 伤害");
			_enemyPendingAttackBonus = 0;
		}

		int damage = ApplyOutgoingDamageModifiers(baseDamage, _enemyStatuses);
		damage = ApplyIncomingDamageModifiers(damage, _playerStatuses);
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

		TryReflectDamage(actualDamage);
	}

	private void EnemyHeal(int healAmount)
	{
		if (healAmount <= 0) return;
		_currentEnemyHealth = Math.Min(_currentEnemyMaxHealth, _currentEnemyHealth + healAmount);
		_eventBus.EmitEnemyDamaged(_currentEnemyId, 0);
	}

	private void ApplySummonBuff()
	{
		if (_enemySummonedCount >= 2)
		{
			GD.Print("召唤上限已达，未获得额外增益");
			return;
		}

		_enemySummonedCount++;
		_currentEnemyBaseAttack += 2;
		GD.Print($"召唤增益触发：当前召唤计数 {_enemySummonedCount}/2，敌方基础攻击 +2");
	}

	private void TryApplyEnemyRagePassive()
	{
		if (_enemyEnraged) return;
		if (!string.Equals(_currentEnemyId, "enemy_locust_matriarch_juvenile", StringComparison.OrdinalIgnoreCase)) return;
		if (_currentEnemyMaxHealth <= 0) return;

		float healthRatio = (float)_currentEnemyHealth / _currentEnemyMaxHealth;
		if (healthRatio <= 0.30f)
		{
			_enemyEnraged = true;
			_currentEnemyBaseAttack += 4;
			GD.Print("蝗虫母体·幼体进入狂暴：基础攻击 +4，不再优先召唤。");
		}
	}

	private void TryApplyOverlordEvolution()
	{
		if (!string.Equals(_currentEnemyId, "enemy_locust_matriarch_overlord", StringComparison.OrdinalIgnoreCase)) return;
		if (_currentEnemyHealth <= 0) return;
		if (_overlordEvolutionStacks >= 5) return;

		_overlordEvolutionHitCounter++;
		if (_overlordEvolutionHitCounter >= 10)
		{
			_overlordEvolutionHitCounter = 0;
			_overlordEvolutionStacks++;
			_currentEnemyBaseAttack += 2;
			_currentEnemyBaseDefense += 2;
			_currentEnemyAttack = _currentEnemyBaseAttack;
			_currentEnemyDefense = _currentEnemyBaseDefense;
			GD.Print($"主宰进化触发：攻击+2，防御+2（{_overlordEvolutionStacks}/5）");
		}
	}
	
	private void StartPlayerTurn()
	{
		_isPlayerTurn = true;
		_playerEnergy = _playerData?.Energy ?? 3; // 重置能量
		_playerExciteDrawCountThisTurn = 0;
		_cardsPlayedThisTurn = 0;
		_skillsPlayedThisTurn = 0;
		_playerDefenseThisTurn = (_playerData?.Defense ?? 0); // 重新计算初始护甲，这里简化处理
		ProcessTurnStartStatuses(isPlayer: true);

		if (TryConsumeSkipTurn(_playerStatuses))
		{
			GD.Print("玩家因控制效果跳过本回合");
			EndPlayerTurn();
			return;
		}
		
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

	public int GetDrawPileCount()
	{
		return _playerDrawPile.Count;
	}

	public int GetDiscardPileCount()
	{
		return _playerDiscardPile.Count;
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

	private void ApplyStatus(Dictionary<string, CombatStatusRuntime> targetStatuses, string statusId, int stacks, int duration)
	{
		if (stacks <= 0) return;
		var data = GetStatusData(statusId);
		if (targetStatuses.TryGetValue(statusId, out var status))
		{
			if (data != null && data.MaxStacks > 0)
			{
				status.Stacks = Math.Min(data.MaxStacks, status.Stacks + stacks);
			}
			else
			{
				status.Stacks += stacks;
			}
			status.RemainingTurns = Math.Max(status.RemainingTurns, duration);
		}
		else
		{
			targetStatuses[statusId] = new CombatStatusRuntime
			{
				Id = statusId,
				Stacks = data != null && data.MaxStacks > 0 ? Math.Min(data.MaxStacks, stacks) : stacks,
				RemainingTurns = duration
			};
		}
	}

	private bool HasStatus(Dictionary<string, CombatStatusRuntime> targetStatuses, string statusId)
	{
		return targetStatuses.TryGetValue(statusId, out var status) && status.Stacks > 0;
	}

	private int GetStatusStacks(Dictionary<string, CombatStatusRuntime> targetStatuses, string statusId)
	{
		return targetStatuses.TryGetValue(statusId, out var status) ? status.Stacks : 0;
	}

	private StatusEffectData GetStatusData(string statusId)
	{
		var dm = GetNodeOrNull<DataManager>("/root/DataManager");
		return dm?.GetStatusEffect(statusId);
	}

	private int ApplyOutgoingDamageModifiers(int baseDamage, Dictionary<string, CombatStatusRuntime> attackerStatuses)
	{
		int damage = baseDamage;
		damage += GetStatusStacks(attackerStatuses, "strength") * 3;
		damage = Mathf.RoundToInt(damage * (1.0f - 0.1f * GetStatusStacks(attackerStatuses, "weak")));
		return Math.Max(0, damage);
	}

	private int ApplyIncomingDamageModifiers(int baseDamage, Dictionary<string, CombatStatusRuntime> defenderStatuses)
	{
		int damage = Mathf.RoundToInt(baseDamage * (1.0f + 0.2f * GetStatusStacks(defenderStatuses, "vulnerable")));
		return Math.Max(0, damage);
	}

	private int GetEffectiveEnemyDefense()
	{
		float defense = _currentEnemyDefense;
		defense *= (1.0f + 0.5f * GetStatusStacks(_enemyStatuses, "ironwall"));
		defense *= Math.Max(0.1f, 1.0f - 0.2f * GetStatusStacks(_enemyStatuses, "armor_break"));
		return Math.Max(0, Mathf.RoundToInt(defense));
	}

	private void ApplyLifestealIfAny(int dealtDamage)
	{
		if (!HasStatus(_playerStatuses, "lifesteal") || _gameManager == null) return;
		int healAmount = Math.Max(1, Mathf.RoundToInt(dealtDamage * 0.2f));
		_gameManager.HealPlayer(healAmount);
	}

	private void TryReflectDamage(int actualDamageToPlayer)
	{
		if (actualDamageToPlayer <= 0) return;
		if (!HasStatus(_playerStatuses, "reflect")) return;
		ApplyPoisonDamageToEnemy(3);
	}

	private void ApplyStatusAfterPlayCard()
	{
		if (HasStatus(_playerStatuses, "curse") && _gameManager != null)
		{
			int curseDamage = 2 * Math.Max(1, GetStatusStacks(_playerStatuses, "curse"));
			_gameManager.ApplyDamageToPlayer(curseDamage);
		}

		if (HasStatus(_playerStatuses, "excite") && _playerExciteDrawCountThisTurn < 2)
		{
			DrawCard();
			_playerExciteDrawCountThisTurn++;
		}
	}

	public int GetEnemyStatusStacksPublic(string statusId)
	{
		return GetStatusStacks(_enemyStatuses, statusId);
	}
	
	public int GetPlayerStatusStacksPublic(string statusId)
	{
		return GetStatusStacks(_playerStatuses, statusId);
	}

	private bool TryConsumeSkipTurn(Dictionary<string, CombatStatusRuntime> statuses)
	{
		if (HasStatus(statuses, "stun"))
		{
			statuses.Remove("stun");
			return true;
		}
		if (HasStatus(statuses, "freeze"))
		{
			var rng = new Random();
			if (rng.NextDouble() <= 0.2)
			{
				statuses.Remove("freeze");
				return true;
			}
		}
		return false;
	}

	private void ProcessTurnStartStatuses(bool isPlayer)
	{
		var statuses = isPlayer ? _playerStatuses : _enemyStatuses;
		int maxHealth = isPlayer ? (_playerData?.MaxHealth ?? 100) : _currentEnemyMaxHealth;

		if (HasStatus(statuses, "poison"))
		{
			int stacks = GetStatusStacks(statuses, "poison");
			// 还原设定：根据最大生命值百分比造成伤害
			int damage = Math.Max(1, Mathf.RoundToInt(maxHealth * 0.04f * stacks));
			if (isPlayer)
			{
				_gameManager?.ApplyDamageToPlayer(damage);
			}
			else
			{
				ApplyPoisonDamageToEnemy(damage);
			}
		}

		if (HasStatus(statuses, "vigor") && isPlayer)
		{
			int heal = Math.Max(1, 2 * GetStatusStacks(statuses, "vigor"));
			_gameManager?.HealPlayer(heal);
		}
	}

	private void ProcessTurnEndStatuses(bool isPlayer)
	{
		var statuses = isPlayer ? _playerStatuses : _enemyStatuses;
		int maxHealth = isPlayer ? (_playerData?.MaxHealth ?? 100) : _currentEnemyMaxHealth;

		if (HasStatus(statuses, "burn"))
		{
			int stacks = GetStatusStacks(statuses, "burn");
			// 还原设定：根据最大生命值百分比造成伤害
			int damage = Math.Max(1, Mathf.RoundToInt(maxHealth * 0.08f * stacks));
			if (isPlayer)
			{
				_gameManager?.ApplyDamageToPlayer(damage);
			}
			else
			{
				ApplyPoisonDamageToEnemy(damage);
			}
			statuses["burn"].Stacks = Math.Max(0, statuses["burn"].Stacks - 1);
		}

		var keys = new List<string>(statuses.Keys);
		foreach (var key in keys)
		{
			var status = statuses[key];
			if (status.RemainingTurns > 0)
			{
				status.RemainingTurns--;
			}

			if (status.Stacks <= 0 || status.RemainingTurns == 0)
			{
				statuses.Remove(key);
			}
		}
	}

	private void ApplyNextBattleStatusFromPermanentUpgrades()
	{
		if (_playerData?.PermanentUpgrades == null) return;
		if (_playerData.PermanentUpgrades.TryGetValue("gain_strength_stacks", out var strength) && strength > 0)
		{
			ApplyStatus(_playerStatuses, "strength", (int)strength, 1);
		}
		if (_playerData.PermanentUpgrades.TryGetValue("gain_ironwall_stacks", out var ironwall) && ironwall > 0)
		{
			ApplyStatus(_playerStatuses, "ironwall", (int)ironwall, 2);
		}
	}
}
