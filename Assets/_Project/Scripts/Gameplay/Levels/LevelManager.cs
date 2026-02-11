using System;
using UnityEngine;
using Unity.Cinemachine;

public class LevelManager : MonoBehaviour
{
    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera virtualCamera;
    public static LevelManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private LevelDatabase database;

    [Header("Current Level")]
    [SerializeField] private string startLevelId = "W1-01";

    [Header("References")]
    [SerializeField] private GameObject player; // arraste seu Player da cena

    public event Action<LevelData> OnLevelLoaded;
    public event Action<LevelData> OnLevelCompleted;

    private LevelData currentLevel;
    private GameObject currentLevelInstance;

    private float levelTimer = 0f;
    private int deathsThisRun = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Se você quiser persistir depois:
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        LoadLevelById(startLevelId);
    }

    private void Update()
    {
        if (currentLevel != null)
            levelTimer += Time.deltaTime;
    }

    public void LoadLevelById(string levelId)
    {
        var level = database.GetById(levelId);
        if (level == null)
        {
            Debug.LogError($"[LevelManager] LevelData não encontrado: {levelId}");
            return;
        }

        LoadLevel(level);
    }

    public void LoadLevel(LevelData level)
    {
        // limpa fase anterior
        if (currentLevelInstance != null)
            Destroy(currentLevelInstance);

        currentLevel = level;
        currentLevelInstance = Instantiate(level.levelPrefab);

        UpdateCinemachineBounds();
        ResetRunStats();
        TeleportPlayerToSpawn();

        OnLevelLoaded?.Invoke(level);
    }

    public void RespawnPlayer()
    {
        deathsThisRun++;
        TeleportPlayerToSpawn();
    }

    public void CompleteLevel()
    {
        if (currentLevel == null) return;

        // Aqui depois entra: salvar estrelas / desbloquear próxima / etc.
        OnLevelCompleted?.Invoke(currentLevel);

        Debug.Log($"[LevelManager] Level completo: {currentLevel.levelId} | Tempo: {levelTimer:F2}s | Mortes: {deathsThisRun}");
    }

    private void TeleportPlayerToSpawn()
    {
        if (player == null)
        {
            Debug.LogError("[LevelManager] Referência do Player não setada.");
            return;
        }

        player.transform.position = currentLevel.playerSpawnPosition;

        // Se usar Rigidbody2D, zera velocidade pra evitar “herdar” queda
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero; // Se der erro, troque por rb.velocity
    }

    private void ResetRunStats()
    {
        levelTimer = 0f;
        deathsThisRun = 0;
    }

    private void UpdateCinemachineBounds()
    {
        if (virtualCamera == null)
        {
            Debug.LogWarning("[LevelManager] VirtualCamera não atribuída. Pulando update de bounds.");
            return;
        }

        var confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();
        if (confiner == null)
        {
            Debug.LogWarning("[LevelManager] CinemachineConfiner2D não encontrado na VirtualCamera.");
            return;
        }

        if (currentLevelInstance == null)
        {
            Debug.LogWarning("[LevelManager] currentLevelInstance é null.");
            return;
        }

        var provider = currentLevelInstance.GetComponentInChildren<CameraBoundsProvider>(true);
        if (provider == null)
        {
            Debug.LogWarning("[LevelManager] CameraBoundsProvider não encontrado dentro da fase.");
            return;
        }

        confiner.BoundingShape2D = provider.BoundsCollider;
        // Garante que o confiner recalcule o cache (importante em runtime)
        confiner.InvalidateBoundingShapeCache();
    }
}
