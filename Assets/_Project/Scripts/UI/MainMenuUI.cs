using UnityEngine;

public class MainMenuUI : MonoBehaviour
{
    public void StartCasualRun() => RunManager.I.StartCasualRun();
    public void StartFullRun() => RunManager.I.StartFullRun();
    public void StartInfinityRun() => RunManager.I.StartInfinityRun();

    public void QuitGame() => Application.Quit();
}
