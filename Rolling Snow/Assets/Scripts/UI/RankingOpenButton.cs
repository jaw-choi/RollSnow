using UnityEngine;

public class RankingOpenButton : MonoBehaviour
{
    public void OpenRanking()
    {
        RankingPanelUI.TryOpenAndRefresh();
    }

    public void CloseRanking()
    {
        if (RankingPanelUI.Instance != null)
            RankingPanelUI.Instance.Close();
    }
}
