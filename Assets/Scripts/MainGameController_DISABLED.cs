using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MainGameController : MonoBehaviour
{
    [Header("UI")]
    public Button startButton;        // MainGame 씬 Start 버튼
    public GameObject startPanel;     // (선택) StartPanel 루트 (없으면 null 가능)

    [Header("Video")]
    public VideoPlayer videoPlayer;  // MainGame 씬 VideoPlayer

    // MainGame Start 버튼 눌렀을 때 모든 시스템(NPC/노트 등) 시작 신호
    public static event Action OnSongStart;

    private bool started = false;

    private void Start()
    {
        // 시작 전에는 영상 재생 금지
        if (videoPlayer != null)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.Stop();
        }

        // Start 버튼 바인딩
        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnClickStart);
        }

        // 선택 트랙 없으면 버튼 비활성
        if (GameManager.Instance == null || GameManager.Instance.GetSelectedTrack() == null)
        {
            Debug.LogError("[MainGameController] 선택된 TrackData가 없습니다. OpeningTest에서 트랙 선택 후 Start를 눌렀는지 확인.");
            if (startButton != null) startButton.interactable = false;
        }
    }

    private void OnClickStart()
    {
        if (started) return;

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("[MainGameController] GameManager.Instance 없음");
            return;
        }

        TrackData track = gm.GetSelectedTrack();
        if (track == null)
        {
            Debug.LogError("[MainGameController] 선택된 TrackData 없음");
            return;
        }

        if (track.gameVideo == null)
        {
            Debug.LogError("[MainGameController] TrackData.gameVideo가 비어있습니다.");
            return;
        }

        started = true;

        // UI 닫기(원하면)
        if (startPanel != null) startPanel.SetActive(false);
        if (startButton != null) startButton.interactable = false;

        StartCoroutine(CoPrepareAndPlay(track.gameVideo));
    }

    private IEnumerator CoPrepareAndPlay(VideoClip clip)
    {
        if (videoPlayer == null)
        {
            Debug.LogError("[MainGameController] videoPlayer 미연결");
            yield break;
        }

        // 안정 세팅
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.playOnAwake = false;

        videoPlayer.clip = clip;

        videoPlayer.Prepare();

        float timeout = 10f;
        float t = 0f;
        while (!videoPlayer.isPrepared && t < timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared)
        {
            Debug.LogError("[MainGameController] Video Prepare timeout");
            yield break;
        }

        // ✅ “시작 기준점”
        videoPlayer.Play();

        // 첫 프레임 이후에 이벤트 쏘면 더 안정적
        yield return null;

        OnSongStart?.Invoke();
        Debug.Log("[MainGameController] ✅ OnSongStart fired");
    }
}
