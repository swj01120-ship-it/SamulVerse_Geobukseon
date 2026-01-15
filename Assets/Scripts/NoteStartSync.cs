using UnityEngine;

public class NoteStartSync : MonoBehaviour
{
    [Header("Enable when song starts")]
    public Behaviour[] enableOnStart;

    [Header("Disable when song ends (optional)")]
    public Behaviour[] disableOnEnd;

    [Header("Options")]
    public bool disableEnableOnStartAtAwake = true;
    public bool reEnableOnRestart = true;

    void Awake()
    {
        if (disableEnableOnStartAtAwake && enableOnStart != null)
        {
            for (int i = 0; i < enableOnStart.Length; i++)
                if (enableOnStart[i] != null)
                    enableOnStart[i].enabled = false;
        }
    }

    void OnEnable()
    {
        MainGameAutoStartController.OnSongStart += HandleStart;
        MainGameAutoStartController.OnSongEnd += HandleEnd;

        // 씬 재진입/오브젝트 재활성화 시 이미 시작된 상태라면 즉시 반영
        if (reEnableOnRestart && MainGameAutoStartController.SongStarted)
            HandleStart();

        if (MainGameAutoStartController.SongEnded)
            HandleEnd();
    }

    void OnDisable()
    {
        MainGameAutoStartController.OnSongStart -= HandleStart;
        MainGameAutoStartController.OnSongEnd -= HandleEnd;
    }

    void HandleStart()
    {
        if (enableOnStart == null) return;

        for (int i = 0; i < enableOnStart.Length; i++)
            if (enableOnStart[i] != null)
                enableOnStart[i].enabled = true;
    }

    void HandleEnd()
    {
        if (disableOnEnd == null) return;

        for (int i = 0; i < disableOnEnd.Length; i++)
            if (disableOnEnd[i] != null)
                disableOnEnd[i].enabled = false;
    }
}
