using UnityEngine;

public class SegmentSpec : MonoBehaviour
{
    public Transform playerSpawn;

    [Header("Anchors")]
    public Transform startAnchor;
    public Transform endAnchor;

    [Header("Tags")]
    public SegmentTag tag = SegmentTag.Neutral;

    [Header("Difficulty (optional)")]
    [Range(1, 10)] public int difficulty = 1;

    [Header("Optional Stage Marker")]
    [Tooltip("Se tiver, ao passar por aqui conta +1 'stage' dentro da run.")]
    public StageGate stageGate;

    public Vector3 StartPos => startAnchor != null ? startAnchor.position : transform.position;
    public Vector3 EndPos => endAnchor != null ? endAnchor.position : transform.position + Vector3.right * 10f;
}

public enum SegmentTag
{
    Start,
    Neutral,
    Precision,
    Vertical,
    Treasure,
    Hazard,
    Rest
}
