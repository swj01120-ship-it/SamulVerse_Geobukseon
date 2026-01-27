// OptionsManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OptionsManager : MonoBehaviour
{
    [Header("음량 슬라이더")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("초기 볼륨 값")]
    [SerializeField] private float defaultMusicVolume = 0.5f;
    [SerializeField] private float defaultBGMVolume = 0.1f;
    [SerializeField] private float defaultSFXVolume = 0.6f;

    private const string KEY_MUSIC = "MusicVolume";
    private const string KEY_BGM = "BGMVolume";
    private const string KEY_SFX = "SFXVolume";

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return null;

        float music = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, defaultMusicVolume));
        float bgm = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_BGM, defaultBGMVolume));
        float sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_SFX, defaultSFXVolume));

        SetupSlider(musicSlider, music, OnMusicChanged);
        SetupSlider(bgmSlider, bgm, OnBGMChanged);
        SetupSlider(sfxSlider, sfx, OnSFXChanged);

        // 초기 적용
        OnMusicChanged(music);
        OnBGMChanged(bgm);
        OnSFXChanged(sfx);
    }

    void SetupSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> cb)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        slider.onValueChanged.RemoveListener(cb);
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(cb);
    }

    // Music = (Home) 프리뷰 + (MainGame) 트랙 음악
    void OnMusicChanged(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_MUSIC, v);
        PlayerPrefs.Save();

        // (MainGame) MusicManager
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetVolume(v);

        // (Home) MusicManager00 (있는 경우)
        if (MusicManager00.Instance != null)
            MusicManager00.Instance.SetVolume(v);

        // ✅ (Home) TrackSelector 프리뷰: 현재/다른 페이지 포함 "전부" 볼륨 통일
        if (TrackSelector.Instance != null)
            TrackSelector.Instance.ApplyPreviewVolume(v);
    }

    // BGM = 메뉴 배경음
    void OnBGMChanged(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_BGM, v);
        PlayerPrefs.Save();

        if (BGMManager.Instance != null)
            BGMManager.Instance.SetBGMVolume(v);
    }

    // SFX
    void OnSFXChanged(float v)
    {
        v = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KEY_SFX, v);
        PlayerPrefs.Save();

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(v);
    }

    private void OnDestroy()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBGMChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
    }
}
