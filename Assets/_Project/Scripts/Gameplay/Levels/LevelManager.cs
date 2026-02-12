using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private LevelDatabase database;
    [SerializeField] private string startLevelId = "W1-01";

    [Header("References")]
    [SerializeField] private GameObject player; // player fixo na cena
    [SerializeField] private LevelCompleteUI levelCompleteUI;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera cinemachineCamera;

    public event Action<LevelData> OnLevelLoaded;
    public event Action<LevelData> OnLevelCompleted;

    private LevelData currentLevel;
    private GameObject currentLevelInstance;

    private float levelTimer;
    private int deathsThisRun;

    // Progress simples (M2)
    private const string PREF_UNLOCKED = "UNLOCKED_LEVEL_INDEX"; // guarda o maior índice desbloqueado (na ordem do database)

    private List<LevelData> orderedLevels = new List<LevelData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildOrderedLevels();
    }

    private void Start()
    {
        if (levelCompleteUI != null)
            levelCompleteUI.Hide();

        LoadLevelById(startLevelId);
    }

    private void Update()
    {
        if (currentLevel != null)
            levelTimer += Time.deltaTime;
    }

    // -----------------------
    // Public API
    // -----------------------
    public void LoadLevelById(string levelId)
    {
        if (database == null)
        {
            Debug.LogError("[LevelManager] Database está NULL no Inspector.");
            return;
        }

        var level = database.GetById(levelId);
        if (level == null)
        {
            Debug.LogError($"[LevelManager] LevelData não encontrado: {levelId}");
            return;
        }

        LoadLevel(level);
    }

    public void RestartLevel()
    {
        if (currentLevel == null) return;
        if (levelCompleteUI != null) levelCompleteUI.Hide();
        UnfreezePlayer();
        LoadLevel(currentLevel);
    }

    public void LoadNextLevel()
    {
        var next = GetNextLevel(currentLevel);
        if (next == null)
        {
            Debug.Log("[LevelManager] Não existe próxima fase.");
            return;
        }

        if (levelCompleteUI != null) levelCompleteUI.Hide();
        UnfreezePlayer();
        LoadLevel(next);
    }

    public void CompleteLevel()
    {
        if (currentLevel == null) return;

        FreezePlayer();

        // Desbloqueio simples (M2)
        UnlockNextLevel(currentLevel);

        bool hasNext = GetNextLevel(currentLevel) != null;

        Debug.Log($"[LevelManager] Level completo: {currentLevel.levelId} | Tempo: {levelTimer:F2}s | Mortes: {deathsThisRun}");

        SaveBestCoinsForCurrentLevel();

        if (levelCompleteUI != null)
            levelCompleteUI.Show(currentLevel.levelId, hasNext);

        OnLevelCompleted?.Invoke(currentLevel);
    }

    public void RespawnPlayer()
    {
        deathsThisRun++;
        TeleportPlayerToSpawn();
    }

    // -----------------------
    // Internal load
    // -----------------------
    private void LoadLevel(LevelData level)
    {
        if (currentLevelInstance != null)
            Destroy(currentLevelInstance);

        currentLevel = level;
        currentLevelInstance = Instantiate(level.levelPrefab);

        ResetRunStats();
        CoinManager.Instance?.ResetRun();
        TeleportPlayerToSpawn();
        UpdateCinemachineBounds();

        OnLevelLoaded?.Invoke(level);
    }

    private void ResetRunStats()
    {
        levelTimer = 0f;
        deathsThisRun = 0;
    }

    private void TeleportPlayerToSpawn()
    {
        if (player == null)
        {
            Debug.LogError("[LevelManager] Referência do Player não setada.");
            return;
        }

        player.transform.position = currentLevel.playerSpawnPosition;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Se você usa spawnPoint por tag também, recomendo remover do PlayerController depois,
        // e deixar 100% pelo LevelManager pra evitar duplicidade.
    }

    // -----------------------
    // Freeze/Unfreeze
    // -----------------------
    private void FreezePlayer()
    {
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
            pc.enabled = false;
    }

    private void UnfreezePlayer()
    {
        if (player == null) return;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = true;

        var pc = player.GetComponent<PlayerController>();
        if (pc != null)
            pc.enabled = true;
    }

    // -----------------------
    // Cinemachine bounds
    // -----------------------
    private void UpdateCinemachineBounds()
    {
        if (cinemachineCamera == null) return;

        var confiner = cinemachineCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner == null) return;

        var provider = currentLevelInstance.GetComponentInChildren<CameraBoundsProvider>(true);
        if (provider == null) return;

        confiner.BoundingShape2D = provider.BoundsCollider;
        confiner.InvalidateBoundingShapeCache();
    }

    // -----------------------
    // Ordered levels + unlock
    // -----------------------
    private void BuildOrderedLevels()
    {
        orderedLevels.Clear();
        if (database == null || database.levels == null) return;

        orderedLevels.AddRange(database.levels);
        orderedLevels.RemoveAll(l => l == null);

        orderedLevels.Sort((a, b) =>
        {
            int w = a.worldIndex.CompareTo(b.worldIndex);
            if (w != 0) return w;
            return a.levelIndex.CompareTo(b.levelIndex);
        });
    }

    private LevelData GetNextLevel(LevelData level)
    {
        if (level == null) return null;

        int idx = orderedLevels.IndexOf(level);
        if (idx < 0) return null;

        int nextIdx = idx + 1;
        if (nextIdx >= orderedLevels.Count) return null;

        return orderedLevels[nextIdx];
    }

    private void UnlockNextLevel(LevelData completed)
    {
        int idx = orderedLevels.IndexOf(completed);
        if (idx < 0) return;

        int nextIdx = idx + 1;
        int unlocked = PlayerPrefs.GetInt(PREF_UNLOCKED, 0);

        // desbloqueia até a próxima (não passa do fim)
        int newUnlocked = Mathf.Max(unlocked, Mathf.Min(nextIdx, orderedLevels.Count - 1));
        PlayerPrefs.SetInt(PREF_UNLOCKED, newUnlocked);
        PlayerPrefs.Save();
    }

    // (mais tarde a tela de seleção vai ler isso)
    public int GetUnlockedMaxIndex()
    {
        return PlayerPrefs.GetInt(PREF_UNLOCKED, 0);
    }

    public IReadOnlyList<LevelData> GetOrderedLevels()
    {
        return orderedLevels;
    }

    private void SaveBestCoinsForCurrentLevel()
    {
        if (currentLevel == null) return;

        int current = CoinManager.Instance != null ? CoinManager.Instance.CoinsThisRun : 0;
        string key = $"BEST_COINS_{currentLevel.levelId}";

        int best = PlayerPrefs.GetInt(key, -1);
        if (current > best)
        {
            PlayerPrefs.SetInt(key, current);
            PlayerPrefs.Save();
        }
    }
}
