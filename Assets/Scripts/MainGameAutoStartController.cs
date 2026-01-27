using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class MainGameAutoStartController : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Header("Audio")]
    public MusicManager musicManager;
    public bool startMusicHere = true;

    [Header("Ready UI (선택)")]
    public GameObject readyUI;
    [Min(0f)] public float readySeconds = 3.0f;

    // ✅ BeatMapSpawner가 찾는 값들
    public static double SongStartDspTime { get; private set; } = -1;
    public static bool SongStarted { get; private set; } = false;

    public static event Action OnSongStart;

    private bool startedEventFired = false;

    private void Awake()
    {
        if (musicManager == null) musicManager = FindObjectOfType<MusicManager>();
    }

    private void Start()
    {
        // ✅ 재진입/재시작 대비 초기화
        SongStarted = false;
        SongStartDspTime = -1;
        startedEventFired = false;

        StartCoroutine(CoStartFlow());
    }

    private IEnumerator CoStartFlow()
    {
        TrackData track = null;
        if (TrackSession.Instance != null)
            track = TrackSession.Instance.SelectedTrack;

        if (track == null)
        {
            Debug.LogError("[MainGameAutoStart] SelectedTrack 없음 (Home에서 선곡 후 Start로 넘어왔는지 확인)");
            yield break;
        }

        if (track.gameVideo == null)
        {
            Debug.LogError("[MainGameAutoStart] TrackData.gameVideo가 비어있음");
            yield break;
        }

        if (startMusicHere && track.audioClip == null)
        {
            Debug.LogError("[MainGameAutoStart] TrackData.audioClip이 비어있음");
            yield break;
        }

        // Ready UI
        if (readyUI != null) readyUI.SetActive(true);
        if (readySeconds > 0f) yield return new WaitForSeconds(readySeconds);
        if (readyUI != null) readyUI.SetActive(false);

        // Video 준비
        if (videoPlayer == null)
        {
            Debug.LogError("[MainGameAutoStart] videoPlayer 미연결");
            yield break;
        }

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.Stop();

        videoPlayer.clip = track.gameVideo;
        videoPlayer.Prepare();

        float timeout = 10f;
        float t = 0f;
        while (!videoPlayer.isPrepared && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError("[MainGameAutoStart] Video Prepare timeout");
            yield break;
        }

        // ✅ “카운트다운 끝난 시작 순간” 기록
        SongStartDspTime = AudioSettings.dspTime;
        SongStarted = true;

        // 음악 시작
        if (startMusicHere)
        {
            if (musicManager == null)
            {
                Debug.LogError("[MainGameAutoStart] MusicManager를 찾지 못함 (씬에 MusicManager 필요)");
                yield break;
            }

            musicManager.SetMusic(track.audioClip, track.bpm);
            musicManager.PlayMusic();
        }

        // 영상 시작
        videoPlayer.Play();

        // 1프레임 후 이벤트
        yield return null;
        FireSongStartOnce();
    }

    private void FireSongStartOnce()
    {
        if (startedEventFired) return;
        startedEventFired = true;

        OnSongStart?.Invoke();
        Debug.Log($"[MainGameAutoStart] ✅ OnSongStart fired (SongStarted={SongStarted}, dsp={SongStartDspTime:F4})");
    }
}
