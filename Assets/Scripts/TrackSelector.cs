// TrackSelector.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class TrackSelector : MonoBehaviour
{
    public static TrackSelector Instance { get; private set; }

    private const string KEY_MUSIC = "MusicVolume";

    [Header("선택: 전체 트랙(15개) - 사용 안 해도 됨")]
    public TrackData[] allTracks;

    [Serializable]
    public class PageUI
    {
        [Header("이 페이지(캔버스) 루트")]
        public GameObject pageRoot; // Canvas - TrackSelect01/02/03

        [Header("✅ 이 페이지에 연결할 TrackData들(여기에 5개 드래그)")]
        public TrackData[] pageTracks;

        [Header("미리보기 화면")]
        public RawImage previewRawImage;
        public VideoPlayer videoPlayer;
        public Image previewOverlay;
        public Button previewButton;

        [Header("대기 화면")]
        public Image idleImageDisplay;

        [Header("곡 썸네일 버튼(이 페이지에 있는 5개)")]
        public Button[] trackButtons;   // 5개
        public Image[] trackImages;     // 5개 (선택 강조)

        [Header("UI 요소")]
        public Text trackNameText;
        public Text artistText;
        public Button startButton;
        public Button tutorialButton;
        public GameObject windowBottom;

        [Header("Preview Audio (없으면 자동 생성)")]
        public AudioSource previewAudioSource;
    }

    [Header("페이지 UI (Canvas-TrackSelect01/02/03)")]
    public PageUI[] pages;

    [Header("설정")]
    public float videoStartDelay = 0.2f;

    [Header("선택 효과")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("씬 이동")]
    [SerializeField] private string gameSceneName = "MainGame";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    [Header("프리뷰 기본 볼륨(저장값 없을 때만 사용)")]
    [Range(0f, 1f)] public float previewDefaultVolume = 1f;

    // 현재 선택
    private TrackData _selectedTrack = null;
    private PageUI _currentPage = null;

    // 프리뷰 제어
    private Coroutine _previewRoutine;
    private int _previewReqId = 0;
    private bool _isPreparing = false;
    private bool _isStarting = false;
    private bool _isPreviewPaused = false;

    // BGM 뮤트 begin/end 안정화
    private bool _bgmMutedByThis = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 각 페이지 오디오/비디오 출력 모드 세팅
        if (pages != null)
        {
            for (int p = 0; p < pages.Length; p++)
            {
                var page = pages[p];
                if (page == null) continue;

                EnsurePageAudio(page);
                SetupVideoAudio(page);
            }
        }
    }

    private void Start()
    {
        ValidatePages();
        BindAllPages();

        // ✅ 3개 캔버스 펼쳐두기
        SetAllPagesActive(true);

        // ✅ 시작 시 저장된 MusicVolume을 모든 페이지 프리뷰에 강제 적용 (페이지마다 커지는 문제 방지)
        float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, previewDefaultVolume));
        ApplyPreviewVolume(saved);

        ResetAllPagesToIdle();
        EndBgmMuteIfNeeded();
    }

    private void OnEnable()
    {
        // 캔버스/오브젝트가 꺼졌다 켜질 때도 저장값 재적용
        float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, previewDefaultVolume));
        ApplyPreviewVolume(saved);
    }

    private void OnDisable()
    {
        StopAllPreviews();
        EndBgmMuteIfNeeded();
    }

    private void ValidatePages()
    {
        if (pages == null) return;

        for (int i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            if (page == null) continue;

            if (page.pageRoot == null)
                Debug.LogWarning($"[TrackSelector] Pages[{i}] pageRoot(Canvas)가 비어있습니다.");

            if (page.pageTracks == null || page.pageTracks.Length == 0)
                Debug.LogWarning($"[TrackSelector] Pages[{i}] pageTracks가 비어있습니다. (Canvas {page.pageRoot?.name})");
        }
    }

    private void SetAllPagesActive(bool on)
    {
        if (pages == null) return;
        for (int i = 0; i < pages.Length; i++)
        {
            if (pages[i] != null && pages[i].pageRoot != null)
                pages[i].pageRoot.SetActive(on);
        }
    }

    private void BindAllPages()
    {
        if (pages == null) return;

        for (int p = 0; p < pages.Length; p++)
        {
            var page = pages[p];
            if (page == null) continue;

            // 트랙 버튼: 페이지 로컬 idx(0~4) -> pageTracks[0~4]
            if (page.trackButtons != null)
            {
                for (int i = 0; i < page.trackButtons.Length; i++)
                {
                    int localIdx = i;
                    var btn = page.trackButtons[localIdx];
                    if (btn == null) continue;

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() =>
                    {
                        PlayClickSound();
                        SelectTrack(page, localIdx);
                    });
                }
            }

            // 프리뷰 토글
            if (page.previewButton != null)
            {
                page.previewButton.onClick.RemoveAllListeners();
                page.previewButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    TogglePreview(page);
                });
            }

            // Start
            if (page.startButton != null)
            {
                page.startButton.onClick.RemoveAllListeners();
                page.startButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    StartGame();
                });
            }

            // Tutorial
            if (page.tutorialButton != null)
            {
                page.tutorialButton.onClick.RemoveAllListeners();
                page.tutorialButton.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    GoTutorial();
                });
            }

            // 선택 전 하단 UI 숨김
            if (page.windowBottom != null) page.windowBottom.SetActive(false);
        }
    }

    // =========================
    // ✅ 선택: (페이지, 로컬 인덱스) -> TrackData
    // =========================
    private void SelectTrack(PageUI page, int localIdx)
    {
        if (page == null) return;

        var track = GetTrackFromPage(page, localIdx);
        if (track == null)
        {
            Debug.LogWarning($"[TrackSelector] 선택 실패: pageTracks가 비었거나 localIdx({localIdx}) 범위 오류 (Canvas {page.pageRoot?.name})");
            return;
        }

        _selectedTrack = track;
        _currentPage = page;

        // 다른 페이지 프리뷰 정리
        StopAllPreviews();

        // 선택 강조/텍스트/하단UI 갱신
        UpdateAllSelectionsUI();
        UpdatePageText(page, track);
        if (page.windowBottom != null) page.windowBottom.SetActive(true);

        // 프리뷰 시작
        if (_previewRoutine != null) StopCoroutine(_previewRoutine);
        _previewRoutine = StartCoroutine(PlayPreviewWithDelay(track, page));
    }

    private TrackData GetTrackFromPage(PageUI page, int localIdx)
    {
        if (page.pageTracks != null && localIdx >= 0 && localIdx < page.pageTracks.Length)
            return page.pageTracks[localIdx];

        return null;
    }

    private void UpdatePageText(PageUI page, TrackData t)
    {
        if (page.trackNameText != null) page.trackNameText.text = (t != null) ? t.trackName : "";
        if (page.artistText != null) page.artistText.text = (t != null) ? t.artistName : "";
    }

    private void UpdateAllSelectionsUI()
    {
        if (pages == null) return;

        for (int p = 0; p < pages.Length; p++)
        {
            var page = pages[p];
            if (page == null || page.trackImages == null) continue;

            for (int i = 0; i < page.trackImages.Length; i++)
            {
                var img = page.trackImages[i];
                if (img == null) continue;

                var t = (page.pageTracks != null && i < page.pageTracks.Length) ? page.pageTracks[i] : null;
                img.color = (t != null && t == _selectedTrack) ? selectedColor : normalColor;
            }
        }
    }

    // =========================
    // 프리뷰
    // =========================
    private IEnumerator PlayPreviewWithDelay(TrackData track, PageUI page)
    {
        int myId = ++_previewReqId;
        yield return new WaitForSeconds(videoStartDelay);

        if (myId != _previewReqId) yield break;
        if (page == null || page.videoPlayer == null) yield break;

        if (track == null || track.previewVideo == null)
        {
            ShowIdle(page);
            EndBgmMuteIfNeeded();
            yield break;
        }

        // UI
        if (page.idleImageDisplay != null) page.idleImageDisplay.gameObject.SetActive(false);
        if (page.previewOverlay != null) page.previewOverlay.gameObject.SetActive(false);
        if (page.previewRawImage != null) page.previewRawImage.gameObject.SetActive(true);

        _isPreviewPaused = false;

        // ✅ 볼륨은 "항상 저장값"이 정답
        EnsurePageAudio(page);
        float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, previewDefaultVolume));
        page.previewAudioSource.volume = saved;
        page.previewAudioSource.Stop();

        page.videoPlayer.Stop();
        page.videoPlayer.clip = null;

        page.videoPlayer.clip = track.previewVideo;
        page.videoPlayer.isLooping = true;

        _isPreparing = true;
        page.videoPlayer.Prepare();
        while (!page.videoPlayer.isPrepared)
        {
            if (myId != _previewReqId) { _isPreparing = false; yield break; }
            yield return null;
        }
        _isPreparing = false;

        if (myId != _previewReqId) yield break;

        BeginBgmMuteForPreview();
        page.videoPlayer.Play();
    }

    private void TogglePreview(PageUI page)
    {
        if (page == null || page.videoPlayer == null) return;
        if (_isPreparing) return;

        // 선택된 곡/페이지에서만 토글
        if (_selectedTrack == null) return;
        if (_currentPage != page) return;

        if (_isPreviewPaused)
        {
            EnsurePageAudio(page);
            // 저장 볼륨 다시 적용(페이지 전환 뒤 꼬임 방지)
            float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, previewDefaultVolume));
            page.previewAudioSource.volume = saved;

            page.videoPlayer.Play();
            _isPreviewPaused = false;
            BeginBgmMuteForPreview();
        }
        else
        {
            if (page.videoPlayer.isPlaying)
            {
                page.videoPlayer.Pause();
                _isPreviewPaused = true;
                EndBgmMuteIfNeeded();
            }
            else
            {
                EnsurePageAudio(page);
                float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, previewDefaultVolume));
                page.previewAudioSource.volume = saved;

                page.videoPlayer.Play();
                _isPreviewPaused = false;
                BeginBgmMuteForPreview();
            }
        }
    }

    private void ShowIdle(PageUI page)
    {
        if (page == null) return;

        if (page.videoPlayer != null) page.videoPlayer.Stop();

        if (page.previewRawImage != null) page.previewRawImage.gameObject.SetActive(false);
        if (page.previewOverlay != null) page.previewOverlay.gameObject.SetActive(true);
        if (page.idleImageDisplay != null) page.idleImageDisplay.gameObject.SetActive(true);

        _isPreviewPaused = false;
    }

    private void ResetAllPagesToIdle()
    {
        if (pages == null) return;
        for (int i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            if (page == null) continue;
            ShowIdle(page);
            if (page.windowBottom != null) page.windowBottom.SetActive(false);
        }
    }

    private void StopAllPreviews()
    {
        _previewReqId++;

        if (_previewRoutine != null) { StopCoroutine(_previewRoutine); _previewRoutine = null; }
        _isPreparing = false;
        _isPreviewPaused = false;

        if (pages != null)
        {
            for (int i = 0; i < pages.Length; i++)
            {
                var page = pages[i];
                if (page == null) continue;

                if (page.videoPlayer != null)
                {
                    page.videoPlayer.Stop();
                    page.videoPlayer.clip = null;
                }
                ShowIdle(page);
            }
        }

        EndBgmMuteIfNeeded();
    }

    // =========================
    // 씬 이동
    // =========================
    public void StartGame()
    {
        if (_isStarting) return;
        StartCoroutine(StartGameCoroutine());
    }

    private IEnumerator StartGameCoroutine()
    {
        if (_isStarting) yield break;
        if (_selectedTrack == null) yield break;

        _isStarting = true;
        StopAllPreviews();

        TrackSession.Ensure().SetTrack(_selectedTrack);

        if (GameManager.Instance != null)
            GameManager.Instance.SelectTrack(_selectedTrack);

        yield return null;

        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading(gameSceneName);
        else
            SceneManager.LoadScene(gameSceneName);
    }

    public void GoTutorial()
    {
        if (_isStarting) return;
        StartCoroutine(GoTutorialCoroutine());
    }

    private IEnumerator GoTutorialCoroutine()
    {
        if (_isStarting) yield break;
        _isStarting = true;

        StopAllPreviews();
        yield return null;

        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading(tutorialSceneName);
        else
            SceneManager.LoadScene(tutorialSceneName);
    }

    // =========================
    // ✅ 옵션에서 프리뷰 볼륨 조절 (핵심: "모든 페이지"에 적용)
    // =========================
    public void ApplyPreviewVolume(float v)
    {
        v = Mathf.Clamp01(v);

        // 저장도 여기서 해버리면 OptionsManager가 없어도 일관성 유지됨
        PlayerPrefs.SetFloat(KEY_MUSIC, v);
        PlayerPrefs.Save();

        if (pages == null) return;

        for (int i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            if (page == null) continue;

            EnsurePageAudio(page);
            if (page.previewAudioSource != null)
                page.previewAudioSource.volume = v;
        }
    }

    // =========================
    // 내부 유틸
    // =========================
    private void PlayClickSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClickSound();
    }

    private void EnsurePageAudio(PageUI page)
    {
        if (page == null) return;

        if (page.previewAudioSource == null)
        {
            // ✅ AudioSource는 해당 pageRoot에 붙이는 게 안전
            if (page.pageRoot != null)
            {
                page.previewAudioSource = page.pageRoot.GetComponent<AudioSource>();
                if (page.previewAudioSource == null)
                    page.previewAudioSource = page.pageRoot.AddComponent<AudioSource>();
            }
            else
            {
                // pageRoot가 없으면 최후수단
                page.previewAudioSource = GetComponent<AudioSource>();
                if (page.previewAudioSource == null) page.previewAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        page.previewAudioSource.playOnAwake = false;
        page.previewAudioSource.loop = false;
        page.previewAudioSource.spatialBlend = 0f;

        // ✅ 초기값도 저장값으로
        float saved = Mathf.Clamp01(PlayerPrefs.GetFloat(KEY_MUSIC, previewDefaultVolume));
        page.previewAudioSource.volume = saved;
    }

    private void SetupVideoAudio(PageUI page)
    {
        if (page == null || page.videoPlayer == null) return;

        EnsurePageAudio(page);

        // VideoPlayer 오디오는 AudioSource로 통일
        page.videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        page.videoPlayer.SetTargetAudioSource(0, page.previewAudioSource);
    }

    private void BeginBgmMuteForPreview()
    {
        if (_bgmMutedByThis) return;
        if (BGMManager.Instance == null) return;

        BGMManager.Instance.BeginPreviewMute();
        _bgmMutedByThis = true;
    }

    private void EndBgmMuteIfNeeded()
    {
        if (!_bgmMutedByThis) return;

        if (BGMManager.Instance != null)
            BGMManager.Instance.EndPreviewMute();

        _bgmMutedByThis = false;
    }
}
