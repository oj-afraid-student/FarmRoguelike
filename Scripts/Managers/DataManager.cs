using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

public partial class DataManager : Node
{
    private Dictionary<string, CardData> _cards = new();
    private Dictionary<string, EnemyData> _enemies = new();
    private Dictionary<string, CropData> _crops = new();
    private Dictionary<string, object> _items = new();
    
    // 简单测试数据
    private void InitializeTestData()
    {
        // 测试卡牌数据
        _cards["card_attack_basic"] = new CardData
        {
            Id = "card_attack_basic",
            Name = "基础攻击",
            Description = "造成基础伤害",
            Type = GameEnums.CardType.Attack,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "damage", 6 } }
        };
        
        _cards["card_defend_basic"] = new CardData
        {
            Id = "card_defend_basic",
            Name = "基础防御",
            Description = "获得防御",
            Type = GameEnums.CardType.Skill,
            Cost = 1,
            Effects = new Dictionary<string, float> { { "defense", 5 } }
        };
        
        // 测试敌人数据
        _enemies["enemy_slime"] = new EnemyData
        {
            Id = "enemy_slime",
            Name = "史莱姆",
            Health = 30,
            Attack = 5,
            Defense = 0
        };
        
        // 测试作物数据
        _crops["crop_wheat"] = new CropData
        {
            Id = "crop_wheat",
            Name = "小麦",
            GrowthTimeSeconds = 300, // 5分钟
            Rewards = new CropReward
            {
                HealthRestore = 20,
                Gold = 10
            }
        };
    }
    
    public override void _Ready()
    {
        InitializeTestData();
        GD.Print($"数据加载完成: {_cards.Count}卡牌, {_enemies.Count}敌人, {_crops.Count}作物");
    }
    
    // 数据获取方法
    public CardData GetCard(string id)
    {
        return _cards.GetValueOrDefault(id);
    }
    
    public EnemyData GetEnemy(string id)
    {
        return _enemies.GetValueOrDefault(id);
    }
    
    public CropData GetCrop(string id)
    {
        return _crops.GetValueOrDefault(id);
    }
    
    public List<CardData> GetAllCards()
    {
        return new List<CardData>(_cards.Values);
    }
    
    public List<CropData> GetAllCrops()
    {
        return new List<CropData>(_crops.Values);
    }
}