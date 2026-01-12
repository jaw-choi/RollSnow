using UnityEngine;

public class PopupController : MonoBehaviour
{
    [SerializeField] private GameObject overlay; // 전체화면 배경(클릭 감지)
    [SerializeField] private GameObject popup;   // 실제 팝업(이미지 포함)

    void Start()
    {
        Close();
    }

    public void Open()
    {
        //if (overlay != null) overlay.SetActive(true);
        if (popup != null) popup.SetActive(true);
    }

    public void Close()
    {
        if (popup != null) popup.SetActive(false);
        //if (overlay != null) overlay.SetActive(false);
    }
}
