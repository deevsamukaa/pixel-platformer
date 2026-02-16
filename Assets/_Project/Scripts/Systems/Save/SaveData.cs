using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = SaveSystem.CURRENT_VERSION;

    // Progresso mínimo
    public int metaCoins = 0;

    // Unlock flags (ex: personagens, trinkets, modos, etc.)
    public List<string> unlockedIds = new List<string>();

    public bool IsUnlocked(string id)
        => !string.IsNullOrEmpty(id) && unlockedIds != null && unlockedIds.Contains(id);

    public void Unlock(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (unlockedIds == null) unlockedIds = new List<string>();
        if (!unlockedIds.Contains(id)) unlockedIds.Add(id);
    }
}
