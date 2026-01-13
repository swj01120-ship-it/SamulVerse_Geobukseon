using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainGameAutoStartController : MonoBehaviour
{
    [Header("TEST MODE")]
    public bool forceStartForTest = true;      // ✅ 켜두면 선택 트랙 없어도 fallback으로 시작
    public TrackData fallbackTrackForTest;     // ✅ MainGame 단독 테스트용 트랙

    [Header("Video Output")]
    public VideoPlayer videoPlayer;
    public RawImage gameVideoRawImage;         // ✅ VideoScreen의 RawImage 연결
    public GameObject gameVideoRoot;           // (선택) 영상 루트 켜기/끄기

    [Header("Ready UI")]
    public GameObject readyUI;
    public Text readyCountText;               // Legacy Text
    [Min(0f)] public float readySeconds = 1.0f;

    [Header("Gameplay Refs")]
    public BeatMapSpawner beatMapSpawner;

    [Header("Safety")]
    [Min(1f)] public float videoPrepareTimeoutSeconds = 10f;

    [Header("Debug")]
    public bool verboseLog = true;

    public static event Action OnSongStart;

    // ✅ BeatMapSpawner 호환: 이미 시작됐는지 체크하는 플래그
    public static bool SongStarted { get; private set; } = false;

    private bool started = false;

    private void Awake()
    {
        // Ready/UI/코루틴 멈춤 방지
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 씬 재진입 시 초기화
        started = false;
        SongStarted = false;
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.prepareCompleted += OnPrepared;

            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.errorReceived += OnVideoError;
        }
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }

    private void Start()
    {
        StartCoroutine(CoStartFlow());
    }

    private IEnumerator CoStartFlow()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 1) 트랙 결정: 선택 트랙 우선, 없으면 fallback
        TrackData track = null;

        if (GameManager.Instance != null)
            track = GameManager.Instance.GetSelectedTrack();

        if (track == null)
            track = fallbackTrackForTest;

        // 테스트 모드 OFF인데 트랙이 없으면 종료
        if (!forceStartForTest && track == null)
        {
            Debug.LogError("[MainGameAutoStart] TrackData 없음 (선택 트랙이 null)");
            yield break;
        }

        if (track == null)
        {
            Debug.LogError("[MainGameAutoStart] fallbackTrackForTest를 인스펙터에 넣어야 테스트 가능");
            yield break;
        }

        if (verboseLog)
            Debug.Log($"[MainGameAutoStart] Using Track: {track.name} / {track.trackName}");

        // 2) 스포너 찾기
        if (beatMapSpawner == null)
            beatMapSpawner = FindObjectOfType<BeatMapSpawner>();

        // 3) Ready 카운트다운(언스케일)
        yield return CoReadyCountdownRealtime(readySeconds);

        // 4) 비트맵 주입(가능하면)
        if (beatMapSpawner != null)
        {
            if (track.beatMap != null)
            {
                beatMapSpawner.SetBeatMap(track.beatMap);
                if (verboseLog) Debug.Log($"[MainGameAutoStart] BeatMap injected: {track.beatMap.name}");
            }
            else
            {
                Debug.LogWarning("[MainGameAutoStart] Track.beatMap이 null (노트 스폰 테스트 불가)");
            }
        }
        else
        {
            Debug.LogWarning("[MainGameAutoStart] BeatMapSpawner 없음 (노트 스폰 테스트 불가)");
        }

        // 5) 영상 재생
        if (videoPlayer == null)
        {
            Debug.LogError("[MainGameAutoStart] videoPlayer 미연결");
            yield break;
        }

        if (gameVideoRawImage == null)
        {
            Debug.LogError("[MainGameAutoStart] gameVideoRawImage 미연결 (영상 출력 안됨)");
            yield break;
        }

        if (track.gameVideo == null)
        {
            Debug.LogError("[MainGameAutoStart] Track.gameVideo가 null");
            yield break;
        }

        if (gameVideoRoot != null) gameVideoRoot.SetActive(true);

        videoPlayer.Stop();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;

        // RawImage에 texture를 붙일 거라 APIOnly 권장
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        gameVideoRawImage.texture = null;

        videoPlayer.clip = track.gameVideo;
        videoPlayer.Prepare();

        float t = 0f;
        while (!videoPlayer.isPrepared && t < videoPrepareTimeoutSeconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError("[MainGameAutoStart] Video Prepare timeout");
            yield break;
        }

        // prepareCompleted가 혹시 누락돼도 여기서 한번 더 강제 연결
        if (videoPlayer.texture != null)
            gameVideoRawImage.texture = videoPlayer.texture;

        videoPlayer.Play();
        if (verboseLog)
            Debug.Log($"[MainGameAutoStart] Video Play() called. isPlaying={videoPlayer.isPlaying}");

        // 6) 시작 신호 (이벤트 + SongStarted 세팅)
        yield return null;
        FireSongStartOnce();

        // ✅ 테스트 목적: 이벤트를 놓치는 경우까지 배제하려고 스포너 직접 호출도 함께
        if (beatMapSpawner != null && track.beatMap != null)
        {
            beatMapSpawner.BeginSpawn();
            if (verboseLog) Debug.Log("[MainGameAutoStart] BeatMapSpawner.BeginSpawn() forced");
        }
    }

    private void OnPrepared(VideoPlayer vp)
    {
        if (gameVideoRawImage != null && vp != null && vp.texture != null)
        {
            gameVideoRawImage.texture = vp.texture;
            if (verboseLog) Debug.Log("[MainGameAutoStart] Video texture assigned to RawImage");
        }
    }

    private void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError($"[MainGameAutoStart] Video error: {msg}");
    }

    private IEnumerator CoReadyCountdownRealtime(float seconds)
    {
        if (readyUI != null) readyUI.SetActive(true);

        if (readyCountText == null)
        {
            if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
            if (readyUI != null) readyUI.SetActive(false);
            yield break;
        }

        float remain = Mathf.Max(0f, seconds);
        int lastInt = int.MinValue;

        while (remain > 0f)
        {
            int cur = Mathf.CeilToInt(remain);
            if (cur != lastInt)
            {
                readyCountText.text = cur.ToString();
                lastInt = cur;
            }

            remain -= Time.unscaledDeltaTime;
            yield return null;
        }

        readyCountText.text = "GO";
        yield return new WaitForSecondsRealtime(0.2f);

        if (readyUI != null) readyUI.SetActive(false);
    }

    private void FireSongStartOnce()
    {
        if (started) return;
        started = true;

        SongStarted = true;            // ✅ BeatMapSpawner가 OnEnable에서 확인
        OnSongStart?.Invoke();

        if (verboseLog) Debug.Log("[MainGameAutoStart] ✅ OnSongStart fired");
    }
}
