using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

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
        CoinHUD.Instance?.Set(RunManager.I != null ? RunManager.I.CoinsThisRun : 0);
    }

    public void Add(int amount)
    {
        RunManager.I?.AddCoins(amount);
    }

}
