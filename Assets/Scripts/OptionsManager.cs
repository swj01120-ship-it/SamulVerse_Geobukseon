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

    void Start()
    {
        StartCoroutine(Init());
    }

    IEnumerator Init()
    {
        yield return null;

        float music = GameManager.Instance ? GameManager.Instance.GetMusicVolume() : defaultMusicVolume;
        float bgm = GameManager.Instance ? GameManager.Instance.GetBGMVolume() : defaultBGMVolume;
        float sfx = GameManager.Instance ? GameManager.Instance.GetSFXVolume() : defaultSFXVolume;

        SetupSlider(musicSlider, music, OnMusicChanged);
        SetupSlider(bgmSlider, bgm, OnBGMChanged);   // ✅ 함수명 통일
        SetupSlider(sfxSlider, sfx, OnSFXChanged);

        // 초기 적용
        OnMusicChanged(music);
        OnBGMChanged(bgm);
        OnSFXChanged(sfx);

        Debug.Log($"✅ Options 초기화 완료 (Music:{music:F2}, BGM:{bgm:F2}, SFX:{sfx:F2})");
    }

    void SetupSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> cb)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;

        slider.onValueChanged.RemoveListener(cb);
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
        slider.onValueChanged.AddListener(cb);
    }

    // ✅ MUSIC: 미리보기 + 메인 게임 음악
    void OnMusicChanged(float v)
    {
        v = Mathf.Clamp01(v);

        if (GameManager.Instance != null)
            GameManager.Instance.SetMusicVolume(v);

        // 1) 현재 재생중인 미리보기(있을 때만)
        if (TrackSelectorManager.Instance != null)
            TrackSelectorManager.Instance.ApplyVolumeToCurrentPreview(v);

        // 2) 메인 게임 음악(MusicManager)
        if (MusicManager.Instance != null)
            MusicManager.Instance.SetVolume(v);

        Debug.Log($"🎵 MUSIC 볼륨 적용: {v:F2}");
    }

    // ✅ BGM: 메뉴 배경음만 (중요: Music 건드리면 안 됨)
    void OnBGMChanged(float v)
    {
        v = Mathf.Clamp01(v);

        // ✅ GameManager에도 저장 (씬 넘어가도 유지)
        if (GameManager.Instance != null)
            GameManager.Instance.SetBGMVolume(v);
        else
        {
            // GameManager 없을 때도 최소 저장은 되게
            PlayerPrefs.SetFloat("BGMVolume", v);
            PlayerPrefs.Save();
        }

        // ✅ 실제 BGM AudioSource 볼륨 적용
        if (BGMManager.Instance != null)
            BGMManager.Instance.SetBGMVolume(v);

        Debug.Log($"🎶 BGM 볼륨 적용: {v:F2}");
    }

    // ✅ SFX: 클릭/효과음만
    void OnSFXChanged(float v)
    {
        v = Mathf.Clamp01(v);

        if (GameManager.Instance != null)
            GameManager.Instance.SetSFXVolume(v);

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetSFXVolume(v);

        Debug.Log($"🔊 SFX 볼륨 적용: {v:F2}");
    }

    private void OnDestroy()
    {
        if (musicSlider != null) musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (bgmSlider != null) bgmSlider.onValueChanged.RemoveListener(OnBGMChanged);
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSFXChanged);
    }
}
