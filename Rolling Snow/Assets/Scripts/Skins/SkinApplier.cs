using UnityEngine;

public class SkinApplier : MonoBehaviour
{
    [SerializeField] private SkinCatalog catalog;
    [SerializeField] private bool applyOnEnable = true;
    [SerializeField] private bool applyToAllRenderers = true;
    [SerializeField] private Transform rendererRoot;
    [SerializeField] private SpriteRenderer targetRenderer;

    void Awake()
    {
        if (!applyOnEnable)
            Apply();
    }

    void OnEnable()
    {
        if (applyOnEnable)
            Apply();
    }

    public void Apply()
    {
        if (catalog == null || catalog.skins == null || catalog.skins.Length == 0)
            return;

        string defaultId = catalog.GetDefaultSkinId();
        string equippedId = SkinStorage.GetEquippedSkinId(defaultId, catalog.equippedKey);
        if (!SkinStorage.IsUnlocked(equippedId, defaultId, catalog.unlockPrefix))
            equippedId = defaultId;

        SkinEntry entry = catalog.FindSkin(equippedId);
        if (entry == null || entry.sprite == null)
            return;

        var renderers = GetRenderers();
        if (renderers == null || renderers.Length == 0)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer != null)
                renderer.sprite = entry.sprite;
        }
    }

    SpriteRenderer[] GetRenderers()
    {
        if (applyToAllRenderers)
        {
            Transform root = rendererRoot != null ? rendererRoot : transform;
            return root.GetComponentsInChildren<SpriteRenderer>(true);
        }

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (targetRenderer == null)
            return null;

        return new[] { targetRenderer };
    }
}
