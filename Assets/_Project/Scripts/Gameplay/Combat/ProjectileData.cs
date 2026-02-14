using UnityEngine;

[CreateAssetMenu(menuName = "_Project/Combat/ProjectileData")]
public class ProjectileData : ScriptableObject
{
    public string id = "rock";
    public GameObject prefab;
    public float speed = 12f;
    public int damage = 1;
    public float cooldown = 0.6f;

    [Tooltip("Offset do spawn em relação ao player (antes de aplicar facing).")]
    public Vector2 spawnOffset = new Vector2(0.6f, 0.2f);
}
