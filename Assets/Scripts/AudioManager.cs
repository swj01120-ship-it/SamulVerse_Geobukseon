using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("효과음(SFX) 설정")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource sfxSource;

    [Header("VR 3D 오디오 (선택 사항)")]
    [SerializeField] private bool useVRSpatialAudio = false;

    private float currentSFXVolume = 1f;

    private void Awake()
    {
        Debug.Log("━━━ AudioManager Awake 호출 ━━━");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSource 자동 생성
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f;

            // ★ PlayerPrefs에서 SFX 볼륨 불러오기
            currentSFXVolume = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
            sfxSource.volume = currentSFXVolume;

            if (useVRSpatialAudio)
            {
                Setup3DAudio();
            }

            Debug.Log($"✓ AudioManager 초기화 완료");
            Debug.Log($"  - clickSound: {(clickSound != null ? clickSound.name : "NULL!")}");
            Debug.Log($"  - SFX 볼륨: {currentSFXVolume:F2}");
        }
        else
        {
            Debug.LogWarning($"⚠ AudioManager 중복! 파괴: {gameObject.name}");
            Destroy(gameObject);
        }
    }

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

        // ★ 매번 최신 볼륨 적용
        sfxSource.volume = currentSFXVolume;
        sfxSource.PlayOneShot(clickSound, currentSFXVolume); // ★ 볼륨 명시적 전달
        Debug.Log($"🔊 클릭음 재생 (볼륨: {currentSFXVolume:F2})");
    }

    public void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.volume = currentSFXVolume;
            sfxSource.PlayOneShot(clip, currentSFXVolume);
        }
    }

    public void SetSFXVolume(float volume)
    {
        currentSFXVolume = Mathf.Clamp01(volume);

        if (sfxSource != null)
        {
            sfxSource.volume = currentSFXVolume;
        }

        PlayerPrefs.SetFloat("SFXVolume", currentSFXVolume);
        PlayerPrefs.Save();

        Debug.Log($"🎚️ SFX 볼륨 설정: {currentSFXVolume:F2}");
    }

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
            if (enable)
            {
                Setup3DAudio();
            }
            else
            {
                sfxSource.spatialBlend = 0f;
            }
        }
    }
}