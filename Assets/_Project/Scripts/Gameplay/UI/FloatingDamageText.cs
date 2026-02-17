using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float lifeTime = 0.8f;

    private TMP_Text text;
    private float timer;

    private void Awake()
    {
        text = GetComponent<TMP_Text>();
    }

    public void SetValue(int value)
    {
        if (value > 0)
        {
            text.text = $"-{value}";
        }
    }

    private void Update()
    {
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        timer += Time.deltaTime;
        if (timer >= lifeTime)
            Destroy(gameObject);
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}
