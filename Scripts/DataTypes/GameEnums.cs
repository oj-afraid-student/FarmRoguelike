// DataTypes/GameEnums.cs
using System;

public static class GameEnums
{
    public enum GameState
    {
        MainMenu,
        MapExploration,
        Combat,
        Farming,
        RewardSelection,
        GameOver
    }

    public enum CardType
    {
        Attack,     // 攻击牌
        Skill,      // 技能牌
        Ability,    // 能力牌
        Curse       // 诅咒牌
    }

    public enum PlayerStatType
    {
        Health,
        MaxHealth,
        Attack,
        Defense,
        ActionPoints,
        Speed,
        Luck
    }

    public enum RoomType
    {
        Combat,
        Event,
        Shop,
        Rest,
        Boss,
        Farming
    }

    public enum RewardType
    {
        StatIncrease,
        CardReward,
        ItemReward,
        GoldReward
    }
}