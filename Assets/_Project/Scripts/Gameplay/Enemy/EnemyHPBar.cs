using UnityEngine;
using UnityEngine.UI;

public class EnemyHPBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    public void SetFill(float value)
    {
        fillImage.fillAmount = Mathf.Clamp01(value);
    }
}
