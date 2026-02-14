using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "_Project/ProcGen/SegmentDatabase")]
public class SegmentDatabase : ScriptableObject
{
    public List<SegmentSpec> segments = new();
}
