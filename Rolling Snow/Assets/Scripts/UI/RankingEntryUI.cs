using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RankingEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rankLabel;
    [SerializeField] private TextMeshProUGUI nameLabel;
    [SerializeField] private TextMeshProUGUI scoreLabel;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.4f, 0.25f);

    public void SetEntry(int rank, string nickname, int score, bool highlight)
    {
        if (rankLabel != null)
            rankLabel.text = rank > 0 ? rank.ToString() : "-";
        if (nameLabel != null)
            nameLabel.text = string.IsNullOrEmpty(nickname) ? "-" : nickname;
        if (scoreLabel != null)
            scoreLabel.text = score.ToString();

        if (highlightImage != null)
        {
            highlightImage.gameObject.SetActive(highlight);
            if (highlight)
                highlightImage.color = highlightColor;
        }
    }
}
