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
    [SerializeField] private string[] bgmScenes = { "Opening" }; // ✅ UI_Buttons 대신 Opening

    private const string KEY_BGM = "BGMVolume";
    [SerializeField] private float defaultBGMVolume = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSource 보장
        if (bgmSource == null)
        {
            bgmSource = GetComponent<AudioSource>();
            if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        }

        bgmSource.playOnAwake = false;
        bgmSource.loop = true;

        if (bgmClip != null)
            bgmSource.clip = bgmClip;

        // 저장된 볼륨 적용 (0이면 완전 무음)
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_BGM, defaultBGMVolume));
        bgmSource.volume = savedVolume;

        // 씬 이벤트
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 현재 씬 즉시 반영
        CheckAndApply(SceneManager.GetActiveScene().name);

        Debug.Log($"✓ BGMManager 초기화 완료 (씬: {SceneManager.GetActiveScene().name}, 볼륨: {bgmSource.volume:F2})");
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckAndApply(scene.name);
    }

    private void CheckAndApply(string sceneName)
    {
        bool shouldPlay = ShouldPlayInScene(sceneName);

        if (shouldPlay)
        {
            if (bgmSource.clip == null && bgmClip != null)
                bgmSource.clip = bgmClip;

            if (bgmSource.clip != null && !bgmSource.isPlaying)
            {
                bgmSource.Play();
                Debug.Log($"🎵 BGM 재생 (씬: {sceneName})");
            }
        }
        else
        {
            if (bgmSource.isPlaying)
            {
                bgmSource.Stop();
                Debug.Log($"🔇 BGM 정지 (씬: {sceneName})");
            }
        }
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

    /// <summary>
    /// BGM 슬라이더가 직접 호출: 0이면 완전 무음이어야 함
    /// </summary>
    public void SetBGMVolume(float value)
    {
        value = Mathf.Clamp01(value);

        PlayerPrefs.SetFloat(KEY_BGM, value);
        PlayerPrefs.Save();

        if (bgmSource != null)
        {
            bgmSource.volume = value;
            Debug.Log($"🎚 BGM 볼륨 적용: {value:F2}");
        }
    }

    /// <summary>
    /// Music(미리보기/게임음악) 시작 순간 강제 0 만들 때 사용
    /// </summary>
    public void ForceMute()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = 0f;
            Debug.Log("🔇 BGM 강제 Mute(0)");
        }
    }

    public void PlayBGM()
    {
        if (bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }
}
