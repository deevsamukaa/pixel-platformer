using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    public int CoinsThisRun { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ResetRun()
    {
        CoinsThisRun = 0;
        CoinHUD.Instance?.Set(CoinsThisRun);
    }

    public void Add(int amount)
    {
        CoinsThisRun += Mathf.Max(0, amount);
        CoinHUD.Instance?.Set(CoinsThisRun);
    }
}
