using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainTrackBootstrap : MonoBehaviour
{
    [Header("필수: 게임 영상 VideoPlayer")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("필수: 음악 MusicManager (메인게임 씬의 MusicManager)")]
    [SerializeField] private MusicManager musicManager;

    [Header("카운트다운 UI(있으면 연결, 없으면 비워도 됨)")]
    [SerializeField] private Text countdownText;   // TMP 쓰면 바꿔줄게
    [SerializeField] private float countdownSeconds = 3f;

    [Header("옵션")]
    [Tooltip("true면 씬 들어오자마자 카운트다운 시작")]
    [SerializeField] private bool autoStart = true;

    private Coroutine playRoutine;

    private void Awake()
    {
        if (musicManager == null) musicManager = FindObjectOfType<MusicManager>();
    }

    private void Start()
    {
        if (!autoStart) return;

        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PrepareCountdownAndPlay());
    }

    private IEnumerator PrepareCountdownAndPlay()
    {
        // 1) 선택 트랙 가져오기
        TrackData track = null;

        if (TrackSession.Instance != null)
            track = TrackSession.Instance.SelectedTrack;

        if (track == null)
        {
            Debug.LogError("SelectedTrack이 없습니다. Home에서 선곡 후 넘어왔는지 확인!");
            SceneManager.LoadScene("Home");
            yield break;
        }

        // 2) 필수 레퍼런스 체크
        if (videoPlayer == null)
        {
            Debug.LogError("MainTrackBootstrap: videoPlayer가 비어있습니다. Inspector에서 연결하세요.");
            yield break;
        }

        if (musicManager == null)
        {
            Debug.LogError("MainTrackBootstrap: musicManager를 찾지 못했습니다. 메인게임 씬에 MusicManager가 있어야 합니다.");
            yield break;
        }

        if (track.gameVideo == null)
        {
            Debug.LogError("SelectedTrack.gameVideo가 비어있습니다(TrackData 확인).");
            SceneManager.LoadScene("Home");
            yield break;
        }

        if (track.audioClip == null)
        {
            Debug.LogError("SelectedTrack.audioClip이 비어있습니다(TrackData 확인).");
            SceneManager.LoadScene("Home");
            yield break;
        }

        // ✅ 씬 들어오자마자 재생되는 걸 막기 위해, 여기서 확실히 멈춰둠
        if (musicManager.audioSource != null)
            musicManager.audioSource.Stop();

        videoPlayer.Stop();

        // 3) 비디오 Prepare (Quest/OpenXR 안정)
        videoPlayer.clip = track.gameVideo;
        videoPlayer.isLooping = false;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;

        // 4) 3-2-1 카운트다운
        float remaining = countdownSeconds;

        while (remaining > 0f)
        {
            int sec = Mathf.CeilToInt(remaining);
            if (countdownText != null) countdownText.text = sec.ToString();
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        if (countdownText != null) countdownText.text = "";

        // 5) ✅ 카운트다운 끝나는 순간: 음악 + 영상 동시 시작
        musicManager.SetMusic(track.audioClip, track.bpm);
        musicManager.PlayMusic();

        videoPlayer.Play();
    }

    // 필요하면 다른 스크립트에서 강제 시작도 가능
    public void ForceStartNow()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        playRoutine = StartCoroutine(PrepareCountdownAndPlay());
    }
}
