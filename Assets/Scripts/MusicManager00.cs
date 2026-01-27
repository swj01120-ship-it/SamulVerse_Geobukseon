using UnityEngine;

public class MusicManager00 : MonoBehaviour
{
    public static MusicManager00 Instance;

    private AudioSource audioSource;
    private float currentVolume = 1f;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
    }

    // UI 버튼 씬 & 게임 씬 공통
    public void SetMusicClip(AudioClip clip)
    {
        if (clip == null) return;

        audioSource.clip = clip;
        audioSource.volume = currentVolume;
    }

    public void Play()
    {
        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
    }

    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        audioSource.volume = currentVolume;
    }
}
