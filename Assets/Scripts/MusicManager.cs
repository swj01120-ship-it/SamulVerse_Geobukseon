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
    [Tooltip("체크하면 씬 시작 시 자동 재생 (MainGame에서는 ON 권장)")]
    public bool autoPlayOnStart = false;

    [Tooltip("autoPlayOnStart일 때 TrackSession의 SelectedTrack을 우선 재생")]
    public bool preferTrackSession = true;

    

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.loop = false;
        audioSource.playOnAwake = false;

        // ✅ “안 들림” 방지 기본값
        audioSource.mute = false;
        audioSource.volume = 1f;
        audioSource.spatialBlend = 0f; // 2D (거리감으로 안 들리는 문제 방지)
    }

    void Start()
    {
        RecalcBeatInterval();

        if (autoPlayOnStart)
        {
            TryAutoPlay();
        }
    }

    void TryAutoPlay()
    {
        // 1) TrackSession 우선
        if (preferTrackSession)
        {
            var session = TrackSession.Instance != null ? TrackSession.Instance : TrackSession.Ensure();
            var track = session.SelectedTrack;

            if (track != null)
            {
                if (track.audioClip != null)
                {
                    SetMusic(track.audioClip, track.bpm);
                    PlayMusic();
                    Debug.Log($"🎵 AutoPlay TrackSession: {track.trackName}");
                    return;
                }
                Debug.LogError("MusicManager: TrackSession.SelectedTrack.audioClip 이 비어있음");
            }
            else
            {
                Debug.LogError("MusicManager: TrackSession.SelectedTrack 이 null (Home에서 SetTrack 되었는지 확인)");
            }
        }

        // 2) fallback: backgroundMusic
        if (backgroundMusic != null)
        {
            SetMusic(backgroundMusic, bpm);
            PlayMusic();
            Debug.Log("🎵 AutoPlay backgroundMusic");
            return;
        }

        // 3) 마지막: 이미 audioSource.clip이 있으면 그냥 재생
        if (audioSource != null && audioSource.clip != null)
        {
            PlayMusic();
            Debug.Log("🎵 AutoPlay existing audioSource.clip");
            return;
        }

        Debug.LogError("MusicManager: 재생할 AudioClip이 없습니다 (TrackSession/BackgroundMusic/AudioSource.clip 모두 없음)");
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

    public void SetMusic(AudioClip clip, float newBpm)
    {
        backgroundMusic = clip;
        bpm = Mathf.Max(1f, newBpm);
        RecalcBeatInterval();

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = backgroundMusic;
            audioSource.time = 0f;

            songPosition = 0f;
            currentBeat = 0;
            songFinished = false;
            
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
        

        Debug.Log($"🎵 Music started! Duration: {audioSource.clip.length:F1}s, BPM: {bpm}");
    }

    public void StopMusic()
    {
        if (audioSource == null) return;
        audioSource.Stop();
        songPosition = 0f;
        currentBeat = 0;
        songFinished = false;
        
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
        
        // 결과 UI 표시(ShowResults)는 RhythmGameManager가 담당하도록 통일
        // (중복 호출/타이밍 꼬임 방지)
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
