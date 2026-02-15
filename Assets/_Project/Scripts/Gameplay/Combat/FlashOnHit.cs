using System.Collections;
using UnityEngine;

public class FlashOnHit : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private float flashTime = 0.06f;

    private Color original;
    private Coroutine co;

    private void Awake()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) original = sr.color;
    }

    public void Flash()
    {
        if (sr == null) return;
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(flashTime);
        sr.color = original;
        co = null;
    }
}
