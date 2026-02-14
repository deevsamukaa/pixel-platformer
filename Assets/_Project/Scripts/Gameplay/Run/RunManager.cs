using UnityEngine;
using UnityEngine.SceneManagement;

public enum RunMode
{
    Casual5 = 5,
    Long10 = 10
}

public class RunManager : MonoBehaviour
{
    public static RunManager I { get; private set; }

    [Header("Run State (Debug)")]
    public RunMode Mode = RunMode.Casual5;
    public int Seed = 0;
    public int CurrentStage = 1;
    public int StagesTotal => (int)Mode;

    public int CoinsThisRun { get; private set; }

    [Header("Rewards")]
    [Range(0f, 1f)] public float keepCoinsOnDeathPercent = 0.4f;

    public bool IsRunActive { get; private set; }

    private const string SCENE_RUN = "RunGameplay";
    private const string SCENE_MENU = "MainMenu";

    private void Awake()
    {
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartNewRun(RunMode mode)
    {
        Mode = mode;
        Seed = Random.Range(int.MinValue, int.MaxValue);
        CurrentStage = 1;
        CoinsThisRun = 0;
        IsRunActive = true;

        SceneManager.LoadScene(SCENE_RUN);
    }

    public void AddCoins(int amount)
    {
        if (!IsRunActive) return;
        CoinsThisRun += Mathf.Max(0, amount);
    }

    public void OnPlayerDied()
    {
        if (!IsRunActive) return;

        IsRunActive = false;
        int coinsKept = Mathf.FloorToInt(CoinsThisRun * keepCoinsOnDeathPercent);

        // TODO (M4): salvar em Progression/SaveSystem (moedas totais)
        Debug.Log($"[RunManager] Run Over! Coins this run={CoinsThisRun} | kept={coinsKept}");

        SceneManager.LoadScene(SCENE_MENU);
    }

    public void OnStageCompleted()
    {
        if (!IsRunActive) return;

        if (CurrentStage >= StagesTotal)
        {
            OnRunVictory();
            return;
        }

        CurrentStage++;
        Debug.Log($"[RunManager] Stage++ => {CurrentStage}/{StagesTotal}");
        // SEM SceneManager.LoadScene aqui.
    }


    private void OnRunVictory()
    {
        IsRunActive = false;

        // TODO (M4): salvar CoinsThisRun inteiro
        Debug.Log($"[RunManager] VICTORY! Coins earned={CoinsThisRun}");

        SceneManager.LoadScene(SCENE_MENU);
    }
}
