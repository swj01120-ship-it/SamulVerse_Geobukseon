using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Music Settings")]
    public AudioClip backgroundMusic;
    public float bpm = 120f;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Timing (Read Only)")]
    public float beatInterval;
    public float songPosition;
    public int currentBeat;

    [Header("Song State (Read Only)")]
    public bool songFinished = false;

    [Header("Options")]
    [Tooltip("체크하면 씬 시작 시 자동 재생(권장: OFF, GameSceneController에서 Start 버튼으로 재생)")]
    public bool autoPlayOnStart = false;

    private bool resultsShown = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // DontDestroyOnLoad는 선택사항.
        // 씬마다 MusicManager가 하나씩 있다면 꺼두는 게 안전.
        // DontDestroyOnLoad(gameObject);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        RecalcBeatInterval();

        // ✅ 핵심: 자동재생 끔 (GameSceneController가 Start 버튼에서 PlayMusic 호출)
        if (autoPlayOnStart && backgroundMusic != null)
        {
            SetMusic(backgroundMusic, bpm);
            PlayMusic();
        }
    }

    void Update()
    {
        if (audioSource == null) return;

        if (audioSource.isPlaying)
        {
            songPosition = audioSource.time;
            currentBeat = (beatInterval > 0f) ? (int)(songPosition / beatInterval) : 0;
        }
        else if (!songFinished && songPosition > 0f)
        {
            OnSongFinished();
        }
    }

    void RecalcBeatInterval()
    {
        beatInterval = (bpm > 0f) ? (60f / bpm) : 0.5f;
    }

    // ✅ GameSceneController에서 호출할 세팅 함수
    public void SetMusic(AudioClip clip, float newBpm)
    {
        backgroundMusic = clip;
        bpm = Mathf.Max(1f, newBpm);
        RecalcBeatInterval();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = backgroundMusic;
            songPosition = 0f;
            currentBeat = 0;
            songFinished = false;
            resultsShown = false;
        }
    }

    public void SetVolume(float v)
    {
        if (audioSource == null) return;
        audioSource.volume = Mathf.Clamp01(v);
    }

    public void PlayMusic()
    {
        if (audioSource == null)
        {
            Debug.LogError("MusicManager: audioSource is null");
            return;
        }

        if (audioSource.clip == null)
        {
            Debug.LogError("MusicManager: audioSource.clip is null (SetMusic 먼저 호출 필요)");
            return;
        }

        audioSource.Play();
        songFinished = false;
        resultsShown = false;

        Debug.Log($"🎵 Music started! Duration: {audioSource.clip.length:F1}s, BPM: {bpm}");
    }

    public void StopMusic()
    {
        if (audioSource == null) return;
        audioSource.Stop();
        songPosition = 0f;
        currentBeat = 0;
        songFinished = false;
        resultsShown = false;
    }

    public void PauseMusic()
    {
        if (audioSource == null) return;
        audioSource.Pause();
    }

    void OnSongFinished()
    {
        songFinished = true;

        Debug.Log("✅ SONG FINISHED!");
        if (!resultsShown)
        {
            resultsShown = true;
            ShowResults();
        }
    }

    void ShowResults()
    {
        if (RhythmGameManager.Instance != null)
            RhythmGameManager.Instance.ShowResults();
        else
            Debug.LogError("RhythmGameManager.Instance is null!");
    }

    public float GetTimeToNextBeat()
    {
        float bi = Mathf.Max(0.0001f, beatInterval);
        float timeSinceLastBeat = songPosition % bi;
        return bi - timeSinceLastBeat;
    }

    public bool IsOnBeat(float tolerance = 0.1f)
    {
        float bi = Mathf.Max(0.0001f, beatInterval);
        float timeSinceLastBeat = songPosition % bi;
        return timeSinceLastBeat < tolerance || timeSinceLastBeat > (bi - tolerance);
    }
}

