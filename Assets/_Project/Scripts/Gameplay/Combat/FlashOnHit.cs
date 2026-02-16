using System.Collections;
using UnityEngine;

public class FlashOnHit : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SpriteRenderer sr;

    [Header("Materials")]
    [Tooltip("Material que usa o shader Custom/SpriteFlash (Mat_SpriteFlash).")]
    [SerializeField] private Material flashMaterial;

    [Tooltip("Se vazio, usa o material original do SpriteRenderer.")]
    [SerializeField] private Material originalMaterial;

    [Header("Flash Settings")]
    [SerializeField] private float flashTime = 0.08f;
    [SerializeField] private Color flashColor = Color.white;

    private Material runtimeFlashMat;
    private Coroutine co;

    private static readonly int FlashAmount = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    private void Awake()
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        Debug.Log($"[FlashOnHit] sr={(sr != null ? sr.name : "NULL")} on {gameObject.name}");

        if (sr == null) return;

        if (originalMaterial == null)
            originalMaterial = sr.sharedMaterial; // guarda original

        Debug.Log($"[FlashOnHit] mat before={sr.sharedMaterial?.shader?.name}");

        // ✅ instancia o material de FLASH (não o material atual)
        if (flashMaterial != null)
        {
            runtimeFlashMat = Instantiate(flashMaterial);
            runtimeFlashMat.SetColor(FlashColorId, flashColor);
            runtimeFlashMat.SetFloat(FlashAmount, 0f);
        }
        else
        {
            Debug.LogWarning($"[FlashOnHit] flashMaterial NÃO atribuído em {gameObject.name}. Não vai piscar.");
        }

        Debug.Log($"[FlashOnHit] mat after={sr.sharedMaterial?.shader?.name}");
    }

    public void Flash()
    {
        if (sr == null) return;
        if (runtimeFlashMat == null) return;

        if (co != null) StopCoroutine(co);
        co = StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        // aplica material de flash
        sr.material = runtimeFlashMat;
        runtimeFlashMat.SetFloat(FlashAmount, 1f);

        yield return new WaitForSeconds(flashTime);

        runtimeFlashMat.SetFloat(FlashAmount, 0f);

        // volta pro original
        sr.material = originalMaterial;
        co = null;
    }
}
