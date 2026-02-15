using System.Collections;
using UnityEngine;

public class CameraShake2D : MonoBehaviour
{
    [SerializeField] private float defaultDuration = 0.08f;
    [SerializeField] private float defaultStrength = 0.12f;

    private Vector3 originalLocalPos;
    private Coroutine co;

    private void Awake()
    {
        originalLocalPos = transform.localPosition;
    }

    public void Shake(float duration = -1f, float strength = -1f)
    {
        if (duration <= 0f) duration = defaultDuration;
        if (strength <= 0f) strength = defaultStrength;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Routine(duration, strength));
    }

    private IEnumerator Routine(float duration, float strength)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            transform.localPosition = originalLocalPos + (Vector3)(Random.insideUnitCircle * strength);
            yield return null;
        }
        transform.localPosition = originalLocalPos;
        co = null;
    }
}
