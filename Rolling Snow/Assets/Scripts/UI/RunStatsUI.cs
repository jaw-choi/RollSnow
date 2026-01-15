using TMPro;
using UnityEngine;

public class RunStatsUI : MonoBehaviour
{
    [Header("Labels")]
    [SerializeField] private TextMeshProUGUI speedLabel;
    [SerializeField] private TextMeshProUGUI distanceLabel;
    [SerializeField] private TextMeshProUGUI sizeLabel;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private TextMeshProUGUI goldLabel;

    [Header("Formats")]
    [SerializeField] private string speedFormat = "Speed : {0:F1}";
    [SerializeField] private string distanceFormat = "Distance : {0}m";
    [SerializeField] private string sizeFormat = "Size : {0:F2}";
    [SerializeField] private string scoreFormat = "Score : {0}";
    [SerializeField] private string goldFormat = "Gold : {0}";
    [SerializeField] private LocalizedString speedFormatLocalized;
    [SerializeField] private LocalizedString distanceFormatLocalized;
    [SerializeField] private LocalizedString sizeFormatLocalized;
    [SerializeField] private LocalizedString scoreFormatLocalized;
    [SerializeField] private LocalizedString goldFormatLocalized;

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying())
            return;

        string speedFormatResolved = LocalizationUtility.Resolve(speedFormatLocalized, speedFormat);
        string distanceFormatResolved = LocalizationUtility.Resolve(distanceFormatLocalized, distanceFormat);
        string sizeFormatResolved = LocalizationUtility.Resolve(sizeFormatLocalized, sizeFormat);
        string scoreFormatResolved = LocalizationUtility.Resolve(scoreFormatLocalized, scoreFormat);
        string goldFormatResolved = LocalizationUtility.Resolve(goldFormatLocalized, goldFormat);

        if (speedLabel != null)
            speedLabel.text = string.Format(speedFormatResolved, gm.GetCurrentSpeed());

        if (distanceLabel != null)
            distanceLabel.text = string.Format(distanceFormatResolved, Mathf.FloorToInt(gm.GetDistanceDescended()));

        if (sizeLabel != null)
            sizeLabel.text = string.Format(sizeFormatResolved, gm.GetCurrentSize());

        if (scoreLabel != null)
            scoreLabel.text = string.Format(scoreFormatResolved, gm.GetDisplayScore());

        if (goldLabel != null)
            goldLabel.text = string.Format(goldFormatResolved, gm.GetRunGoldEarned());
    }
}
