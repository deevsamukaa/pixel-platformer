using TMPro;
using UnityEngine;

public class MetaCoinsHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text txt;

    private void Awake()
    {
        if (txt == null) txt = GetComponentInChildren<TMP_Text>(true);
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (txt != null)
            txt.text = Progression.MetaCoins.ToString();
    }
}
