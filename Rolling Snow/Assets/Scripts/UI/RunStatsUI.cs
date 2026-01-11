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
    [SerializeField] private string distanceFormat = "Distance : {0:F1}m";
    [SerializeField] private string sizeFormat = "Size : {0:F2}";
    [SerializeField] private string scoreFormat = "Score : {0}";
    [SerializeField] private string goldFormat = "Gold : {0}";

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying())
            return;

        if (speedLabel != null)
            speedLabel.text = string.Format(speedFormat, gm.GetCurrentSpeed());

        if (distanceLabel != null)
            distanceLabel.text = string.Format(distanceFormat, gm.GetDistanceDescended());

        if (sizeLabel != null)
            sizeLabel.text = string.Format(sizeFormat, gm.GetCurrentSize());

        if (scoreLabel != null)
            scoreLabel.text = string.Format(scoreFormat, gm.GetDisplayScore());

        if (goldLabel != null)
            goldLabel.text = string.Format(goldFormat, gm.GetRunGoldEarned());
    }
}
