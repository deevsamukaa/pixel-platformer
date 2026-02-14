#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RemoveMissingScriptsTool
{
    [MenuItem("Tools/_Project/Remove Missing Scripts (Selected)")]
    public static void RemoveFromSelected()
    {
        var gos = Selection.gameObjects;
        int removed = 0;

        foreach (var go in gos)
        {
            removed += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        Debug.Log($"Removed Missing Scripts from selected objects. Total removed: {removed}");
    }

    [MenuItem("Tools/_Project/Remove Missing Scripts (All in Scene)")]
    public static void RemoveFromScene()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        int removed = 0;

        foreach (var r in roots)
        {
            removed += RemoveRecursively(r);
        }

        Debug.Log($"Removed Missing Scripts from scene. Total removed: {removed}");
    }

    private static int RemoveRecursively(GameObject go)
    {
        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);

        foreach (Transform child in go.transform)
            removed += RemoveRecursively(child.gameObject);

        return removed;
    }
}
#endif
