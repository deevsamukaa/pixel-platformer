using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FpsCounter : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    float dt;

    void Update()
    {
        dt += (Time.unscaledDeltaTime - dt) * 0.1f;
        float fps = 1f / dt;
        if (text != null) text.text = $"FPS: {fps:0}";
    }
}