using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class RunManager : MonoBehaviour
{
    public bool IsInfinityCheckpointPending { get; private set; }

    public static RunManager I { get; private set; }

    [Header("Modes (assign in inspector)")]
    [SerializeField] private GameModeDefinition casualMode;
    [SerializeField] private GameModeDefinition fullMode;
    [SerializeField] private GameModeDefinition infinityMode;

    [Header("Revive (Rewarded Ad)")]
    [SerializeField] private bool enableReviveAd = true;
    [SerializeField] private float reviveInvulnerableSeconds = 3f;
    [SerializeField] private bool reviveRestoreFullHP = true;

    [Header("Run State (Debug)")]
    [SerializeField] private GameModeDefinition currentMode;
    public GameModeDefinition CurrentMode => currentMode;

    public int Seed { get; private set; }
    public int CurrentStage { get; private set; } = 1;

    public int CoinsThisRun { get; private set; }
    public bool IsRunActive { get; private set; }

    // Death pipeline (revive)
    public bool IsDeathPending { get; private set; }
    public bool ReviveUsedThisRun { get; private set; }
    private Vector3 _cachedDeathPos;
    private PlayerController _cachedPlayer;

    // Infinity checkpoint system
    public float PendingCheckpointBonusPercent { get; private set; } = 0f;
    public int AccumulatedCheckpointBonusCoins { get; private set; } = 0;

    public event Action OnCoinsChanged;
    public event Action OnStageChanged;
    public event Action<int, float> OnCheckpointReached;

    // UI hooks (pra você plugar painéis simples)
    public event Action OnReviveOffered;  // mostrar painel revive
    public event Action<RewardCalculator.Result> OnRunEnded; // mostrar resumo (futuro)

    private const string SCENE_RUN = "RunGameplay";
    private const string SCENE_MENU = "MainMenu";

    [System.Serializable]
    public struct RunResult
    {
        public bool valid;
        public bool victory;
        public string modeName;
        public int stageReached;
        public int seed;

        public int baseCoins;
        public int completionBonusCoins;
        public int checkpointBonusCoins;

        public int payout;          // total que o jogador recebe (após penalidade/bônus)
        public bool reviveUsed;
    }

    public RunResult LastResult { get; private set; }
    public bool HasLastResult => LastResult.valid;


    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Start Run API (Menu)

    public void StartCasualRun() => StartNewRun(casualMode);
    public void StartFullRun() => StartNewRun(fullMode);
    public void StartInfinityRun() => StartNewRun(infinityMode);

    public void StartNewRun(GameModeDefinition mode)
    {
        LastResult = new RunResult { valid = false };

        if (mode == null)
        {
            Debug.LogError("[RunManager] Mode NULL. Arraste os GameModeDefinition no inspector.");
            return;
        }

        currentMode = mode;
        Seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        CurrentStage = 1;
        CoinsThisRun = 0;

        // revive
        IsDeathPending = false;
        ReviveUsedThisRun = false;
        _cachedPlayer = null;

        // infinity
        AccumulatedCheckpointBonusCoins = 0;
        PendingCheckpointBonusPercent =
            (currentMode.type == GameModeType.Infinity)
                ? Mathf.Max(0f, currentMode.checkpointBonusPercent)
                : 0f;

        IsRunActive = true;
        OnCoinsChanged?.Invoke();
        OnStageChanged?.Invoke();

        SceneManager.LoadScene(SCENE_RUN);
    }

    #endregion

    #region Coins

    public void AddCoins(int rawAmount)
    {
        if (!IsRunActive || IsDeathPending || currentMode == null) return;

        int amount = Mathf.Max(0, rawAmount);
        float mult = Mathf.Max(0f, currentMode.perCollectMultiplier);
        int finalAmount = Mathf.FloorToInt(amount * mult);

        CoinsThisRun += Mathf.Max(0, finalAmount);
        OnCoinsChanged?.Invoke();

    }

    #endregion

    #region Death / Revive

    /// <summary>
    /// Chamado pelo PlayerController.Die() quando Run está ativa.
    /// Aqui NÃO encerra imediatamente: oferece revive (se aplicável).
    /// </summary>
    public void RequestPlayerDeath(PlayerController player)
    {
        if (!IsRunActive || currentMode == null) return;
        if (IsDeathPending) return;

        IsDeathPending = true;
        _cachedPlayer = player;
        _cachedDeathPos = (player != null) ? player.transform.position : Vector3.zero;

        // Decide se oferece revive
        bool canOfferRevive = enableReviveAd && !ReviveUsedThisRun;

        if (canOfferRevive)
        {
            // UI mostra painel: Reviver (AD) / Desistir
            OnReviveOffered?.Invoke();
            Debug.Log("[RunManager] Death pending -> Offer revive AD.");
        }
        else
        {
            // encerra direto
            FinalizeDeath_NoRevive();
        }
    }

    /// <summary>
    /// Chamado pela UI quando o jogador escolhe NÃO reviver.
    /// </summary>
    public void PlayerDeclinedRevive()
    {
        if (!IsDeathPending) return;
        FinalizeDeath_NoRevive();
    }

    /// <summary>
    /// Chamado pela UI quando o jogador clicou em "Reviver com AD".
    /// Aqui você pluga o SDK real depois; por ora tem stub.
    /// </summary>
    public void PlayerWantsRevive()
    {
        if (!IsDeathPending) return;

        // TODO: trocar por integração real de Rewarded Ad.
        // Por enquanto: simula sucesso.
        bool adWatchedSuccessfully = true;

        if (!adWatchedSuccessfully)
        {
            FinalizeDeath_NoRevive();
            return;
        }

        DoRevive();
    }

    private void DoRevive()
    {
        ReviveUsedThisRun = true;
        IsDeathPending = false;

        if (_cachedPlayer == null)
        {
            Debug.LogWarning("[RunManager] Revive sem player cacheado. Encerrando.");
            FinalizeDeath_NoRevive();
            return;
        }

        // Reativa player no ponto onde morreu
        _cachedPlayer.ReviveAt(_cachedDeathPos);

        // HP + invulnerabilidade
        var hp = _cachedPlayer.GetComponent<PlayerHealth>();
        if (hp != null)
        {
            if (reviveRestoreFullHP) hp.ResetFull();
            hp.GrantInvulnerability(reviveInvulnerableSeconds);
        }

        Debug.Log($"[RunManager] Revived! Invuln={reviveInvulnerableSeconds}s | Coins kept={CoinsThisRun}");
    }

    private void FinalizeDeath_NoRevive()
    {
        IsDeathPending = false;
        IsRunActive = false;

        var result = RewardCalculator.Death(currentMode, CoinsThisRun);

        Progression.AddMetaCoins(result.totalBeforeAd);

        LastResult = new RunResult
        {
            valid = true,
            victory = false,
            modeName = currentMode != null ? currentMode.displayName : "(null)",
            stageReached = CurrentStage,
            seed = Seed,
            baseCoins = CoinsThisRun,
            completionBonusCoins = result.completionBonusCoins,
            checkpointBonusCoins = 0,
            payout = result.totalBeforeAd,   // aqui entra o % de morte (ex 40% / 20%)
            reviveUsed = ReviveUsedThisRun
        };

        SceneManager.LoadScene(SCENE_MENU);
    }

    #endregion

    #region Victory

    private void OnRunVictory()
    {
        if (!IsRunActive || currentMode == null) return;

        IsRunActive = false;

        var result = RewardCalculator.Victory(currentMode, CoinsThisRun, AccumulatedCheckpointBonusCoins);

        Progression.AddMetaCoins(result.totalBeforeAd);

        LastResult = new RunResult
        {
            valid = true,
            victory = true,
            modeName = currentMode != null ? currentMode.displayName : "(null)",
            stageReached = CurrentStage,
            seed = Seed,
            baseCoins = CoinsThisRun,
            completionBonusCoins = result.completionBonusCoins,
            checkpointBonusCoins = result.checkpointBonusCoins,
            payout = result.totalBeforeAd,
            reviveUsed = ReviveUsedThisRun
        };

        SceneManager.LoadScene(SCENE_MENU);

    }

    #endregion

    #region Stage Progression

    public void OnStageGatePassed()
    {
        if (!IsRunActive || IsDeathPending || currentMode == null) return;

        // ✅ Se já tem checkpoint pendente, ignora qualquer gate até o player decidir
        if (currentMode.type == GameModeType.Infinity && IsInfinityCheckpointPending)
            return;

        int completedStage = CurrentStage;

        // Finito: se concluiu o último gate -> vitória
        if (!currentMode.IsInfinite && completedStage >= currentMode.maxStages)
        {
            OnRunVictory();
            return;
        }

        // Infinity: checkpoint a cada 5 (ou pelo asset)
        if (currentMode.type == GameModeType.Infinity &&
            currentMode.checkpointEveryStages > 0 &&
            (completedStage % currentMode.checkpointEveryStages == 0))
        {
            IsInfinityCheckpointPending = true;

            OnCheckpointReached?.Invoke(completedStage, PendingCheckpointBonusPercent);
            Debug.Log($"[RunManager] Infinity Checkpoint at END of stage {completedStage}. Pending=+{PendingCheckpointBonusPercent * 100f:0}%");
            return;
        }

        // Caso normal: avança imediatamente
        CurrentStage++;
        OnStageChanged?.Invoke();
    }

    public void ClaimInfinityCheckpointReward()
    {
        if (!IsRunActive || IsDeathPending || currentMode == null) return;
        if (currentMode.type != GameModeType.Infinity) return;

        float p = Mathf.Max(0f, PendingCheckpointBonusPercent);
        int bonus = Mathf.FloorToInt(CoinsThisRun * p);
        AccumulatedCheckpointBonusCoins += Mathf.Max(0, bonus);

        PendingCheckpointBonusPercent = Mathf.Max(0f, currentMode.checkpointBonusPercent);

        Debug.Log($"[RunManager] Infinity Claim! BonusCoins={bonus} Accumulated={AccumulatedCheckpointBonusCoins}");
    }

    public void SkipInfinityCheckpointReward()
    {
        if (!IsRunActive || IsDeathPending || currentMode == null) return;
        if (currentMode.type != GameModeType.Infinity) return;

        float mult = Mathf.Max(1f, currentMode.checkpointSkipMultiplier);
        PendingCheckpointBonusPercent *= mult;

        Debug.Log($"[RunManager] Infinity Skip! NewPending=+{PendingCheckpointBonusPercent * 100f:0}%");
    }

    #endregion

    public void ClearLastResult()
    {
        LastResult = new RunResult { valid = false };
    }

    public void EndInfinityRunNow()
    {
        if (!IsRunActive || IsDeathPending || currentMode == null) return;
        if (currentMode.type != GameModeType.Infinity) return;

        // Finaliza como “cashout”: base + bônus acumulados (e bônus final se você tiver)
        IsRunActive = false;

        var result = RewardCalculator.Victory(currentMode, CoinsThisRun, AccumulatedCheckpointBonusCoins);

        // Se você já implementou LastResult, salve aqui também
        // LastResult = ...

        Debug.Log($"[RunManager] INFINITY CASHOUT! Base={CoinsThisRun} Checkpoints={result.checkpointBonusCoins} Total={result.totalBeforeAd}");

        SceneManager.LoadScene("MainMenu");
    }

    public void InfinityContinue()
    {
        if (!IsRunActive || IsDeathPending || currentMode == null) return;
        if (currentMode.type != GameModeType.Infinity) return;
        if (!IsInfinityCheckpointPending) return;

        // não coleta agora => aumenta pending
        float mult = Mathf.Max(1f, currentMode.checkpointSkipMultiplier);
        PendingCheckpointBonusPercent *= mult;

        IsInfinityCheckpointPending = false;

        // ✅ agora sim avança para o próximo stage
        CurrentStage++;
        OnStageChanged?.Invoke();

        Debug.Log($"[RunManager] Infinity CONTINUE -> NewPending=+{PendingCheckpointBonusPercent * 100f:0}% | NextStage={CurrentStage}");
    }


    public void InfinityCashoutAndEndRun()
    {
        if (!IsRunActive || IsDeathPending || currentMode == null) return;
        if (currentMode.type != GameModeType.Infinity) return;
        if (!IsInfinityCheckpointPending) return;

        float p = Mathf.Max(0f, PendingCheckpointBonusPercent);
        int bonus = Mathf.FloorToInt(CoinsThisRun * p);
        AccumulatedCheckpointBonusCoins += Mathf.Max(0, bonus);

        PendingCheckpointBonusPercent = Mathf.Max(0f, currentMode.checkpointBonusPercent);
        IsInfinityCheckpointPending = false;

        Debug.Log($"[RunManager] Infinity CASHOUT -> Bonus={bonus} Accumulated={AccumulatedCheckpointBonusCoins}");

        OnRunVictory();
    }

}
