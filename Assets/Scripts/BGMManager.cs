using UnityEngine;
using UnityEngine.SceneManagement;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioClip bgmClip;

    [Header("BGM 재생 씬 설정")]
    [Tooltip("이 배열에 들어있는 씬에서만 BGM이 재생됩니다.")]
    [SerializeField] private string[] bgmScenes = { "Home" };

    private const string KEY_BGM = "BGMVolume";
    [SerializeField] private float defaultBGMVolume = 0.1f;

    [Header("Preview Mute Behavior")]
    [Tooltip("true면 프리뷰 중 BGM을 mute만 하지 않고 Pause로 멈춥니다(권장: 겹침/꼬임 방지).")]
    [SerializeField] private bool pauseBgmDuringPreview = true;

    // Preview mute reference counter
    private int _previewMuteCount = 0;

    // 프리뷰 들어가기 직전 상태 기억 (복구 용)
    private bool _wasPlayingBeforePreview = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        }

        // 기본 세팅
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f; // 2D 고정
        bgmSource.mute = false;

        // 클립 지정
        if (bgmSource.clip == null && bgmClip != null)
            bgmSource.clip = bgmClip;

        // 볼륨
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_BGM, defaultBGMVolume));
        bgmSource.volume = savedVolume;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 현재 씬 적용
        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyForScene(scene.name);
    }

    // =========================================================
    // Core Apply
    // =========================================================
    private void ApplyForScene(string sceneName)
    {
        if (bgmSource == null) return;

        bool shouldPlay = ShouldPlayInScene(sceneName);

        // 1) 이 씬에서 BGM 재생 대상이 아니면 완전히 정지
        if (!shouldPlay)
        {
            // 프리뷰 카운터가 남아있어도, 씬이 아니면 BGM 자체를 끄는 게 맞음
            if (bgmSource.isPlaying) bgmSource.Stop();
            bgmSource.mute = false;   // 다음에 Home 들어왔을 때 mute 꼬임 방지
            return;
        }

        // 2) 이 씬에서 재생 대상이면 clip 보장
        if (bgmSource.clip == null && bgmClip != null)
            bgmSource.clip = bgmClip;

        if (bgmSource.clip == null)
            return; // 재생할 게 없음

        // 3) 프리뷰 중이면: 재생 상태는 유지하되 (Pause or mute) 처리
        if (_previewMuteCount > 0)
        {
            if (pauseBgmDuringPreview)
            {
                // 이미 재생중이었으면 Pause 유지, 아니면 굳이 새로 Play하지 않음
                if (bgmSource.isPlaying)
                    bgmSource.Pause();
            }
            else
            {
                // mute 방식일 때: 필요하면 Play는 하되, 소리는 mute
                if (!bgmSource.isPlaying)
                    bgmSource.Play();

                bgmSource.mute = true;
            }
            return;
        }

        // 4) 프리뷰가 아니면 정상 재생
        bgmSource.mute = false;

        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    private bool ShouldPlayInScene(string sceneName)
    {
        if (bgmScenes == null || bgmScenes.Length == 0) return false;

        for (int i = 0; i < bgmScenes.Length; i++)
        {
            if (!string.IsNullOrEmpty(bgmScenes[i]) && bgmScenes[i] == sceneName)
                return true;
        }
        return false;
    }

    // =========================================================
    // Volume
    // =========================================================
    public void SetBGMVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(KEY_BGM, value);
        PlayerPrefs.Save();

        if (bgmSource != null)
            bgmSource.volume = value;
    }

    // =========================================================
    // Preview Mute API (TrackSelector가 호출)
    // =========================================================
    public void BeginPreviewMute()
    {
        if (bgmSource == null) return;

        if (_previewMuteCount == 0)
        {
            // 프리뷰 시작 직전 상태 저장
            _wasPlayingBeforePreview = bgmSource.isPlaying;
        }

        _previewMuteCount++;

        if (pauseBgmDuringPreview)
        {
            // 재생중이면 Pause
            if (bgmSource.isPlaying)
                bgmSource.Pause();
        }
        else
        {
            // mute 방식
            bgmSource.mute = true;
        }
    }

    public void EndPreviewMute()
    {
        if (bgmSource == null) return;

        _previewMuteCount = Mathf.Max(0, _previewMuteCount - 1);

        // 아직 프리뷰가 남아있으면 유지
        if (_previewMuteCount > 0)
        {
            if (!pauseBgmDuringPreview)
                bgmSource.mute = true;
            return;
        }

        // 프리뷰 완전히 종료: 현재 씬 기준으로 복구 여부 판단
        string sceneName = SceneManager.GetActiveScene().name;
        bool shouldPlay = ShouldPlayInScene(sceneName);

        if (!shouldPlay)
        {
            // Home이 아닌 씬이면 그냥 정리
            bgmSource.mute = false;
            if (bgmSource.isPlaying) bgmSource.Stop();
            return;
        }

        // Home 등 재생 씬이면 복구
        if (pauseBgmDuringPreview)
        {
            // 원래 재생중이었으면 UnPause / 아니면 굳이 재생 시작하지 않음(선택)
            if (_wasPlayingBeforePreview)
            {
                bgmSource.UnPause();
                if (!bgmSource.isPlaying && bgmSource.clip != null)
                    bgmSource.Play();
            }
        }
        else
        {
            bgmSource.mute = false;

            if (_wasPlayingBeforePreview && !bgmSource.isPlaying && bgmSource.clip != null)
                bgmSource.Play();
        }
    }
}
