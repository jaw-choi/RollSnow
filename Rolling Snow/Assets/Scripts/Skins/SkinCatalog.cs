using UnityEngine;

[CreateAssetMenu(menuName = "RollSnow/Skin Catalog", fileName = "SkinCatalog")]
public class SkinCatalog : ScriptableObject
{
    public string defaultSkinId = "skin_0";
    public int defaultSkinIndex = 0;
    public string unlockPrefix = SkinStorage.DefaultUnlockPrefix;
    public string equippedKey = SkinStorage.DefaultEquippedKey;
    public SkinEntry[] skins;

    public string GetDefaultSkinId()
    {
        if (!string.IsNullOrEmpty(defaultSkinId))
            return defaultSkinId;

        if (skins == null || skins.Length == 0)
            return "skin_0";

        int index = Mathf.Clamp(defaultSkinIndex, 0, skins.Length - 1);
        return SkinStorage.GetSkinId(skins[index], index);
    }

    public int FindSkinIndex(string skinId)
    {
        if (skins == null || skins.Length == 0 || string.IsNullOrEmpty(skinId))
            return -1;

        for (int i = 0; i < skins.Length; i++)
        {
            if (SkinStorage.GetSkinId(skins[i], i) == skinId)
                return i;
        }

        return -1;
    }

    public SkinEntry FindSkin(string skinId)
    {
        int index = FindSkinIndex(skinId);
        if (index < 0 || skins == null || index >= skins.Length)
            return null;

        return skins[index];
    }
}
