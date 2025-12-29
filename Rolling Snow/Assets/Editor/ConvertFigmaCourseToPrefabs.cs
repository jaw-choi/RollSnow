using UnityEngine;
using UnityEditor;

public static class ConvertFigmaCourseToPrefabs
{
    // If true, remove previously generated root before regenerating
    private const bool RecreateOutputRoot = true;

    // Output root suffix
    private const string OutputSuffix = "_Prefabs";

    // If true, copy LOCAL transform values (recommended for keeping layout identical)
    private const bool CopyLocalTransform = true;

    // If true, use sprite-name as key instead of gameobject name
    private const bool UseSpriteNameAsKey = false;

    // If true, disable original sprite objects after conversion (keeps them for reference)
    private const bool DisableOriginals = false;

    // If true, match visual size by comparing source sprite bounds vs prefab sprite bounds
    private const bool MatchVisualSizeBySpriteBounds = true;

    [MenuItem("Tools/Figma/Convert Selected UI Root To Prefabs")]
    private static void ConvertSelected()
    {
        Transform root = Selection.activeTransform;
        if (root == null) return;

        FigmaPrefabMap mapAsset = FindPrefabMapAsset();
        if (mapAsset == null) return;

        Transform outRoot = PrepareOutputRoot(root);

        SpriteRenderer[] srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        if (srs == null || srs.Length == 0) return;

        for (int i = 0; i < srs.Length; i++)
        {
            SpriteRenderer src = srs[i];
            if (src == null) continue;
            if (src.sprite == null) continue;

            string key = GetKey(src);
            if (!mapAsset.TryGetPrefab(key, out GameObject prefab) || prefab == null)
                continue;

            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (inst == null) continue;

            // Keep the same name as original
            inst.name = src.gameObject.name;

            // Parent to output root first, then copy transform
            inst.transform.SetParent(outRoot, false);

            if (CopyLocalTransform)
            {
                inst.transform.localPosition = GetLocalPositionRelativeToRoot(src.transform, root);
                inst.transform.localRotation = GetLocalRotationRelativeToRoot(src.transform, root);
                inst.transform.localScale = GetLocalScaleRelativeToRoot(src.transform, root);
            }
            else
            {
                inst.transform.SetParent(outRoot, true);
                inst.transform.position = src.transform.position;
                inst.transform.rotation = src.transform.rotation;
                inst.transform.localScale = src.transform.lossyScale;
            }

            // Scale correction: match visual size when prefab sprite differs from source sprite
            if (MatchVisualSizeBySpriteBounds)
            {
                SpriteRenderer dstSr = inst.GetComponentInChildren<SpriteRenderer>(true);
                if (dstSr != null && dstSr.sprite != null)
                {
                    Vector2 srcSize = src.sprite.bounds.size;
                    Vector2 dstSize = dstSr.sprite.bounds.size;

                    Vector3 s = inst.transform.localScale;

                    if (dstSize.x > 0f) s.x *= (srcSize.x / dstSize.x);
                    if (dstSize.y > 0f) s.y *= (srcSize.y / dstSize.y);

                    inst.transform.localScale = s;
                }
            }

            if (DisableOriginals)
                src.gameObject.SetActive(false);
        }
    }

    private static string GetKey(SpriteRenderer src)
    {
        if (UseSpriteNameAsKey && src.sprite != null)
            return src.sprite.name;

        return src.gameObject.name;
    }

    private static Transform PrepareOutputRoot(Transform selectedRoot)
    {
        string outName = selectedRoot.name + OutputSuffix;

        Transform existing = null;
        Transform parent = selectedRoot.parent;

        if (parent != null)
            existing = parent.Find(outName);
        else
        {
            GameObject found = GameObject.Find(outName);
            if (found != null) existing = found.transform;
        }

        if (existing != null && RecreateOutputRoot)
        {
            Object.DestroyImmediate(existing.gameObject);
            existing = null;
        }

        if (existing != null)
            return existing;

        GameObject outObj = new GameObject(outName);
        Transform outRoot = outObj.transform;

        outRoot.SetParent(parent, false);

        // Match selected root transform so local copying works cleanly
        outRoot.position = selectedRoot.position;
        outRoot.rotation = selectedRoot.rotation;
        outRoot.localScale = selectedRoot.localScale;

        return outRoot;
    }

    // Convert child's world transform into selected root local space
    private static Vector3 GetLocalPositionRelativeToRoot(Transform child, Transform root)
    {
        return root.InverseTransformPoint(child.position);
    }

    private static Quaternion GetLocalRotationRelativeToRoot(Transform child, Transform root)
    {
        return Quaternion.Inverse(root.rotation) * child.rotation;
    }

    private static Vector3 GetLocalScaleRelativeToRoot(Transform child, Transform root)
    {
        // Approximation: derive relative scale from lossyScale ratios
        Vector3 c = child.lossyScale;
        Vector3 r = root.lossyScale;

        float sx = (r.x != 0f) ? (c.x / r.x) : c.x;
        float sy = (r.y != 0f) ? (c.y / r.y) : c.y;
        float sz = (r.z != 0f) ? (c.z / r.z) : c.z;

        return new Vector3(sx, sy, sz);
    }

    private static FigmaPrefabMap FindPrefabMapAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:FigmaPrefabMap");
        if (guids == null || guids.Length == 0) return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        if (string.IsNullOrEmpty(path)) return null;

        return AssetDatabase.LoadAssetAtPath<FigmaPrefabMap>(path);
    }
}
