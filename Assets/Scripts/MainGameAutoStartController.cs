using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainGameAutoStartController : MonoBehaviour
{
    [Header("TEST MODE")]
    public bool forceStartForTest = true;
    public TrackData fallbackTrackForTest;

    [Header("Audio")]
    public AudioSource songAudioSource;
    [Range(0f, 1f)] public float songVolume = 1.0f;

    [Header("Ready UI (Legacy)")]
    public GameObject readyUI;
    public Text readyCountText;
    [Min(0f)] public float readySeconds = 3.0f;

    [Header("Flow References (Optional)")]
    public BeatMapSpawner beatMapSpawner;
    public VideoScreenManager videoScreenManager;

    [Header("Start Timing")]
    [Tooltip("DSP 스케줄 시작까지 여유(초). 0.15~0.30 권장")]
    public float scheduleLeadTime = 0.20f;

    [Header("End Handling")]
    public bool fireSongEndEvent = true;
    [Tooltip("곡 끝난 뒤 OnSongEnd 발사까지 여유(초)")]
    public float endGraceSeconds = 0.15f;

    [Header("Debug")]
    public bool verboseLog = true;

    // ====== Global Events ======
    public static event Action OnSongStart;
    public static event Action OnSongEnd;

    public static bool SongStarted { get; private set; }
    public static bool SongEnded { get; private set; }
    public static double SongStartDspTime { get; private set; } = -1;

    bool _starting;
    Coroutine _endWatcher;

    void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (songAudioSource == null)
        {
            songAudioSource = GetComponent<AudioSource>();
            if (songAudioSource == null) songAudioSource = gameObject.AddComponent<AudioSource>();
        }

        songAudioSource.playOnAwake = false;
        songAudioSource.loop = false;

        // 자동 참조
        if (beatMapSpawner == null) beatMapSpawner = FindObjectOfType<BeatMapSpawner>();
        if (videoScreenManager == null) videoScreenManager = FindObjectOfType<VideoScreenManager>();
    }

    void Start()
    {
        StartCoroutine(CoStartFlow());
    }

    IEnumerator CoStartFlow()
    {
        if (_starting) yield break;
        _starting = true;

        SongStarted = false;
        SongEnded = false;
        SongStartDspTime = -1;

        // 1) 트랙 확보
        TrackData track = TryGetSelectedTrack();
        if (track == null && forceStartForTest) track = fallbackTrackForTest;

        if (track == null)
        {
            Debug.LogError("[MainGameAutoStart] TrackData가 없습니다. (선택 트랙/테스트 트랙 모두 null)");
            yield break;
        }

        // 2) TrackData에서 리소스 꺼내기 (필드명 달라도 대응)
        AudioClip audioClip = TryGetFieldOrProperty<AudioClip>(track, "audioClip", "AudioClip", "musicClip", "songClip");
        VideoClip videoClip = TryGetFieldOrProperty<VideoClip>(track, "gameVideo", "GameVideo", "videoClip", "VideoClip");
        TextAsset beatMapJson = TryGetFieldOrProperty<TextAsset>(track, "beatMap", "BeatMap", "beatMapJson", "BeatMapJson", "beatMapText");

        if (audioClip == null)
        {
            Debug.LogError("[MainGameAutoStart] TrackData에서 AudioClip을 찾지 못했습니다. (audioClip 필드 확인)");
            yield break;
        }

        // 3) BeatMap 주입 (스폰 시스템은 스폰 담당이 알아서 시작 이벤트를 받아서 시작)
        if (beatMapSpawner != null && beatMapJson != null)
        {
            beatMapSpawner.SetBeatMap(beatMapJson);
            if (verboseLog) Debug.Log($"[MainGameAutoStart] BeatMap injected: {beatMapJson.name}");
        }
        else if (beatMapSpawner == null)
        {
            Debug.LogWarning("[MainGameAutoStart] BeatMapSpawner를 찾지 못했습니다.");
        }
        else if (beatMapJson == null)
        {
            Debug.LogWarning("[MainGameAutoStart] BeatMap JSON이 null 입니다. (노트 스폰이 안 될 수 있음)");
        }

        // 4) Ready 카운트다운
        yield return CoReadyCountdownRealtime(readySeconds);

        // 5) 비디오 준비/재생은 VideoScreenManager “한 군데서만”
        //    (여기서는 clip 전달만)
        if (videoScreenManager != null && videoClip != null)
        {
            videoScreenManager.Play(videoClip);
            if (verboseLog) Debug.Log($"[MainGameAutoStart] VideoScreenManager.Play({videoClip.name})");
        }
        else if (videoClip == null)
        {
            if (verboseLog) Debug.LogWarning("[MainGameAutoStart] videoClip이 null이라 영상은 스킵합니다.");
        }
        else
        {
            Debug.LogWarning("[MainGameAutoStart] VideoScreenManager를 찾지 못했습니다.");
        }

        // 6) 음악 DSP 스케줄 시작
        double dspNow = AudioSettings.dspTime;
        double dspStart = dspNow + Mathf.Max(0.05f, scheduleLeadTime);

        songAudioSource.clip = audioClip;
        songAudioSource.volume = songVolume;
        songAudioSource.PlayScheduled(dspStart);

        SongStartDspTime = dspStart;

        // 7) dspStart 시점에 “시작 이벤트” 발사(한 번만)
        yield return new WaitUntil(() => AudioSettings.dspTime >= dspStart - 0.001);

        SongStarted = true;
        OnSongStart?.Invoke();

        if (verboseLog) Debug.Log($"[MainGameAutoStart] OnSongStart fired. dspStart={dspStart:F4}");

        // 8) 종료 감시(ResultUI 등이 OnSongEnd로 반응하도록)
        if (fireSongEndEvent)
        {
            if (_endWatcher != null) StopCoroutine(_endWatcher);
            _endWatcher = StartCoroutine(CoWatchSongEnd());
        }
    }

    IEnumerator CoWatchSongEnd()
    {
        // songAudioSource.timeSamples는 PlayScheduled 직후엔 0으로 머물 수 있음
        // isPlaying + clip.length 기준으로 안정적으로 종료를 감지
        var clip = (songAudioSource != null) ? songAudioSource.clip : null;
        if (clip == null) yield break;

        // 실제로 재생이 시작될 때까지 대기 (최대 몇 초)
        float safety = 0f;
        while (songAudioSource != null && !songAudioSource.isPlaying && safety < 10f)
        {
            safety += Time.unscaledDeltaTime;
            yield return null;
        }

        // 재생 종료 대기
        while (songAudioSource != null && songAudioSource.isPlaying)
            yield return null;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, endGraceSeconds));

        SongEnded = true;
        OnSongEnd?.Invoke();

        if (verboseLog) Debug.Log("[MainGameAutoStart] OnSongEnd fired.");
    }

    IEnumerator CoReadyCountdownRealtime(float seconds)
    {
        if (readyUI != null) readyUI.SetActive(true);

        float t = Mathf.Max(0f, seconds);
        while (t > 0f)
        {
            if (readyCountText != null)
                readyCountText.text = Mathf.CeilToInt(t).ToString();

            yield return new WaitForSecondsRealtime(1f);
            t -= 1f;
        }

        if (readyCountText != null) readyCountText.text = "GO";
        yield return new WaitForSecondsRealtime(0.2f);

        if (readyUI != null) readyUI.SetActive(false);
    }

    TrackData TryGetSelectedTrack()
    {
        // GameManager가 있을 때만 사용 (없으면 null)
        try
        {
            var gmType = Type.GetType("GameManager");
            if (gmType == null) return null;

            var instProp = gmType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var inst = instProp?.GetValue(null, null);
            if (inst == null) return null;

            var getSel = gmType.GetMethod("GetSelectedTrack", BindingFlags.Public | BindingFlags.Instance);
            if (getSel == null) return null;

            return getSel.Invoke(inst, null) as TrackData;
        }
        catch { return null; }
    }

    static T TryGetFieldOrProperty<T>(object obj, params string[] names) where T : class
    {
        if (obj == null) return null;

        var t = obj.GetType();
        foreach (var n in names)
        {
            var f = t.GetField(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
            {
                var v = f.GetValue(obj) as T;
                if (v != null) return v;
            }

            var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null)
            {
                var v = p.GetValue(obj, null) as T;
                if (v != null) return v;
            }
        }
        return null;
    }
}
