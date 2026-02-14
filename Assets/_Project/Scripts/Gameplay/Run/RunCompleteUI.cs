using UnityEngine;
using UnityEngine.SceneManagement;

#if TMP_PRESENT
using TMPro;
#endif

public class RunCompleteUI : MonoBehaviour
{
    public enum AutoShowMode
    {
        Never,
        OnMainMenuScene
    }

    [Header("Wiring")]
    [Tooltip("Painel raiz (ativado/desativado). Se vazio, usa o próprio GameObject.")]
    [SerializeField] private GameObject root;

#if TMP_PRESENT
    [Header("Texts (TMP)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
#else
    [Header("Texts")]
    [Tooltip("Se você usa TextMeshPro, instale TMP e remova esta necessidade. Para compilar sem TMP, deixe nulo.")]
    [SerializeField] private UnityEngine.UI.Text titleText;
    [SerializeField] private UnityEngine.UI.Text detailsText;
#endif

    [Header("Behavior")]
    [SerializeField] private AutoShowMode autoShow = AutoShowMode.OnMainMenuScene;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Reset()
    {
        root = gameObject;
    }

    private void Awake()
    {
        if (root == null) root = gameObject;
    }

    private void Start()
    {
        if (autoShow == AutoShowMode.OnMainMenuScene)
        {
            var active = SceneManager.GetActiveScene().name;
            if (active == mainMenuSceneName)
            {
                // Se existir RunManager e a run já terminou, mostramos um resumo.
                // Como ainda não temos um "RunResult" persistido, mostramos o que estiver disponível.
                TryAutoShowFromRunManager();
            }
            else
            {
                Hide();
            }
        }
        else
        {
            Hide();
        }
    }

    private void TryAutoShowFromRunManager()
    {
        if (RunManager.I == null)
        {
            Hide();
            return;
        }

        // Se a run ainda está ativa, não faz sentido mostrar tela de complete.
        // (No seu fluxo atual, RunManager volta pro menu e IsRunActive = false)
        if (RunManager.I.IsRunActive)
        {
            Hide();
            return;
        }

        // Sem um "resultado" explícito, a gente mostra como "Resumo da última run".
        ShowSummary(
            title: "Resumo da run",
            victory: false,
            coinsThisRun: RunManager.I.CoinsThisRun,
            coinsKeptOnDeath: Mathf.FloorToInt(RunManager.I.CoinsThisRun * RunManager.I.keepCoinsOnDeathPercent),
            wasDeath: true // como default, porque no M1 ainda não persistimos “foi vitória”
        );
    }

    public void ShowVictory()
    {
        if (RunManager.I == null)
        {
            ShowSummary("Vitória!", victory: true, coinsThisRun: 0, coinsKeptOnDeath: 0, wasDeath: false);
            return;
        }

        ShowSummary(
            title: "Vitória!",
            victory: true,
            coinsThisRun: RunManager.I.CoinsThisRun,
            coinsKeptOnDeath: RunManager.I.CoinsThisRun, // vitória mantém tudo
            wasDeath: false
        );
    }

    public void ShowRunOver()
    {
        if (RunManager.I == null)
        {
            ShowSummary("Run Over", victory: false, coinsThisRun: 0, coinsKeptOnDeath: 0, wasDeath: true);
            return;
        }

        int kept = Mathf.FloorToInt(RunManager.I.CoinsThisRun * RunManager.I.keepCoinsOnDeathPercent);

        ShowSummary(
            title: "Run Over",
            victory: false,
            coinsThisRun: RunManager.I.CoinsThisRun,
            coinsKeptOnDeath: kept,
            wasDeath: true
        );
    }

    private void ShowSummary(string title, bool victory, int coinsThisRun, int coinsKeptOnDeath, bool wasDeath)
    {
        if (root == null) root = gameObject;
        root.SetActive(true);

        string modeStr = "(sem RunManager)";
        string stageStr = "-";
        string seedStr = "-";

        if (RunManager.I != null)
        {
            modeStr = RunManager.I.Mode.ToString();
            stageStr = $"{RunManager.I.CurrentStage}/{RunManager.I.StagesTotal}";
            seedStr = RunManager.I.Seed.ToString();
        }

        SetText(titleText, title);

        // Texto enxuto e útil pra debug
        string details =
            $"Modo: {modeStr}\n" +
            $"Stage: {stageStr}\n" +
            $"Seed: {seedStr}\n" +
            $"Moedas na run: {coinsThisRun}\n" +
            (victory ? $"Recompensa: {coinsThisRun}" : $"Mantidas: {coinsKeptOnDeath}");

        SetText(detailsText, details);
    }

    public void Hide()
    {
        if (root == null) root = gameObject;
        root.SetActive(false);
    }

    public void GoToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void PlayAgainSameMode()
    {
        if (RunManager.I == null)
        {
            SceneManager.LoadScene("RunGameplay");
            return;
        }

        RunManager.I.StartNewRun(RunManager.I.Mode);
    }

    private void SetText(object textComponent, string value)
    {
        if (textComponent == null) return;

#if TMP_PRESENT
        if (textComponent is TMP_Text tmp) tmp.text = value;
#else
        if (textComponent is UnityEngine.UI.Text txt) txt.text = value;
#endif
    }
}
