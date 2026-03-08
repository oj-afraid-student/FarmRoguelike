// DataTypes/GameEnums.cs
using System;

public static class GameEnums
{
    public enum GameState
    {
        MainMenu,
        MapExploration,
        PreCombat,
        Combat,
        Farming,
        RewardSelection,
        GameOver,
        EnderChest
    }

    public enum CardType
    {
        Attack,     // 攻击牌
        Skill,      // 技能牌
        Ability,    // 能力牌
    }

    public enum PlayerStatType
    {
        Health,
        MaxHealth,
        Attack,
        Defense,
        Energy,
        Speed,
        Luck
    }

    public enum RoomType
    {
        Combat,
        Boss,
        Reward,
        Trap
    }

    public enum RewardType
    {
        StatIncrease,
        CardReward,
        ItemReward,
        GoldReward
    }

    // 作物效果类型
    public enum CropEffectType
    {
        StatBoost,      // 属性加成（如提高生命上限）
        CurseTrade,     // 诅咒权衡（降低速度，提高攻击）
        Forget          // 遗忘（删除卡牌或属性）
    }
}