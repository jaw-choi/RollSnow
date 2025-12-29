using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Figma/Figma Prefab Map", fileName = "FigmaPrefabMap")]
public class FigmaPrefabMap : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string key;        // e.g. "tree 5", "rock 3"
        public GameObject prefab; // your obstacle prefab with collider, scripts, etc.
    }

    public List<Entry> entries = new List<Entry>();

    public bool TryGetPrefab(string key, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrEmpty(key)) return false;

        for (int i = 0; i < entries.Count; i++)
        {
            Entry e = entries[i];
            if (e == null) continue;
            if (e.prefab == null) continue;
            if (string.Equals(e.key, key, StringComparison.Ordinal))
            {
                prefab = e.prefab;
                return true;
            }
        }
        return false;
    }
}
