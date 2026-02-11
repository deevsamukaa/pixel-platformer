using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelCompleteUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject rootPanel;
    [SerializeField] private TMP_Text titleText;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button levelSelectButton; // opcional

    private void Awake()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (nextButton != null) nextButton.onClick.AddListener(OnNextClicked);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
        if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OnLevelSelectClicked);
    }

    public void Show(string levelId, bool hasNextLevel)
    {
        if (titleText != null)
            titleText.text = $"Fase Finalizada! ({levelId})";

        if (nextButton != null)
            nextButton.gameObject.SetActive(hasNextLevel);

        if (rootPanel != null)
            rootPanel.SetActive(true);
    }

    public void Hide()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
    }

    private void OnNextClicked()
    {
        LevelManager.Instance?.LoadNextLevel();
    }

    private void OnRestartClicked()
    {
        LevelManager.Instance?.RestartLevel();
    }

    private void OnLevelSelectClicked()
    {
        // M2: pode ficar vazio por enquanto.
        // Depois a gente abre uma cena/menu de seleção.
        Debug.Log("[LevelCompleteUI] Level Select ainda não implementado (M2).");
    }
}
