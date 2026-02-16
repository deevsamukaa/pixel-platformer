using UnityEngine;

public static class Progression
{
    private static SaveData _data;

    public static SaveData Data
    {
        get
        {
            if (_data == null) _data = SaveSystem.Load();
            return _data;
        }
    }

    public static int MetaCoins => Data.metaCoins;

    public static void AddMetaCoins(int amount)
    {
        int a = Mathf.Max(0, amount);
        Data.metaCoins += a;
        SaveSystem.Save(Data);
    }

    public static bool IsUnlocked(string id) => Data.IsUnlocked(id);

    public static void Unlock(string id)
    {
        Data.Unlock(id);
        SaveSystem.Save(Data);
    }

    public static void ReloadFromDisk()
    {
        _data = SaveSystem.Load();
    }
}
