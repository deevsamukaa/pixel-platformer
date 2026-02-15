using UnityEngine;

[System.Serializable]
public struct DamageInfo
{
    public int damage;
    public Vector2 hitDirection;     // direção do empurrão (ideal normalizada)
    public float knockbackForce;     // força do knockback
    public GameObject source;        // quem causou o dano

    public DamageInfo(int damage, Vector2 hitDirection, float knockbackForce, GameObject source = null)
    {
        this.damage = damage;
        this.hitDirection = hitDirection;
        this.knockbackForce = knockbackForce;
        this.source = source;
    }
}
