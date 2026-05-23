using UnityEngine;

public class PlayerPopupText : MonoBehaviour
{
    [SerializeField] private Transform popupAnchor;
    [SerializeField] private PopupText popupPrefab;
    [SerializeField] private string startMessage = "Run for your dream";
    [SerializeField] private float startMessageDuration = 2f;

    private void Start()
    {
        ShowPopup(startMessage, startMessageDuration);
    }

    public void ShowPopup(string message)
    {
        ShowPopup(message, 2f);
    }

    public void ShowPopup(string message, float duration)
    {
        if (popupAnchor == null || popupPrefab == null)
            return;

        PopupText popup = Instantiate(popupPrefab, popupAnchor.position, Quaternion.identity, popupAnchor);
        popup.Setup(message, duration);
    }
}
