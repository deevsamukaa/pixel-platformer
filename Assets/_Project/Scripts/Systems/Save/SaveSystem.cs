using UnityEngine;

public static class SaveSystem
{
    public const int CURRENT_VERSION = 1;
    private const string KEY = "SAVE_V1_JSON";

    public static SaveData Load()
    {
        if (!PlayerPrefs.HasKey(KEY))
            return NewFresh();

        string json = PlayerPrefs.GetString(KEY, "");
        if (string.IsNullOrEmpty(json))
            return NewFresh();

        SaveData data;
        try
        {
            data = JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            data = NewFresh();
        }

        if (data == null) data = NewFresh();

        // Migração simples (quando você aumentar versões no futuro)
        if (data.version != CURRENT_VERSION)
        {
            data = Migrate(data);
            Save(data);
        }

        return data;
    }

    public static void Save(SaveData data)
    {
        if (data == null) return;
        data.version = CURRENT_VERSION;

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(KEY, json);
        PlayerPrefs.Save();
    }

    public static void WipeAll()
    {
        PlayerPrefs.DeleteKey(KEY);
        PlayerPrefs.Save();
    }

    private static SaveData NewFresh()
    {
        return new SaveData
        {
            version = CURRENT_VERSION,
            metaCoins = 0,
            unlockedIds = new System.Collections.Generic.List<string>()
        };
    }

    private static SaveData Migrate(SaveData oldData)
    {
        // ✅ Migração “cascade”: vai transformando até a versão atual
        if (oldData == null) return NewFresh();

        int v = oldData.version;

        // Exemplo:
        // if (v < 1) { ... set defaults ...; v = 1; }

        // Hoje estamos no v1, então só normaliza
        oldData.version = CURRENT_VERSION;
        if (oldData.unlockedIds == null)
            oldData.unlockedIds = new System.Collections.Generic.List<string>();

        return oldData;
    }
}
