using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Levels/Level Database", fileName = "LevelDatabase")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelData> levels = new List<LevelData>();

    public LevelData GetById(string id)
    {
        return levels.Find(l => l != null && l.levelId == id);
    }

    public LevelData GetByIndex(int worldIndex, int levelIndex)
    {
        return levels.Find(l => l != null && l.worldIndex == worldIndex && l.levelIndex == levelIndex);
    }
}
