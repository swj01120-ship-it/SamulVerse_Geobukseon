using UnityEngine;

public class NoteStartSync : MonoBehaviour
{
    [Header("Start when song starts")]
    public Behaviour[] enableOnStart; // BeatMapSpawner, RhythmGameManager, ComboSystem 등

    private void Awake()
    {
        // 시작 전 OFF (원치 않으면 이 부분 제거 가능)
        if (enableOnStart != null)
        {
            for (int i = 0; i < enableOnStart.Length; i++)
                if (enableOnStart[i] != null)
                    enableOnStart[i].enabled = false;
        }
    }

    private void OnEnable()
    {
        MainGameAutoStartController.OnSongStart += HandleStart;
    }

    private void OnDisable()
    {
        MainGameAutoStartController.OnSongStart -= HandleStart;
    }

    private void HandleStart()
    {
        if (enableOnStart != null)
        {
            for (int i = 0; i < enableOnStart.Length; i++)
                if (enableOnStart[i] != null)
                    enableOnStart[i].enabled = true;
        }
    }
}
