public interface IDamageable
{
    /// <returns>true se o dano foi aplicado (não estava invulnerável, etc)</returns>
    bool TakeDamage(DamageInfo info);
}
