using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoScreenManager : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer videoPlayer;
    public RawImage videoScreen;
    public VideoClip musicVideoClip;

    [Header("Screen Settings")]
    public bool playOnStart = true;
    public bool loopVideo = true;
    public float screenOpacity = 0.8f;

    [Header("Sync with Music")]
    public MusicManager musicManager;
    public bool syncWithMusic = true;

    private RenderTexture renderTexture;

    void Start()
    {
        SetupVideoPlayer();
        SetupOpacity();

        if (playOnStart)
        {
            PlayVideo();
        }
    }

    void SetupVideoPlayer()
    {
        Debug.Log("=== Video Player Setup Start ===");

        // Render Texture 생성
        renderTexture = new RenderTexture(1920, 1080, 16);
        renderTexture.Create();

        // Video Player 설정
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.isLooping = loopVideo;
        videoPlayer.playOnAwake = false;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None; // 소리 끄기
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;

        if (musicVideoClip != null)
        {
            videoPlayer.clip = musicVideoClip;
            Debug.Log($"Video clip assigned: {musicVideoClip.name}");
        }
        else
        {
            Debug.LogWarning("Music Video Clip is not assigned!");
        }

        // RawImage에 Render Texture 할당
        if (videoScreen != null)
        {
            videoScreen.texture = renderTexture;
            Debug.Log("Render Texture assigned to RawImage");
        }
        else
        {
            Debug.LogError("Video Screen RawImage is NULL!");
        }

        // 이벤트 등록
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;

        Debug.Log("=== Video Player Setup Complete ===");
    }

    void SetupOpacity()
    {
        if (videoScreen != null)
        {
            Color color = videoScreen.color;
            color.a = screenOpacity;
            videoScreen.color = color;
        }
    }

    void Update()
    {
        // 음악과 동기화
        if (syncWithMusic && musicManager != null && musicManager.audioSource != null)
        {
            // 음악이 재생 중인데 비디오가 안 재생 중이면
            if (musicManager.audioSource.isPlaying && !videoPlayer.isPlaying)
            {
                PlayVideo();
            }
            // 음악이 멈췄는데 비디오가 재생 중이면
            else if (!musicManager.audioSource.isPlaying && videoPlayer.isPlaying)
            {
                PauseVideo();
            }
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log("✅ Video prepared successfully!");
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"❌ Video Error: {message}");
    }

    public void PlayVideo()
    {
        if (videoPlayer != null && musicVideoClip != null)
        {
            Debug.Log("▶️ Starting video playback...");
            videoPlayer.Play();
        }
        else
        {
            Debug.LogWarning("Cannot play video - VideoPlayer or Clip is null");
        }
    }

    public void PauseVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            Debug.Log("⏸️ Video paused");
        }
    }

    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            Debug.Log("⏹️ Video stopped");
        }
    }

    void OnDestroy()
    {
        // Render Texture 정리
        if (renderTexture != null)
        {
            renderTexture.Release();
        }
    }
}
