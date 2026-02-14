using UnityEngine;

public class Damageable : MonoBehaviour
{
    [SerializeField] private int hp = 3;

    public void TakeDamage(int amount)
    {
        hp -= Mathf.Max(0, amount);
        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }
}
