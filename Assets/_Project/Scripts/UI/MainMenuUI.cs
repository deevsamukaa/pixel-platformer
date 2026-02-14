using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void StartCasualRun()
    {
        RunManager.I.StartNewRun(RunMode.Casual5);
    }

    public void StartLongRun()
    {
        RunManager.I.StartNewRun(RunMode.Long10);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
