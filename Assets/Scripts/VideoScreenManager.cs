using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public class VideoScreenManager : MonoBehaviour
{
    [Header("References")]
    public VideoPlayer videoPlayer;
    public RawImage videoScreen;

    [Header("Playback")]
    public bool loopVideo = false;
    [Range(0f, 1f)] public float screenOpacity = 0.9f;

    [Header("Audio")]
    public bool outputVideoAudio = false; // 보통 OFF (음악은 AudioSource로 재생)
    public ushort directAudioTrack = 0;

    [Header("Prepare Safety")]
    public float prepareTimeout = 10f;

    [Header("RenderTexture")]
    public int width = 1440;
    public int height = 1584;

    [Header("Debug")]
    public bool verboseLog = true;

    RenderTexture _rt;
    bool _preparing;

    void Awake()
    {
        if (videoPlayer == null) videoPlayer = GetComponent<VideoPlayer>();

        ApplyOpacity();

        if (videoPlayer == null)
        {
            Debug.LogError("[VideoScreenManager] VideoPlayer가 없습니다.");
            return;
        }

        // 이벤트 등록
        videoPlayer.prepareCompleted -= OnPrepared;
        videoPlayer.prepareCompleted += OnPrepared;

        videoPlayer.errorReceived -= OnError;
        videoPlayer.errorReceived += OnError;

        // 기본 세팅
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = loopVideo;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;

        SetupAudioMode();
        EnsureOutputPipe();
    }

    void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnPrepared;
            videoPlayer.errorReceived -= OnError;
        }

        ReleaseRT();
    }

    public void Play(VideoClip clip)
    {
        if (videoPlayer == null) return;
        if (clip == null)
        {
            Log("Play(clip) clip==null → 스킵");
            return;
        }

        // ✅ “자동으로 꺼지는” 이슈 방어: 화면/부모를 강제로 ON
        ForceActive(videoScreen != null ? videoScreen.transform : transform);

        EnsureOutputPipe();
        ApplyOpacity();

        SafeStop();

        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = clip;
        videoPlayer.isLooping = loopVideo;

        _preparing = true;
        videoPlayer.Prepare();

        StopAllCoroutines();
        StartCoroutine(CoPrepareTimeout());

        Log($"Prepare start: {clip.name}");
    }

    public void StopVideo()
    {
        SafeStop();
        Log("StopVideo()");
    }

    void EnsureOutputPipe()
    {
        if (videoPlayer == null) return;

        if (_rt == null)
        {
            _rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            _rt.name = $"RT_{gameObject.name}_Video";
            _rt.Create();
            Log($"RT created: {_rt.name} ({width}x{height})");
        }

        // ✅ RenderTexture 출력 고정
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = _rt;

        if (videoScreen != null)
        {
            videoScreen.texture = _rt;
            videoScreen.enabled = true;
        }
    }

    void SetupAudioMode()
    {
        if (videoPlayer == null) return;

        if (!outputVideoAudio)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            return;
        }

        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        videoPlayer.EnableAudioTrack(directAudioTrack, true);
    }

    void ApplyOpacity()
    {
        if (videoScreen == null) return;
        var c = videoScreen.color;
        c.a = Mathf.Clamp01(screenOpacity);
        videoScreen.color = c;
    }

    void SafeStop()
    {
        _preparing = false;

        try { if (videoPlayer != null && videoPlayer.isPlaying) videoPlayer.Stop(); }
        catch { }

        try { if (videoPlayer != null) videoPlayer.time = 0; }
        catch { }
    }

    IEnumerator CoPrepareTimeout()
    {
        float t = 0f;
        while (_preparing && t < prepareTimeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_preparing)
        {
            _preparing = false;
            Log("Prepare timeout (클립/코덱/기기 상태 확인)");
        }
    }

    void OnPrepared(VideoPlayer vp)
    {
        _preparing = false;

        try
        {
            vp.Play();
            Log("Play()");
        }
        catch
        {
            Log("Play failed");
        }
    }

    void OnError(VideoPlayer vp, string msg)
    {
        _preparing = false;
        Debug.LogError($"[VideoScreenManager] Video Error: {msg}");
    }

    void ReleaseRT()
    {
        if (_rt == null) return;
        if (_rt.IsCreated()) _rt.Release();
        Destroy(_rt);
        _rt = null;
    }

    static void ForceActive(Transform t)
    {
        if (t == null) return;

        // 해당 오브젝트 ON
        if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);

        // 부모도 ON
        var p = t.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
            p = p.parent;
        }
    }

    void Log(string msg)
    {
        if (verboseLog) Debug.Log($"[VideoScreenManager] {msg}");
    }
}
