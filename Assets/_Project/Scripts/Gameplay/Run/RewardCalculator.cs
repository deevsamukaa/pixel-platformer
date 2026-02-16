using UnityEngine;

public static class RewardCalculator
{
    public struct Result
    {
        public int baseCoins;
        public int completionBonusCoins;
        public int checkpointBonusCoins;
        public int totalBeforeAd;
        public bool canDoubleWithAd;
        public int totalIfDoubled;
    }

    public static Result Victory(GameModeDefinition mode, int baseCoins, int checkpointBonusCoins)
    {
        int completionBonus = Mathf.FloorToInt(baseCoins * Mathf.Max(0f, mode.completionBonusPercent));
        int total = baseCoins + completionBonus + Mathf.Max(0, checkpointBonusCoins);

        bool canDouble = mode.allowRewardAdDouble;
        return new Result
        {
            baseCoins = baseCoins,
            completionBonusCoins = completionBonus,
            checkpointBonusCoins = checkpointBonusCoins,
            totalBeforeAd = total,
            canDoubleWithAd = canDouble,
            totalIfDoubled = canDouble ? total * 2 : total
        };
    }

    public static Result Death(GameModeDefinition mode, int baseCoins)
    {
        int kept = Mathf.FloorToInt(baseCoins * Mathf.Clamp01(mode.keepOnDeathPercent));
        bool canDouble = mode.allowRewardAdDouble; // se quiser, você pode desativar no death também.

        return new Result
        {
            baseCoins = baseCoins,
            completionBonusCoins = 0,
            checkpointBonusCoins = 0,
            totalBeforeAd = kept,
            canDoubleWithAd = canDouble,
            totalIfDoubled = canDouble ? kept * 2 : kept
        };
    }
}
