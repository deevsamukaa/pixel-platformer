using UnityEngine;

public class ReviveUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (RunManager.I != null)
            RunManager.I.OnReviveOffered += Show;
    }

    private void OnDisable()
    {
        if (RunManager.I != null)
            RunManager.I.OnReviveOffered -= Show;
    }

    private void Show()
    {
        if (panel != null) panel.SetActive(true);

        // opcional: pausar o tempo enquanto escolhe
        Time.timeScale = 0f;
    }

    private void Hide()
    {
        if (panel != null) panel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void ClickReviveAd()
    {
        Hide();
        RunManager.I?.PlayerWantsRevive();
    }

    public void ClickDecline()
    {
        Hide();
        RunManager.I?.PlayerDeclinedRevive();
    }
}
