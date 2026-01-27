using UnityEngine;

public class HomeExitOpener : MonoBehaviour
{
    [SerializeField] private GameObject exitMessageCanvas; // Canvas-ExitMessage

    public void OpenExit()
    {
        // Home에서 나가기 -> ExitMessage 열기
        if (exitMessageCanvas != null)
            exitMessageCanvas.SetActive(true);
    }

    public void CloseExit()
    {
        if (exitMessageCanvas != null)
            exitMessageCanvas.SetActive(false);
    }
}
