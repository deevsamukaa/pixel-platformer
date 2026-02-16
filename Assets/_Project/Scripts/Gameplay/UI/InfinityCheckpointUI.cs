using UnityEngine;
using TMPro;

public class InfinityCheckpointUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject rootPanel; // começa desativado (Active=false)

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;

    [Header("Behavior")]
    [SerializeField] private bool pauseGameWhileOpen = true;

    private int _stage;
    private float _pendingPercent;

    private void Awake()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (RunManager.I != null)
            RunManager.I.OnCheckpointReached += HandleCheckpointReached;
    }

    private void OnDisable()
    {
        if (RunManager.I != null)
            RunManager.I.OnCheckpointReached -= HandleCheckpointReached;
    }

    private void HandleCheckpointReached(int stage, float pendingPercent)
    {
        _stage = stage;
        _pendingPercent = pendingPercent;
        Show();
    }

    private void Show()
    {
        if (rootPanel == null)
        {
            Debug.LogError("[InfinityCheckpointUI] rootPanel não atribuído no Inspector.");
            return;
        }

        rootPanel.SetActive(true);
        if (pauseGameWhileOpen) Time.timeScale = 0f;

        if (titleText != null)
            titleText.text = "Checkpoint!";

        int baseCoins = RunManager.I != null ? RunManager.I.CoinsThisRun : 0;
        int bonusIfCashoutNow = Mathf.FloorToInt(baseCoins * Mathf.Max(0f, _pendingPercent));

        if (detailsText != null)
        {
            detailsText.text =
                $"Stage: {_stage}\n" +
                $"Moedas na run: {baseCoins}\n" +
                $"Bônus se encerrar agora: +{bonusIfCashoutNow} ({_pendingPercent * 100f:0}%)\n" +
                $"Continuar sem coletar aumenta o bônus pendente em +25%.";
        }
    }

    private void Hide()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (pauseGameWhileOpen) Time.timeScale = 1f;
    }

    // ✅ Botão: CONTINUAR (não coleta) -> pending *= 1.25
    public void ClickContinue()
    {
        Hide();
        RunManager.I?.InfinityContinue();
    }

    // ✅ Botão: ENCERRAR e COLETAR (cashout)
    public void ClickCashout()
    {
        RunManager.I?.InfinityCashoutAndEndRun();
    }

    // ✅ Opcional: ENCERRAR e COLETAR x2 com AD (stub por enquanto)
    public void ClickCashoutDoubleWithAd()
    {
        Hide();

        // TODO: integrar Rewarded Ad real.
        bool adWatchedSuccessfully = true;

        if (!adWatchedSuccessfully)
        {
            // se falhar, cai no cashout normal
            RunManager.I?.ClaimInfinityCheckpointReward();
            RunManager.I?.EndInfinityRunNow();
            return;
        }

        // Cashout normal
        RunManager.I?.ClaimInfinityCheckpointReward();

        // Dobra apenas o bônus do checkpoint (ou o payout inteiro — você escolhe a regra)
        // Aqui vou dobrar SOMENTE o bônus acumulado do checkpoint por segurança.
        // Se quiser dobrar o payout inteiro, a gente move isso pro RewardCalculator/LastResult.
        if (RunManager.I != null)
        {
            int extra = RunManager.I.AccumulatedCheckpointBonusCoins; // dobra acumulado => soma mais 1x
            // Não existe método público pra adicionar direto, então a forma correta é:
            // - ou criar RunManager.AddCheckpointBonusCoins(extra)
            // - ou guardar esse "ad multiplier" no LastResult
            // Para não inventar agora, deixo só como log.
            Debug.Log($"[InfinityCheckpointUI] AD watched. (TODO) Extra checkpoint bonus = {extra}");
        }

        RunManager.I?.EndInfinityRunNow();
    }
}
