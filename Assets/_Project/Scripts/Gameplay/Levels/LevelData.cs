using UnityEngine;

[CreateAssetMenu(menuName = "Game/Levels/Level Data", fileName = "LevelData_")]
public class LevelData : ScriptableObject
{
    [Header("Identity")]
    public string levelId;          // Ex: "W1-01"
    public int worldIndex = 1;       // Mundo 1, 2, 3...
    public int levelIndex = 1;       // Fase 1, 2, 3...

    [Header("Content")]
    public GameObject levelPrefab;  // Prefab da fase (Tilemap + hazards + etc.)

    [Header("Spawn")]
    public Vector2 playerSpawnPosition;

    [Header("Targets (Stars)")]
    public float targetTimeSeconds = 40f;
    public int targetDeaths = 2;
}
