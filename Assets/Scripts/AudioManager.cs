using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM 설정 (메인메뉴/공통)")]
    [SerializeField] private AudioClip menuBgm;
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private bool playMenuBgmOnStart = true;

    [Header("효과음(SFX) 설정")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource sfxSource;

    [Header("VR 3D 오디오 (선택 사항)")]
    [SerializeField] private bool useVRSpatialAudio = false;

    private float currentBGMVolume = 0.2f;
    private float currentSFXVolume = 0.5f;

    private const string KEY_BGM = "BGMVolume";
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

        // AudioSource 자동 생성
        if (bgmSource == null) bgmSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        // BGM 세팅
        bgmSource.playOnAwake = false;
        bgmSource.loop = true;
        bgmSource.spatialBlend = 0f;

        // SFX 세팅
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;

        // 볼륨 로드 (GameManager > PlayerPrefs)
        ReloadVolumesFromManagerOrPrefs();

        if (useVRSpatialAudio)
            Setup3DAudio();

        // 메뉴에서 자동 재생 옵션
        if (playMenuBgmOnStart && menuBgm != null)
            PlayMenuBGM();
    }

    // -------------------------
    // BGM
    // -------------------------
    public void PlayMenuBGM()
    {
        if (bgmSource == null) return;

        ReloadVolumesFromManagerOrPrefs();

        if (menuBgm == null)
        {
            Debug.LogWarning("⚠ menuBgm이 비어있습니다. Inspector에서 연결하세요.");
            return;
        }

        if (bgmSource.clip != menuBgm)
            bgmSource.clip = menuBgm;

        bgmSource.volume = currentBGMVolume;

        if (!bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.Stop();
    }

    public void SetBGMVolume(float volume)
    {
        currentBGMVolume = Mathf.Clamp01(volume);

        if (bgmSource != null)
            bgmSource.volume = currentBGMVolume;

        PlayerPrefs.SetFloat(KEY_BGM, currentBGMVolume);
        PlayerPrefs.Save();
    }

    // 필요하면 메뉴 프리뷰 재생 중 배경음 낮추기 용도
    public void SetBGMVolumeMultiplier(float mul)
    {
        mul = Mathf.Clamp01(mul);
        if (bgmSource != null)
            bgmSource.volume = currentBGMVolume * mul;
    }

    // -------------------------
    // SFX
    // -------------------------
    public void PlayClickSound()
    {
        if (sfxSource == null)
        {
            Debug.LogError("❌ sfxSource가 null입니다!");
            return;
        }

        if (clickSound == null)
        {
            Debug.LogError("❌ clickSound가 null입니다! Inspector에서 연결하세요!");
            return;
        }

        ReloadVolumesFromManagerOrPrefs();

        sfxSource.volume = currentSFXVolume;
        sfxSource.PlayOneShot(clickSound, currentSFXVolume);
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null) return;

        ReloadVolumesFromManagerOrPrefs();

        sfxSource.volume = currentSFXVolume;
        sfxSource.PlayOneShot(clip, currentSFXVolume);
    }

    public void SetSFXVolume(float volume)
    {
        currentSFXVolume = Mathf.Clamp01(volume);

        if (sfxSource != null)
            sfxSource.volume = currentSFXVolume;

        PlayerPrefs.SetFloat(KEY_SFX, currentSFXVolume);
        PlayerPrefs.Save();
    }

    // -------------------------
    // Volume source
    // -------------------------
    public void ReloadVolumesFromManagerOrPrefs()
    {
        // GameManager 우선
        if (GameManager.Instance != null)
        {
            currentBGMVolume = Mathf.Clamp01(GameManager.Instance.GetBGMVolume());
            currentSFXVolume = Mathf.Clamp01(GameManager.Instance.GetSFXVolume());
        }
        else
        {
            currentBGMVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_BGM, currentBGMVolume));
            currentSFXVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX, currentSFXVolume));
        }

        if (bgmSource != null) bgmSource.volume = currentBGMVolume;
        if (sfxSource != null) sfxSource.volume = currentSFXVolume;
    }

    // -------------------------
    // VR 3D Audio (optional)
    // -------------------------
    private void Setup3DAudio()
    {
        if (sfxSource != null)
        {
            sfxSource.spatialBlend = 1f;
            sfxSource.rolloffMode = AudioRolloffMode.Linear;
            sfxSource.minDistance = 1f;
            sfxSource.maxDistance = 10f;
        }
    }

    public void SetVRSpatialAudio(bool enable)
    {
        useVRSpatialAudio = enable;

        if (sfxSource != null)
        {
            if (enable) Setup3DAudio();
            else sfxSource.spatialBlend = 0f;
        }
    }
}
