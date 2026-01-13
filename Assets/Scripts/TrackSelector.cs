using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class TrackSelector : MonoBehaviour
{
    [Header("트랙 데이터")]
    public TrackData[] tracks;

    [Header("미리보기 화면")]
    public RawImage previewRawImage;
    public VideoPlayer videoPlayer;
    public Image previewOverlay;
    public Button previewButton;

    [Header("대기 화면")]
    public Image idleImageDisplay;

    [Header("곡 썸네일 버튼")]
    public Button[] trackButtons;

    [Header("곡 정보 (Legacy Text)")]
    public Text trackNameText;
    public Text artistText;
    public Text trackInfoText;

    [Header("Start")]
    public Button startButton;
    public GameObject windowBottom;

    [Header("옵션 패널")]
    public GameObject optionsPanel;
    public Slider musicSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Button optionsOpenButton;
    public Button optionsCloseButton;

    [Header("옵션 라벨(선택)")]
    public Text musicValueText;
    public Text bgmValueText;
    public Text sfxValueText;

    [Header("옵션")]
    public float videoStartDelay = 0.2f;
    public bool autoPreviewOnSelect = true;
    public bool loopPreview = true;
    public bool applyThumbnailToButtons = true;

    [Header("하이라이트")]
    public Color normalButtonColor = Color.white;
    public Color selectedButtonColor = new Color(1f, 0.92f, 0.55f, 1f);
    public float selectedColorMultiplier = 1f;

    [Header("디버그")]
    public bool verboseLog = false;

    private int currentSelectedIndex = -1;
    private int pendingTrackIndex = -1;

    private bool isPreviewPaused = false;
    private bool isStarting = false;
    private bool isPlayingPreview = false;

    private Vector2 originalWindowBottomPos;
    private bool windowBottomShown = false;

    private void Start()
    {
        CacheWindowBottom();
        SetupVideoPlayer();

        // 버튼 UI 채우기
        RefreshTrackButtonsUI();

        // 버튼 클릭 바인딩
        BindTrackButtons();

        // 기타 버튼 바인딩
        BindPreviewButton();
        BindStartButton();

        // 옵션 패널 연결
        BindOptionsPanel();

        // 초기 UI 상태
        ShowIdleScreen();
        SetPauseOverlay(false);
        UpdateInfoTexts(-1);
        ApplyHighlight(-1);

        // Start 버튼은 선택 전 비활성 추천
        if (startButton != null) startButton.interactable = false;
    }

    // -------------------------
    // Track Buttons UI Fill
    // -------------------------
    private void RefreshTrackButtonsUI()
    {
        if (tracks == null || trackButtons == null) return;

        int n = Mathf.Min(tracks.Length, trackButtons.Length);
        for (int i = 0; i < n; i++)
        {
            var t = tracks[i];
            var btn = trackButtons[i];
            if (t == null || btn == null) continue;

            // 자식 구조 기반: Thumbnail/Title/SubInfo
            var thumb = btn.transform.Find("Thumbnail")?.GetComponent<Image>();
            if (thumb != null)
            {
                thumb.enabled = (t.thumbnail != null);
                thumb.sprite = t.thumbnail;
            }

            var title = btn.transform.Find("Title")?.GetComponent<Text>();
            if (title != null)
                title.text = string.IsNullOrEmpty(t.trackName) ? $"Track {i + 1}" : t.trackName;

            var sub = btn.transform.Find("SubInfo")?.GetComponent<Text>();
            if (sub != null)
                sub.text = $"BPM {t.bpm} / {t.difficulty}";

            // 옵션: 버튼 자체 Image에도 썸네일
            if (applyThumbnailToButtons && t.thumbnail != null)
            {
                var img = btn.GetComponent<Image>();
                if (img != null) img.sprite = t.thumbnail;
            }

            // 기본 컬러 세팅
            SetButtonNormal(btn);
        }
    }

    private void BindTrackButtons()
    {
        if (tracks == null || trackButtons == null) return;

        int n = Mathf.Min(tracks.Length, trackButtons.Length);
        for (int i = 0; i < n; i++)
        {
            int idx = i;
            var btn = trackButtons[idx];
            if (btn == null) continue;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                if (isStarting) return;
                PlayClickSound();
                OnTrackButtonClicked(idx);
            });
        }
    }

    private void OnTrackButtonClicked(int index)
    {
        if (tracks == null || index < 0 || index >= tracks.Length) return;
        if (tracks[index] == null) return;

        // 선택 저장 + 텍스트 갱신
        UpdateInfoTexts(index);

        // 하이라이트 적용
        ApplyHighlight(index);

        // Start 활성화
        if (startButton != null) startButton.interactable = true;

        // GameManager에 선택 저장
        if (GameManager.Instance != null)
            GameManager.Instance.SelectTrack(tracks[index]);

        // 자동 프리뷰 여부
        pendingTrackIndex = index;

        CancelInvoke(nameof(PlayVideoDelayed));
        if (autoPreviewOnSelect)
            Invoke(nameof(PlayVideoDelayed), videoStartDelay);
        else
            StopPreview();

        if (verboseLog) Debug.Log($"[TrackSelector] Selected index: {index}");
    }

    // -------------------------
    // Highlight
    // -------------------------
    private void ApplyHighlight(int selectedIndex)
    {
        if (trackButtons == null) return;

        for (int i = 0; i < trackButtons.Length; i++)
        {
            var btn = trackButtons[i];
            if (btn == null) continue;

            if (i == selectedIndex) SetButtonSelected(btn);
            else SetButtonNormal(btn);
        }
    }

    private void SetButtonNormal(Button btn)
    {
        var colors = btn.colors;
        colors.normalColor = normalButtonColor;
        colors.highlightedColor = normalButtonColor;
        colors.pressedColor = normalButtonColor * 0.9f;
        colors.selectedColor = normalButtonColor;
        colors.colorMultiplier = 1f;
        btn.colors = colors;
    }

    private void SetButtonSelected(Button btn)
    {
        var colors = btn.colors;
        colors.normalColor = selectedButtonColor;
        colors.highlightedColor = selectedButtonColor;
        colors.pressedColor = selectedButtonColor * 0.9f;
        colors.selectedColor = selectedButtonColor;
        colors.colorMultiplier = selectedColorMultiplier;
        btn.colors = colors;
    }

    // -------------------------
    // Preview Button
    // -------------------------
    private void BindPreviewButton()
    {
        if (previewButton == null) return;

        previewButton.onClick.RemoveAllListeners();
        previewButton.onClick.AddListener(OnPreviewClick);
    }

    public void OnPreviewClick()
    {
        if (isStarting) return;

        PlayClickSound();

        if (!isPlayingPreview)
        {
            if (currentSelectedIndex < 0) return;
            pendingTrackIndex = currentSelectedIndex;
            CancelInvoke(nameof(PlayVideoDelayed));
            Invoke(nameof(PlayVideoDelayed), 0f);
            return;
        }

        if (videoPlayer == null) return;

        isPreviewPaused = !isPreviewPaused;
        SetPauseOverlay(isPreviewPaused);

        if (isPreviewPaused) videoPlayer.Pause();
        else
        {
            ApplyPreviewVolume(GetMusicVolume());
            videoPlayer.Play();
        }
    }

    private void PlayVideoDelayed()
    {
        if (pendingTrackIndex < 0) return;

        if (TrackSelectorManager.Instance != null)
            TrackSelectorManager.Instance.OnTrackSelectorStartPlaying(this, videoPlayer);

        ShowPreviewScreen();
        PlayPreviewVideo(pendingTrackIndex);
        ShowWindowBottom();

        isPreviewPaused = false;
        SetPauseOverlay(false);

        isPlayingPreview = true;
        pendingTrackIndex = -1;
    }

    private void PlayPreviewVideo(int index)
    {
        if (videoPlayer == null) return;
        if (tracks == null || index < 0 || index >= tracks.Length) return;

        TrackData t = tracks[index];
        if (t == null || t.previewVideo == null)
        {
            StopPreview();
            return;
        }

        BGMManager.Instance?.ForceMute();

        SetupVideoPlayer();

        if (previewRawImage != null) previewRawImage.texture = null;

        videoPlayer.Stop();
        videoPlayer.clip = t.previewVideo;
        videoPlayer.time = 0;

        ApplyPreviewVolume(GetMusicVolume());

        videoPlayer.Prepare();
        videoPlayer.Play();
    }

    public void StopPreview()
    {
        CancelInvoke(nameof(PlayVideoDelayed));
        pendingTrackIndex = -1;

        if (videoPlayer != null)
        {
            if (videoPlayer.isPlaying) videoPlayer.Stop();
            videoPlayer.clip = null;
        }

        if (previewRawImage != null) previewRawImage.texture = null;

        ShowIdleScreen();
        HideWindowBottom();

        isPlayingPreview = false;
        isPreviewPaused = false;
        SetPauseOverlay(false);

        if (TrackSelectorManager.Instance != null)
            TrackSelectorManager.Instance.OnTrackSelectorStopped(this);
    }

    // -------------------------
    // Start Button
    // -------------------------
    private void BindStartButton()
    {
        if (startButton == null) return;

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() =>
        {
            if (!isStarting) StartCoroutine(StartGameCoroutine());
        });
    }

    private IEnumerator StartGameCoroutine()
    {
        if (currentSelectedIndex < 0) yield break;
        if (isStarting) yield break;

        isStarting = true;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ GameManager.Instance 없음");
            isStarting = false;
            yield break;
        }

        TrackData selected = tracks[currentSelectedIndex];
        if (selected == null)
        {
            Debug.LogError("❌ 선택된 TrackData가 null");
            isStarting = false;
            yield break;
        }

        GameManager.Instance.SelectTrack(selected);

        yield return null;

        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading("MainGame");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
    }

    // -------------------------
    // Options Panel
    // -------------------------
    private void BindOptionsPanel()
    {
        // 패널 초기 상태
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (optionsOpenButton != null)
        {
            optionsOpenButton.onClick.RemoveAllListeners();
            optionsOpenButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                OpenOptions();
            });
        }

        if (optionsCloseButton != null)
        {
            optionsCloseButton.onClick.RemoveAllListeners();
            optionsCloseButton.onClick.AddListener(() =>
            {
                PlayClickSound();
                CloseOptions();
            });
        }

        // 슬라이더 초기값 + 이벤트
        SyncOptionSlidersFromGameManager();

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(v =>
            {
                if (GameManager.Instance != null) GameManager.Instance.SetMusicVolume(v);
                ApplyPreviewVolume(v);
                UpdateOptionValueTexts();
            });
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.onValueChanged.AddListener(v =>
            {
                if (GameManager.Instance != null) GameManager.Instance.SetBGMVolume(v);
                UpdateOptionValueTexts();
            });
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(v =>
            {
                if (GameManager.Instance != null) GameManager.Instance.SetSFXVolume(v);
                UpdateOptionValueTexts();
            });
        }

        UpdateOptionValueTexts();
    }

    private void OpenOptions()
    {
        if (optionsPanel == null) return;
        optionsPanel.SetActive(true);
        SyncOptionSlidersFromGameManager();
        UpdateOptionValueTexts();
    }

    private void CloseOptions()
    {
        if (optionsPanel == null) return;
        optionsPanel.SetActive(false);
    }

    private void SyncOptionSlidersFromGameManager()
    {
        float music = GetMusicVolume();
        float bgm = GetBgmVolume();
        float sfx = GetSfxVolume();

        if (musicSlider != null) musicSlider.value = music;
        if (bgmSlider != null) bgmSlider.value = bgm;
        if (sfxSlider != null) sfxSlider.value = sfx;
    }

    private void UpdateOptionValueTexts()
    {
        if (musicValueText != null && musicSlider != null)
            musicValueText.text = Mathf.RoundToInt(musicSlider.value * 100f).ToString();

        if (bgmValueText != null && bgmSlider != null)
            bgmValueText.text = Mathf.RoundToInt(bgmSlider.value * 100f).ToString();

        if (sfxValueText != null && sfxSlider != null)
            sfxValueText.text = Mathf.RoundToInt(sfxSlider.value * 100f).ToString();
    }

    // -------------------------
    // Video Player Setup / Events
    // -------------------------
    private void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = loopPreview;
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.waitForFirstFrame = true;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        TrySetDirectMute(false);

        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.prepareCompleted += OnVideoPrepared;

        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.errorReceived += OnVideoError;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (previewRawImage != null && vp != null && vp.texture != null)
            previewRawImage.texture = vp.texture;
    }

    private void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogWarning($"[TrackSelector] VideoPlayer error: {msg}");
        StopPreview();
    }

    // -------------------------
    // UI Helpers
    // -------------------------
    private void CacheWindowBottom()
    {
        if (windowBottom == null) return;

        var rt = windowBottom.GetComponent<RectTransform>();
        if (rt != null) originalWindowBottomPos = rt.anchoredPosition;

        windowBottom.SetActive(false);
        windowBottomShown = false;
    }

    private void ShowIdleScreen()
    {
        if (idleImageDisplay != null) idleImageDisplay.gameObject.SetActive(true);
        if (previewRawImage != null) previewRawImage.gameObject.SetActive(false);
    }

    private void ShowPreviewScreen()
    {
        if (idleImageDisplay != null) idleImageDisplay.gameObject.SetActive(false);
        if (previewRawImage != null) previewRawImage.gameObject.SetActive(true);
    }

    private void ShowWindowBottom()
    {
        if (windowBottom == null || windowBottomShown) return;

        windowBottom.SetActive(true);

        var rt = windowBottom.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = originalWindowBottomPos;

        windowBottomShown = true;
    }

    private void HideWindowBottom()
    {
        if (windowBottom == null) return;

        windowBottom.SetActive(false);
        windowBottomShown = false;
    }

    private void SetPauseOverlay(bool paused)
    {
        if (previewOverlay == null) return;
        previewOverlay.color = paused ? new Color(0f, 0f, 0f, 0.3f) : new Color(0f, 0f, 0f, 0f);
    }

    private void UpdateInfoTexts(int index)
    {
        currentSelectedIndex = index;

        if (index < 0 || tracks == null || index >= tracks.Length || tracks[index] == null)
        {
            if (trackNameText != null) trackNameText.text = "";
            if (artistText != null) artistText.text = "";
            if (trackInfoText != null) trackInfoText.text = "";
            return;
        }

        TrackData t = tracks[index];

        if (trackNameText != null) trackNameText.text = t.trackName;
        if (artistText != null) artistText.text = t.artistName;

        if (trackInfoText != null)
            trackInfoText.text = $"BPM {t.bpm}  /  {t.difficulty}";
    }

    // -------------------------
    // Volume / Sound
    // -------------------------
    private float GetMusicVolume()
    {
        if (GameManager.Instance != null) return Mathf.Clamp01(GameManager.Instance.GetMusicVolume());
        return Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 0.5f));
    }

    private float GetBgmVolume()
    {
        if (GameManager.Instance != null) return Mathf.Clamp01(GameManager.Instance.GetBGMVolume());
        return Mathf.Clamp01(PlayerPrefs.GetFloat("BGMVolume", 0.1f));
    }

    private float GetSfxVolume()
    {
        if (GameManager.Instance != null) return Mathf.Clamp01(GameManager.Instance.GetSFXVolume());
        return Mathf.Clamp01(PlayerPrefs.GetFloat("SFXVolume", 0.6f));
    }

    public void ApplyPreviewVolume(float volume)
    {
        if (videoPlayer == null) return;

        volume = Mathf.Clamp01(volume);

        if (videoPlayer.audioOutputMode != VideoAudioOutputMode.Direct)
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        TrySetDirectMute(false);
        TrySetDirectVolume(volume);

        if (verboseLog) Debug.Log($"✅ 미리보기 볼륨 적용: {volume:F2}");
    }

    private void TrySetDirectMute(bool mute)
    {
        if (videoPlayer == null) return;
        try { videoPlayer.SetDirectAudioMute(0, mute); } catch { }
    }

    private void TrySetDirectVolume(float volume)
    {
        if (videoPlayer == null) return;
        try { videoPlayer.SetDirectAudioVolume(0, volume); } catch { }
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClickSound();
    }
}
