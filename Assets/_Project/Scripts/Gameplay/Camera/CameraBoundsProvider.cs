using UnityEngine;

/// <summary>
/// Coloque este script no GameObject "CameraBounds" dentro do prefab da fase.
/// Ele expõe o Collider2D que o Confiner deve usar.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CameraBoundsProvider : MonoBehaviour
{
    public Collider2D BoundsCollider => GetComponent<Collider2D>();
}