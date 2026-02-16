using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RunCompleteUI : MonoBehaviour
{
    [Header("Wiring")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (root == null) root = gameObject;

        if (titleText == null || detailsText == null)
        {
            var texts = GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0 && titleText == null) titleText = texts[0];
            if (texts.Length > 1 && detailsText == null) detailsText = texts[1];
        }
    }

    private void Start()
    {
        Hide();

        if (SceneManager.GetActiveScene().name != mainMenuSceneName) return;
        if (RunManager.I == null) return;
        if (!RunManager.I.HasLastResult) return;

        Show(RunManager.I.LastResult);
    }

    private void Show(RunManager.RunResult r)
    {
        root.SetActive(true);

        SetText(titleText, r.victory ? "Vitória!" : "Run Over");

        string details =
            $"Modo: {r.modeName}\n" +
            $"Stage: {r.stageReached}\n" +
            $"Seed: {r.seed}\n" +
            $"Moedas coletadas: {r.baseCoins}\n";

        if (r.victory)
        {
            if (r.completionBonusCoins > 0)
                details += $"Bônus final: +{r.completionBonusCoins}\n";
            if (r.checkpointBonusCoins > 0)
                details += $"Bônus checkpoints: +{r.checkpointBonusCoins}\n";
        }
        else
        {
            details += $"Penalidade aplicada (keep %): payout parcial\n";
        }

        if (r.reviveUsed)
            details += $"Revive: usado\n";

        details += $"Recompensa: {r.payout}";

        SetText(detailsText, details);
    }

    public void Hide()
    {
        if (root == null) root = gameObject;
        root.SetActive(false);
    }

    public void GoToMenu()
    {
        // ✅ fecha e consome o resultado (não reaparece)
        Hide();
        RunManager.I?.ClearLastResult();

        // ✅ Se já estamos no menu, NÃO recarrega a cena
        // (se você quiser recarregar mesmo assim, pode, mas limpando o resultado já resolve)
    }


    public void RetrySameMode()
    {
        Hide();
        RunManager.I?.ClearLastResult();

        if (RunManager.I != null && RunManager.I.CurrentMode != null)
            RunManager.I.StartNewRun(RunManager.I.CurrentMode);
        else
            SceneManager.LoadScene("RunGameplay");
    }


    private void SetText(TMP_Text t, string value)
    {
        if (t != null) t.text = value;
    }
}
