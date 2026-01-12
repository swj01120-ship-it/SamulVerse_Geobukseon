using UnityEngine;
using UnityEngine.Video;

public class TrackSelectorManager : MonoBehaviour
{
    public static TrackSelectorManager Instance;

    [Header("3개의 TrackSelector")]
    [SerializeField] private TrackSelector trackSelector01;
    [SerializeField] private TrackSelector trackSelector02;
    [SerializeField] private TrackSelector trackSelector03;

    private TrackSelector currentPlayingSelector = null;
    private VideoPlayer currentPlayingVideo = null;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public void OnTrackSelectorStartPlaying(TrackSelector selector, VideoPlayer videoPlayer)
    {
        if (currentPlayingSelector != null && currentPlayingSelector != selector)
            currentPlayingSelector.StopPreview();

        if (currentPlayingVideo != null && currentPlayingVideo != videoPlayer)
            currentPlayingVideo.Stop();

        currentPlayingSelector = selector;
        currentPlayingVideo = videoPlayer;
    }

    public void OnTrackSelectorStopped(TrackSelector selector)
    {
        if (currentPlayingSelector == selector)
        {
            currentPlayingSelector = null;
            currentPlayingVideo = null;
        }
    }

    // ✅ OptionsManager가 Music 슬라이더 움직일 때 호출할 “정답 함수”
    public void ApplyVolumeToCurrentPreview(float v)
    {
        if (currentPlayingSelector != null)
            currentPlayingSelector.ApplyPreviewVolume(v);
    }
}
