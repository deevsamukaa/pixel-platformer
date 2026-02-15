using System.Collections;
using UnityEngine;

public class HitStop : MonoBehaviour
{
    private static HitStop _instance;

    private void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void Do(float duration = 0.035f, float timeScale = 0.08f)
    {
        if (_instance == null)
        {
            var go = new GameObject("HitStop");
            _instance = go.AddComponent<HitStop>();
        }
        _instance.StartCoroutine(_instance.Routine(duration, timeScale));
    }

    private IEnumerator Routine(float duration, float timeScale)
    {
        float prev = Time.timeScale;
        Time.timeScale = timeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = prev;
    }
}
