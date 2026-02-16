using UnityEngine;

public enum GameModeType
{
    Casual,
    Full,
    Infinity
}

[CreateAssetMenu(menuName = "_Project/Run/Game Mode Definition", fileName = "GM_")]
public class GameModeDefinition : ScriptableObject
{
    [Header("Identity")]
    public GameModeType type;
    public string displayName;

    [Header("Stages")]
    [Tooltip("-1 = infinito")]
    public int maxStages = 5;

    [Header("Rewards (base)")]
    [Tooltip("Multiplicador aplicado em cada coleta (Infinity = 1.2).")]
    public float perCollectMultiplier = 1f;

    [Tooltip("Percentual mantido se morrer antes (Casual/Full=0.4, Infinity=0.2).")]
    [Range(0f, 1f)] public float keepOnDeathPercent = 0.4f;

    [Tooltip("Bônus ao finalizar a run (Casual=+50% => 0.5, Full=+100% => 1.0). Infinity geralmente 0 aqui.")]
    [Range(0f, 5f)] public float completionBonusPercent = 0.5f;

    [Header("Ads")]
    public bool allowRewardAdDouble = true;

    [Header("Infinity Checkpoints")]
    [Tooltip("A cada N fases, oferece bônus. (Infinity = 5)")]
    public int checkpointEveryStages = 5;

    [Tooltip("Bônus inicial no checkpoint (Infinity = +50% => 0.5).")]
    public float checkpointBonusPercent = 0.5f;

    [Tooltip("Se o player NÃO coletar e avançar, multiplica o bônus pendente por 1.25 (=> +25%).")]
    public float checkpointSkipMultiplier = 1.25f;

    public bool IsInfinite => maxStages < 0;
}
