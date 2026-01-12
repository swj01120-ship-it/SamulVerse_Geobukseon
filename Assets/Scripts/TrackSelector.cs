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

    [Header("UI 요소")]
    public Text trackNameText;
    public Text artistText;
    public Button startButton;
    public GameObject windowBottom;

    [Header("설정")]
    public float videoStartDelay = 0.2f;

    private int currentSelectedIndex = -1;
    private int pendingTrackIndex = -1;

    private bool isPreviewPaused = false;
    private bool isStarting = false;
    private bool windowBottomShown = false;
    private Vector2 originalWindowBottomPosition;
    private bool isPlayingPreview = false;

    void Start()
    {
        if (windowBottom != null)
        {
            RectTransform rt = windowBottom.GetComponent<RectTransform>();
            if (rt != null) originalWindowBottomPosition = rt.anchoredPosition;
            windowBottom.SetActive(false);
        }

        // 썸네일 버튼 연결
        for (int i = 0; i < trackButtons.Length; i++)
        {
            int idx = i;
            if (trackButtons[i] == null) continue;

            trackButtons[i].onClick.RemoveAllListeners();
            trackButtons[i].onClick.AddListener(() =>
            {
                if (isStarting) return;

                PlayClickSound();
                SelectTrackWithDelay(idx);
            });
        }

        if (previewButton != null)
        {
            previewButton.onClick.RemoveAllListeners();
            previewButton.onClick.AddListener(OnPreviewClick);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                if (!isStarting) StartCoroutine(StartGameCoroutine());
            });
        }

        SetupVideoPlayerDirect();
        ApplyPreviewVolume(GetMusicVolume());
        ShowIdleScreen();
    }

    float GetMusicVolume()
    {
        if (GameManager.Instance != null) return Mathf.Clamp01(GameManager.Instance.GetMusicVolume());
        return Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 0.5f));
    }

    void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClickSound();
    }

    void SetupVideoPlayerDirect()
    {
        if (videoPlayer == null) return;

        videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        try { videoPlayer.SetDirectAudioMute(0, false); } catch { }
    }

    // ✅ OptionsManager / TrackSelectorManager가 호출
    public void ApplyPreviewVolume(float volume)
    {
        if (videoPlayer == null) return;

        volume = Mathf.Clamp01(volume);

        if (videoPlayer.audioOutputMode != VideoAudioOutputMode.Direct)
            videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        try
        {
            videoPlayer.SetDirectAudioMute(0, false);
            videoPlayer.SetDirectAudioVolume(0, volume);
        }
        catch { }

        Debug.Log($"✅ 미리보기 볼륨 적용: {volume:F2}");
    }

    void SelectTrackWithDelay(int index)
    {
        UpdateTrackInfo(index);
        pendingTrackIndex = index;

        CancelInvoke(nameof(PlayVideoDelayed));
        Invoke(nameof(PlayVideoDelayed), videoStartDelay);
    }

    void UpdateTrackInfo(int index)
    {
        if (tracks == null || index < 0 || index >= tracks.Length || tracks[index] == null) return;

        currentSelectedIndex = index;

        if (trackNameText != null) trackNameText.text = tracks[index].trackName;
        if (artistText != null) artistText.text = tracks[index].artistName;
    }

    void PlayVideoDelayed()
    {
        if (pendingTrackIndex < 0) return;

        // 현재 재생중인 셀렉터 등록 (다른 미리보기 정지)
        if (TrackSelectorManager.Instance != null)
            TrackSelectorManager.Instance.OnTrackSelectorStartPlaying(this, videoPlayer);

        ShowPreviewScreen();
        PlayPreviewVideo(pendingTrackIndex);
        ShowWindowBottom();

        isPreviewPaused = false;
        if (previewOverlay != null) previewOverlay.color = Color.clear;

        isPlayingPreview = true;
        pendingTrackIndex = -1;
    }

    void PlayPreviewVideo(int index)
    {
        if (videoPlayer == null) return;
        if (tracks == null || index < 0 || index >= tracks.Length) return;
        if (tracks[index] == null || tracks[index].previewVideo == null) return;

        // ✅ 규칙: 미리보기(=Music) 시작 순간 BGM 무조건 0
        BGMManager.Instance?.ForceMute();

        SetupVideoPlayerDirect();

        videoPlayer.Stop();
        videoPlayer.clip = tracks[index].previewVideo;
        videoPlayer.time = 0;

        ApplyPreviewVolume(GetMusicVolume());
        videoPlayer.Play();
    }

    void ShowIdleScreen()
    {
        if (idleImageDisplay != null) idleImageDisplay.gameObject.SetActive(true);
        if (previewRawImage != null) previewRawImage.gameObject.SetActive(false);

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();
    }

    void ShowPreviewScreen()
    {
        if (idleImageDisplay != null) idleImageDisplay.gameObject.SetActive(false);
        if (previewRawImage != null) previewRawImage.gameObject.SetActive(true);
    }

    void ShowWindowBottom()
    {
        if (windowBottomShown || windowBottom == null) return;

        windowBottom.SetActive(true);

        RectTransform rt = windowBottom.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = originalWindowBottomPosition;

        windowBottomShown = true;
    }

    public void OnPreviewClick()
    {
        if (currentSelectedIndex < 0 || videoPlayer == null) return;

        PlayClickSound();

        isPreviewPaused = !isPreviewPaused;

        if (previewOverlay != null)
            previewOverlay.color = isPreviewPaused ? new Color(0, 0, 0, 0.3f) : Color.clear;

        if (isPreviewPaused) videoPlayer.Pause();
        else
        {
            ApplyPreviewVolume(GetMusicVolume());
            videoPlayer.Play();
        }
    }

    IEnumerator StartGameCoroutine()
    {
        if (currentSelectedIndex < 0) yield break;
        if (isStarting) yield break;

        isStarting = true;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ GameManager.Instance 없음");
            yield break;
        }

        TrackData selected = tracks[currentSelectedIndex];
        if (selected == null)
        {
            Debug.LogError("❌ 선택된 TrackData가 null");
            yield break;
        }

        // ✅ 반드시 저장
        GameManager.Instance.SelectTrack(selected);

        yield return null;

        // ✅ 반드시 Main으로 이동
        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading("MainGame");
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
    }

    public void StopPreview()
    {
        if (!isPlayingPreview) return;

        if (videoPlayer != null && videoPlayer.isPlaying)
            videoPlayer.Stop();

        ShowIdleScreen();

        if (windowBottom != null)
        {
            windowBottom.SetActive(false);
            windowBottomShown = false;
        }

        isPlayingPreview = false;

        if (TrackSelectorManager.Instance != null)
            TrackSelectorManager.Instance.OnTrackSelectorStopped(this);
    }
}
