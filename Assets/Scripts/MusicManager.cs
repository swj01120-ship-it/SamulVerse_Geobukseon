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

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        RecalcBeatInterval();

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
        else if (!songFinished && songPosition > 0f && audioSource.clip != null)
        {
            // ⭐ 음악이 끝까지 재생되었는지 확인
            if (songPosition >= audioSource.clip.length - 0.5f)
            {
                Debug.Log($"[MusicManager] Song ended: {songPosition:F2}s / {audioSource.clip.length:F2}s");
                OnSongFinished();
            }
        }
    }

    void RecalcBeatInterval()
    {
        beatInterval = (bpm > 0f) ? (60f / bpm) : 0.5f;
    }

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

        Debug.Log("✅ [MusicManager] SONG FINISHED!");

        if (!resultsShown)
        {
            resultsShown = true;
            ShowResults();
        }
    }

    void ShowResults()
    {
        Debug.Log("[MusicManager] Calling RhythmGameManager.ShowResults()");

        if (RhythmGameManager.Instance != null)
        {
            RhythmGameManager.Instance.ShowResults();
        }
        else
        {
            Debug.LogError("[MusicManager] RhythmGameManager.Instance is null!");
        }
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