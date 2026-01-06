using System;
using TMPro;
using UnityEngine;

public class GoldTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldLabel;
    [SerializeField] private string format = "{0}";
    [SerializeField] private bool useThousandsSeparator = true;

    void Awake()
    {
        if (goldLabel == null)
            goldLabel = FindGoldLabel();
    }

    void OnEnable()
    {
        var system = GoldSystem.GetOrCreate();
        if (system != null)
            system.GoldChanged += HandleGoldChanged;

        Refresh();
    }

    void OnDisable()
    {
        if (GoldSystem.Instance != null)
            GoldSystem.Instance.GoldChanged -= HandleGoldChanged;
    }

    void HandleGoldChanged(int amount)
    {
        UpdateLabel(amount);
    }

    void Refresh()
    {
        var system = GoldSystem.GetOrCreate();
        if (system == null)
            return;

        UpdateLabel(system.GetGold());
    }

    void UpdateLabel(int amount)
    {
        if (goldLabel == null)
            return;

        string value = useThousandsSeparator ? amount.ToString("N0") : amount.ToString();
        goldLabel.text = string.Format(format, value);
    }

    TextMeshProUGUI FindGoldLabel()
    {
        var labels = GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            var label = labels[i];
            if (label != null && label.name.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0)
                return label;
        }

        return null;
    }
}
