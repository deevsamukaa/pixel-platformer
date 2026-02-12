using UnityEngine;
using TMPro;

public class CoinHUD : MonoBehaviour
{
    public static CoinHUD Instance { get; private set; }

    [SerializeField] private TMP_Text coinsText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (coinsText == null)
            coinsText = GetComponentInChildren<TMP_Text>(true);
    }

    public void Set(int value)
    {
        if (coinsText != null)
            coinsText.text = value.ToString();
    }
}
