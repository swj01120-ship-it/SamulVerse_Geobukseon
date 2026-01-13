using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("선택된 트랙(메인메뉴 → 게임씬 전달)")]
    [SerializeField] public TrackData selectedTrack;

    // PlayerPrefs Keys (기존 스크립트 호환)
    private const string KEY_MUSIC = "MusicVolume";
    private const string KEY_BGM = "BGMVolume";
    private const string KEY_SFX = "SFXVolume";

    [Header("볼륨(0~1)")]
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.5f; // 미리보기/게임 음악(영상 포함)
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.1f; // 메뉴 배경음
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.6f; // 효과음

    // 선택: 볼륨 바뀌면 구독자에게 알림 (원하면 TrackSelector/OptionsManager에서 사용)
    public System.Action OnVolumeChanged;

    private void Awake()
    {
        // 싱글톤
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolumes();
    }

    // ----------------------------
    // Track 선택 저장 (메뉴 → 게임)
    // ----------------------------
    public void SelectTrack(TrackData track)
    {
        if (track == null)
        {
            Debug.LogError("❌ GameManager.SelectTrack: track이 null입니다.");
            return;
        }
        selectedTrack = track;
        Debug.Log($"✅ Track Selected: {track.trackName}");
    }

    public TrackData GetSelectedTrack() => selectedTrack;

    public float GetMusicVolume() => musicVolume;
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;

    public void SetMusicVolume(float v) { musicVolume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(KEY_MUSIC, musicVolume); }
    public void SetBGMVolume(float v) { bgmVolume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(KEY_BGM, bgmVolume); }
    public void SetSFXVolume(float v) { sfxVolume = Mathf.Clamp01(v); PlayerPrefs.SetFloat(KEY_SFX, sfxVolume); }

    private void LoadVolumes()
    {
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, musicVolume));
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_BGM, bgmVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX, sfxVolume));
    }

    private void SaveVolume(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
        OnVolumeChanged?.Invoke();
    }


}
