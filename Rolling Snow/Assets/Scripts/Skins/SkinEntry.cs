using UnityEngine;

[System.Serializable]
public class SkinEntry
{
    public string id;
    public string displayName;
    public RandomRewardButton.SkinRarity rarity = RandomRewardButton.SkinRarity.Common;
    public Sprite sprite;
}
