using UnityEngine;

/// <summary>
/// Подменяет визуал под дочерним Model: удаляет существующих детей и инстанцирует префаб tier в локальном нуле.
/// </summary>
public static class ComponentModelBinder
{
    public static void BindVisual(Transform root, PcPrefabCatalogMap map, PcComponentData data)
    {
        if (root == null || map == null || data == null)
        {
            return;
        }

        GameObject visualPrefab = map.GetVisualModelForTier(data.CategoryCode, data.ModelTier);
        if (visualPrefab == null)
        {
            return;
        }

        string childName = map.GetModelChildName(data.CategoryCode);
        Transform modelRoot = FindDeepChild(root, childName);
        if (modelRoot == null)
        {
            Debug.LogWarning($"ComponentModelBinder: '{childName}' not found under '{root.name}'.");
            return;
        }

        for (int i = modelRoot.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(modelRoot.GetChild(i).gameObject);
        }

        GameObject instance = Object.Instantiate(visualPrefab, modelRoot);
        instance.name = visualPrefab.name;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeepChild(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
