using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class GameSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button startGameButton;
    [SerializeField] private GameObject startGameButtonRoot;

    [Header("대기 화면(Idle)")]
    [SerializeField] private Image idleImage;
    [SerializeField] private GameObject idleRoot;

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

        SetIdleVisible(true);
        SetVideoVisible(false);
        SetStartButtonVisible(true);

        if (verboseLog) Debug.Log("✅ [GameScene] Awake: 초기 UI 세팅 완료");
    }

    void Start()
    {
        BindStartButton();
        StartCoroutine(Init());
    }

    void BindStartButton()
    {
        if (startGameButton == null)
        {
            Debug.LogError("❌ [GameScene] startGameButton이 인스펙터에 연결되지 않았습니다.");
            return;
        }

        startGameButton.onClick.RemoveAllListeners();
        startGameButton.onClick.AddListener(StartGame);

        if (verboseLog) Debug.Log("✅ [GameScene] Start 버튼 리스너 연결 완료");
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
            Debug.LogError("❌ [GameScene] GameManager.Instance가 없습니다!");
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
            Debug.LogError("❌ [GameScene] 선택된 트랙이 없습니다!");
            yield break;
        }

        selectedTrack = GameManager.Instance.selectedTrack;

        if (verboseLog)
        {
            Debug.Log($"✅ [GameScene] 트랙 확보: {selectedTrack.trackName}");
            Debug.Log($"✅ [GameScene] BPM: {selectedTrack.bpm}");
            Debug.Log($"✅ [GameScene] audioClip: {(selectedTrack.audioClip ? selectedTrack.audioClip.name : "없음")}");
            Debug.Log($"✅ [GameScene] gameVideo: {(selectedTrack.gameVideo ? selectedTrack.gameVideo.name : "없음")}");
            Debug.Log($"✅ [GameScene] beatMap: {(selectedTrack.beatMap ? selectedTrack.beatMap.name : "없음")}");
        }

        // ✅ 3) BeatMapSpawner에 BeatMap 자동 할당
        SetupBeatMapSpawner();

        // ✅ 4) MusicManager 설정
        SetupMusicManager();

        // 5) VideoPlayer 준비
        PrepareVideo();

        isReady = true;

        if (verboseLog) Debug.Log("✅ [GameScene] Init 완료!");
    }

    void SetupBeatMapSpawner()
    {
        if (selectedTrack == null || selectedTrack.beatMap == null)
        {
            Debug.LogError("❌ [GameScene] selectedTrack 또는 beatMap이 없습니다!");
            return;
        }

        BeatMapSpawner spawner = FindObjectOfType<BeatMapSpawner>();
        if (spawner == null)
        {
            Debug.LogError("❌ [GameScene] BeatMapSpawner를 찾을 수 없습니다!");
            return;
        }

        // ✅ Reflection을 사용하여 jsonFile 필드 가져오기
        var beatMapType = selectedTrack.beatMap.GetType();
        var jsonFileField = beatMapType.GetField("jsonFile");

        if (jsonFileField == null)
        {
            Debug.LogError("❌ [GameScene] BeatMap에 'jsonFile' 필드를 찾을 수 없습니다!");
            return;
        }

        TextAsset jsonFile = jsonFileField.GetValue(selectedTrack.beatMap) as TextAsset;

        if (jsonFile == null)
        {
            Debug.LogError("❌ [GameScene] BeatMap의 jsonFile이 null입니다!");
            return;
        }

        spawner.beatMapJson = jsonFile;

        if (verboseLog)
        {
            Debug.Log($"✅ [GameScene] BeatMapSpawner에 JSON 할당: {jsonFile.name}");
        }
    }

    void SetupMusicManager()
    {
        if (MusicManager.Instance == null)
        {
            Debug.LogWarning("⚠ [GameScene] MusicManager가 없습니다!");
            return;
        }

        if (selectedTrack == null)
        {
            Debug.LogError("❌ [GameScene] selectedTrack이 null!");
            return;
        }

        MusicManager.Instance.bpm = selectedTrack.bpm;

        if (verboseLog)
        {
            Debug.Log($"✅ [GameScene] MusicManager BPM 설정: {selectedTrack.bpm}");
        }
    }

    void PrepareVideo()
    {
        if (gameVideoPlayer == null)
        {
            Debug.LogError("❌ [GameScene] gameVideoPlayer 미연결!");
            return;
        }

        if (selectedTrack == null || selectedTrack.gameVideo == null)
        {
            Debug.LogError("❌ [GameScene] gameVideo가 없습니다!");
            return;
        }

        gameVideoPlayer.Stop();
        gameVideoPlayer.source = VideoSource.VideoClip;
        gameVideoPlayer.clip = selectedTrack.gameVideo;
        gameVideoPlayer.playOnAwake = false;
        gameVideoPlayer.isLooping = false;
        gameVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;

        ApplyVideoVolume(GetMusicVolume());

        if (verboseLog) Debug.Log("✅ [GameScene] VideoPlayer 준비 완료");
    }

    float GetMusicVolume()
    {
        if (GameManager.Instance != null)
            return Mathf.Clamp01(GameManager.Instance.GetMusicVolume());
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

            if (verboseLog) Debug.Log($"🎬 [GameScene] Video 볼륨: {v:F2}");
        }
        catch
        {
            Debug.LogWarning("⚠ [GameScene] VideoPlayer 볼륨 설정 실패");
        }
    }

    void StartGame()
    {
        Debug.Log("🟩🟩🟩 [GameScene] StartGame() 호출! 🟩🟩🟩");

        if (isStarted) return;
        isStarted = true;

        if (!isReady)
        {
            Debug.LogError("❌ [GameScene] Init이 완료되지 않았습니다!");
            return;
        }

        if (selectedTrack == null)
        {
            Debug.LogError("❌ [GameScene] selectedTrack이 null!");
            return;
        }

        // UI 전환
        SetStartButtonVisible(false);
        SetIdleVisible(false);
        SetVideoVisible(true);

        // BGM 정지
        if (BGMManager.Instance != null)
            BGMManager.Instance.ForceMute();

        // 음악 시작
        if (MusicManager.Instance != null && selectedTrack.audioClip != null)
        {
            MusicManager.Instance.SetMusic(selectedTrack.audioClip, selectedTrack.bpm);
            MusicManager.Instance.SetVolume(GetMusicVolume());
            MusicManager.Instance.PlayMusic();

            if (verboseLog) Debug.Log("🎵 [GameScene] 음악 재생 시작");
        }
        else
        {
            Debug.LogWarning("⚠ [GameScene] MusicManager 또는 audioClip 없음");
        }

        // 비디오 시작
        if (gameVideoPlayer != null && gameVideoPlayer.clip != null)
        {
            ApplyVideoVolume(GetMusicVolume());
            gameVideoPlayer.time = 0;
            gameVideoPlayer.Play();

            if (verboseLog) Debug.Log("🎬 [GameScene] Video 재생 시작");
        }
        else
        {
            Debug.LogError("❌ [GameScene] VideoPlayer 또는 clip 없음");
        }

        // ✅ BeatMapSpawner 시작 (직접 호출)
        BeatMapSpawner spawner = FindObjectOfType<BeatMapSpawner>();
        if (spawner != null)
        {
            spawner.BeginSpawn();
            if (verboseLog) Debug.Log("✅ [GameScene] BeatMapSpawner.BeginSpawn() 호출");
        }

        // 리듬게임 시작 신호
        if (RhythmGameManager.Instance != null)
            RhythmGameManager.Instance.NotifyGameStarted();
    }

    void SetIdleVisible(bool on)
    {
        if (idleRoot != null) idleRoot.SetActive(on);
        if (idleImage != null) idleImage.gameObject.SetActive(on);
    }

    void SetVideoVisible(bool on)
    {
        if (gameVideoRawImage != null) gameVideoRawImage.gameObject.SetActive(on);
    }

    void SetStartButtonVisible(bool on)
    {
        if (startGameButtonRoot != null) startGameButtonRoot.SetActive(on);
        if (startGameButton != null) startGameButton.gameObject.SetActive(on);
    }
}