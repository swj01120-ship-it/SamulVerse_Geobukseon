using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("선택된 트랙")]
    public TrackData selectedTrack;

    private const string KEY_MUSIC = "MusicVolume";
    private const string KEY_BGM = "BGMVolume";
    private const string KEY_SFX = "SFXVolume";

    [Header("볼륨(0~1)")]
    [Range(0f, 1f)] public float musicVolume = 0.5f; // 미리보기 + 메인 게임 음악
    [Range(0f, 1f)] public float bgmVolume = 0.1f; // 메뉴 배경음
    [Range(0f, 1f)] public float sfxVolume = 0.6f; // 효과음

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 저장값 로드
        musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, musicVolume));
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_BGM, bgmVolume));
        sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX, sfxVolume));
    }

    public void SelectTrack(TrackData track)
    {
        if (track == null)
        {
            Debug.LogError("❌ SelectTrack: track이 null입니다!");
            return;
        }

        selectedTrack = track;
        Debug.Log($"✅ 트랙 저장됨: {track.trackName} / BPM {track.bpm}");
    }

    public float GetMusicVolume() => musicVolume;
    public float GetBGMVolume() => bgmVolume;
    public float GetSFXVolume() => sfxVolume;

    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_MUSIC, musicVolume);
        PlayerPrefs.Save();
    }

    public void SetBGMVolume(float v)
    {
        bgmVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_BGM, bgmVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_SFX, sfxVolume);
        PlayerPrefs.Save();
    }
}
