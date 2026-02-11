using UnityEngine;
using UnityEngine.EventSystems;

public class JumpButton : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private MobileInputUI inputUI;

    private void Awake()
    {
        if (inputUI == null)
            inputUI = FindFirstObjectByType<MobileInputUI>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (inputUI != null)
            inputUI.JumpPressed();
    }
}
