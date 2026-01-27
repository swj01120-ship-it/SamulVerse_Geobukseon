using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("SFX Source")]
    [SerializeField] private AudioSource sfxSource;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip clickSound;

    [Header("Default Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float defaultSfxVolume = 0.6f;

    private const string KEY_SFX = "SFXVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
            if (sfxSource == null)
                sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D

        float v = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX, defaultSfxVolume));
        SetSFXVolume(v);
    }

    public void PlayClickSound()
    {
        if (sfxSource == null) return;
        if (clickSound == null) return;

        sfxSource.PlayOneShot(clickSound, 1f);
    }

    public void SetSFXVolume(float v)
    {
        v = Mathf.Clamp01(v);

        if (sfxSource != null)
            sfxSource.volume = v;

        PlayerPrefs.SetFloat(KEY_SFX, v);
        PlayerPrefs.Save();
    }

    public void PlayButtonClickSound() => PlayClickSound();
}
