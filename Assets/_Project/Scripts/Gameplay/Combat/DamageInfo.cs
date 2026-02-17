using UnityEngine;

public struct DamageInfo
{
    public int damage;
    public Vector2 hitDirection;
    public float knockbackForce;
    public GameObject source;

    public Vector2 hitPoint;
    public Vector2 hitNormal;
    public GameObject hitVfxPrefab;

    public DamageInfo(int damage, Vector2 dir, float kb, GameObject source,
                      Vector2 hitPoint, Vector2 hitNormal, GameObject hitVfxPrefab = null)
    {
        this.damage = damage;
        this.hitDirection = dir;
        this.knockbackForce = kb;
        this.source = source;
        this.hitPoint = hitPoint;
        this.hitNormal = hitNormal;
        this.hitVfxPrefab = hitVfxPrefab;
    }
}
