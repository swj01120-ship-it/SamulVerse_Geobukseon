using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class GameSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private GameObject startGameButtonRoot; // 버튼을 감싸는 패널이 있으면 넣기(없으면 비워도 됨)

    [Header("대기 화면(Idle)")]
    [SerializeField] private Image idleImage;               // 대기 이미지
    [SerializeField] private GameObject idleRoot;           // idleImage가 들어있는 패널(없으면 비워도 됨)

    [Header("게임 영상")]
    [SerializeField] private VideoPlayer gameVideoPlayer;
    [SerializeField] private RawImage gameVideoRawImage;

    [Header("디버그")]
    [SerializeField] private bool verboseLog = true;

    private TrackData selectedTrack;
    private bool isReady = false;
    private bool isStarted = false;

    void Awake()
    {
        Time.timeScale = 1f;

        // 초기 UI 상태: 대기화면 ON, 영상 OFF, 시작버튼 ON
        SetIdleVisible(true);
        SetVideoVisible(false);
        SetStartButtonVisible(true);

        if (verboseLog) Debug.Log("✅ [Main] Awake: 초기 UI 세팅 완료");
    }

    void Start()
    {
        // 버튼 연결(여기서 한번)
        BindStartButton();

        // 트랙/비디오 준비
        StartCoroutine(Init());
    }

    void BindStartButton()
    {
        if (startGameButton == null)
        {
            Debug.LogError("❌ [Main] startGameButton이 인스펙터에 연결되지 않았습니다.");
            return;
        }

        startGameButton.onClick.RemoveAllListeners();
        startGameButton.onClick.AddListener(StartGame);

        if (verboseLog) Debug.Log("✅ [Main] Start 버튼 리스너 연결 완료");
    }

    IEnumerator Init()
    {
        // 1) GameManager 기다림
        float t = 0f;
        while (GameManager.Instance == null && t < 3f)
        {
            t += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("❌ [Main] GameManager.Instance가 없습니다. (Opening에서 넘어올 때 GameManager가 유지되는지 확인)");
            yield break;
        }

        // 2) selectedTrack 기다림
        t = 0f;
        while (GameManager.Instance.selectedTrack == null && t < 3f)
        {
            t += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        if (GameManager.Instance.selectedTrack == null)
        {
            Debug.LogError("❌ [Main] 선택된 트랙이 없습니다. (Opening에서 Start로 진입했는지 / GameManager 중복 파괴됐는지 확인)");
            yield break;
        }

        selectedTrack = GameManager.Instance.selectedTrack;

        if (verboseLog)
        {
            Debug.Log($"✅ [Main] 트랙 확보: {selectedTrack.trackName} / BPM {selectedTrack.bpm}");
            Debug.Log($"✅ [Main] gameVideo: {(selectedTrack.gameVideo ? selectedTrack.gameVideo.name : "없음")}");
            Debug.Log($"✅ [Main] audioClip: {(selectedTrack.audioClip ? selectedTrack.audioClip.name : "없음")}");
        }

        // 3) VideoPlayer 준비
        PrepareVideo();

        // 준비 완료
        isReady = true;

        if (verboseLog) Debug.Log("✅ [Main] Init 완료: Start 버튼 누르면 게임 시작됩니다.");
    }

    void PrepareVideo()
    {
        if (gameVideoPlayer == null)
        {
            Debug.LogError("❌ [Main] gameVideoPlayer가 인스펙터에 연결되지 않았습니다.");
            return;
        }

        if (selectedTrack == null || selectedTrack.gameVideo == null)
        {
            Debug.LogError("❌ [Main] 선택 트랙의 gameVideo가 비어 있습니다.");
            return;
        }

        gameVideoPlayer.Stop();
        gameVideoPlayer.source = VideoSource.VideoClip;
        gameVideoPlayer.clip = selectedTrack.gameVideo;
        gameVideoPlayer.playOnAwake = false;
        gameVideoPlayer.isLooping = false;

        // 오디오 직접 출력
        gameVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        ApplyVideoVolume(GetMusicVolume());

        if (verboseLog) Debug.Log("✅ [Main] VideoPlayer 준비 완료");
    }

    float GetMusicVolume()
    {
        if (GameManager.Instance != null) return Mathf.Clamp01(GameManager.Instance.GetMusicVolume());
        return Mathf.Clamp01(PlayerPrefs.GetFloat("MusicVolume", 0.5f));
    }

    public void ApplyVideoVolume(float v)
    {
        if (gameVideoPlayer == null) return;

        v = Mathf.Clamp01(v);

        try
        {
            if (gameVideoPlayer.audioOutputMode != VideoAudioOutputMode.Direct)
                gameVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

            gameVideoPlayer.SetDirectAudioMute(0, false);
            gameVideoPlayer.SetDirectAudioVolume(0, v);

            if (verboseLog) Debug.Log($"🎬 [Main] Video 볼륨 적용: {v:F2}");
        }
        catch
        {
            Debug.LogWarning("⚠ [Main] VideoPlayer DirectAudioVolume 적용 중 예외가 발생했습니다(플랫폼/트랙 문제 가능).");
        }
    }

    void StartGame()
    {
        // ✅ 0) 버튼 눌림 확인용 로그 (가장 중요)
        Debug.Log("🟩🟩🟩 [Main] StartGame() 호출됨 (버튼 클릭됨) 🟩🟩🟩");

        if (isStarted) return;
        isStarted = true;

        if (!isReady)
        {
            Debug.LogError("❌ [Main] 아직 Init이 끝나지 않았습니다. (selectedTrack/Video 준비 실패)");
            return;
        }

        if (selectedTrack == null)
        {
            Debug.LogError("❌ [Main] StartGame: selectedTrack이 null입니다.");
            return;
        }

        // ✅ 1) UI 전환: 대기 OFF, 영상 ON, 버튼 OFF
        SetStartButtonVisible(false);
        SetIdleVisible(false);
        SetVideoVisible(true);

        // ✅ 2) 규칙: Music 시작 순간 BGM 무조건 0
        BGMManager.Instance?.ForceMute();

        // ✅ 3) 메인 음악 시작
        if (MusicManager.Instance != null && selectedTrack.audioClip != null)
        {
            MusicManager.Instance.SetMusic(selectedTrack.audioClip, selectedTrack.bpm);
            MusicManager.Instance.SetVolume(GetMusicVolume());
            MusicManager.Instance.PlayMusic();

            if (verboseLog) Debug.Log("🎵 [Main] MusicManager 재생 시작");
        }
        else
        {
            Debug.LogWarning("⚠ [Main] MusicManager 또는 selectedTrack.audioClip이 없습니다. (메인 음악은 스킵)");
        }

        // ✅ 4) 비디오 시작
        if (gameVideoPlayer != null && gameVideoPlayer.clip != null)
        {
            ApplyVideoVolume(GetMusicVolume());
            gameVideoPlayer.time = 0;
            gameVideoPlayer.Play();

            if (verboseLog) Debug.Log("🎬 [Main] Video 재생 시작");
        }
        else
        {
            Debug.LogError("❌ [Main] VideoPlayer 또는 clip이 없습니다. (영상 재생 불가)");
        }

        // ✅ 5) 리듬게임 시작 신호
        RhythmGameManager.Instance?.NotifyGameStarted();
    }

    void SetIdleVisible(bool on)
    {
        if (idleRoot != null) idleRoot.SetActive(on);
        if (idleImage != null) idleImage.gameObject.SetActive(on);
    }

    void SetVideoVisible(bool on)
    {
        if (gameVideoRawImage != null) gameVideoRawImage.gameObject.SetActive(on);
        if (gameVideoPlayer != null)
        {
            // 영상 표시 여부만 바꾸는 것이므로 재생은 StartGame에서만
        }
    }

    void SetStartButtonVisible(bool on)
    {
        if (startGameButtonRoot != null) startGameButtonRoot.SetActive(on);
        if (startGameButton != null) startGameButton.gameObject.SetActive(on);
    }
}
