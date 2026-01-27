using UnityEngine;

public class TrackSession : MonoBehaviour
{
    public static TrackSession Instance { get; private set; }
    public TrackData SelectedTrack { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetTrack(TrackData track)
    {
        SelectedTrack = track;
    }

    // ✅ TrackSession 오브젝트를 씬에 안 둬도 자동 생성되게
    public static TrackSession Ensure()
    {
        if (Instance != null) return Instance;

        var go = new GameObject("TrackSession");
        Instance = go.AddComponent<TrackSession>();
        DontDestroyOnLoad(go);
        return Instance;
    }
}
