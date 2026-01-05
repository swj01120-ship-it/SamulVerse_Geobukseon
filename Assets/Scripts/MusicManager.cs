using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Music Settings")]
    public AudioClip backgroundMusic;
    public float bpm = 120f;

    [Header("Audio Source")]
    public AudioSource audioSource;

    [Header("Timing")]
    public float beatInterval;
    public float songPosition;
    public int currentBeat;

    [Header("Song State")]
    public bool songFinished = false;
    private bool resultsShown = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Start()
    {
        beatInterval = 60f / bpm;

        audioSource.clip = backgroundMusic;
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.6f;

        PlayMusic();
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            songPosition = audioSource.time;
            currentBeat = (int)(songPosition / beatInterval);
        }
        else if (!songFinished && songPosition > 0)
        {
            // 음악이 끝났음!
            OnSongFinished();
        }
    }

    public void PlayMusic()
    {
        audioSource.Play();
        songFinished = false;
        resultsShown = false;
        Debug.Log($"★ Music started! Duration: {backgroundMusic.length:F1}s, BPM: {bpm}");
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    public void PauseMusic()
    {
        audioSource.Pause();
    }

    void OnSongFinished()
    {
        songFinished = true;

        Debug.Log("★★★ SONG FINISHED! ★★★");
        Debug.Log($"Song Position: {songPosition:F2}s");
        Debug.Log($"Clip Length: {backgroundMusic.length:F2}s");

        // 결과 표시 (한 번만)
        if (!resultsShown)
        {
            resultsShown = true;
            ShowResults();
        }
    }

    void ShowResults()
    {
        Debug.Log("Showing results...");

        if (RhythmGameManager.Instance != null)
        {
            RhythmGameManager.Instance.ShowResults();
        }
        else
        {
            Debug.LogError("RhythmGameManager.Instance is null!");
        }
    }

    public float GetTimeToNextBeat()
    {
        float timeSinceLastBeat = songPosition % beatInterval;
        return beatInterval - timeSinceLastBeat;
    }

    public bool IsOnBeat(float tolerance = 0.1f)
    {
        float timeSinceLastBeat = songPosition % beatInterval;
        return timeSinceLastBeat < tolerance || timeSinceLastBeat > (beatInterval - tolerance);
    }
}
