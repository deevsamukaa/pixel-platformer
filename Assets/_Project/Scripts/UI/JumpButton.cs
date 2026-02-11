using UnityEngine;
using UnityEngine.EventSystems;

public class JumpButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
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

    public void OnPointerUp(PointerEventData eventData)
    {
        if (inputUI != null)
            inputUI.JumpReleased();
    }

    private void OnDisable()
    {
        if (inputUI != null)
            inputUI.JumpReleased();
    }
}
