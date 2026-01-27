using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.Rendering; // AmbientMode
using UnityEngine.XR;

// ✅ CommonUsages 모호성 방지 (InputSystem 쪽 CommonUsages랑 충돌 방지)
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public class IntroSceneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("UI (Optional)")]
    [SerializeField] private CanvasGroup fadeGroup;      // 없어도 됨
    [SerializeField] private GameObject skipHintRoot;    // 없어도 됨

    [Header("Next Scene")]
    [SerializeField] private string nextSceneName = "Home";

    [Header("Skip")]
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private float skipEnableDelay = 0.75f;     // 시작 직후 오동작 방지
    [SerializeField] private float fallbackTimeout = 40f;       // 영상/이벤트 꼬일 때 강제 다음 씬

    [Header("Fix (Intro -> Home)")]
    [Tooltip("Home에서 쓰는 Skybox Material이 있으면 넣어줘(없으면 비워도 됨)")]
    [SerializeField] private Material homeSkybox;
    [SerializeField] private bool restoreRenderSettings = true;
    [SerializeField] private bool forceMainCameraSkybox = true;

    [Header("Debug / Editor Only (Optional)")]
    [SerializeField] private KeyCode editorSkipKey = KeyCode.Space;

    private bool _loading;
    private float _skipTimer;
    private Coroutine _playRoutine;

    // XR 버튼 Down 판정용(눌림 유지가 아니라 '방금 눌림'만 잡기)
    private bool _prevRightPrimary;

    private void OnEnable()
    {
        // 인트로는 타임스케일 1로 강제 (Pause 남아있으면 영상 멈춤 방지)
        Time.timeScale = 1f;
    }

    private void Start()
    {
        if (skipHintRoot) skipHintRoot.SetActive(false);

        if (fadeGroup)
        {
            fadeGroup.alpha = 1f;
            fadeGroup.blocksRaycasts = true;
        }

        if (videoPlayer == null)
        {
            Debug.LogError("[Intro] VideoPlayer not assigned. Loading next scene.");
            LoadNextImmediate();
            return;
        }

        videoPlayer.isLooping = false;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.errorReceived += OnVideoError;

        _playRoutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        // Prepare
        videoPlayer.Prepare();

        float prepT = 0f;
        while (!videoPlayer.isPrepared && prepT < 8f)
        {
            prepT += Time.unscaledDeltaTime;
            yield return null;
        }

        // Play
        videoPlayer.Play();

        // Fade-in (optional)
        if (fadeGroup)
        {
            float t = 0f;
            const float dur = 0.35f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(1f, 0f, t / dur);
                yield return null;
            }
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }

        // Enable skip UI after delay
        _skipTimer = 0f;
        yield return new WaitForSecondsRealtime(skipEnableDelay);
        if (skipHintRoot) skipHintRoot.SetActive(true);

        // Timeout safety
        float timer = 0f;
        while (!_loading)
        {
            timer += Time.unscaledDeltaTime;
            if (fallbackTimeout > 0f && timer >= fallbackTimeout)
            {
                Debug.LogWarning("[Intro] Timeout -> load next.");
                TriggerLoadNext();
                yield break;
            }
            yield return null;
        }
    }

    private void Update()
    {
        if (_loading) return;

        _skipTimer += Time.unscaledDeltaTime;
        if (!allowSkip || _skipTimer < skipEnableDelay) return;

        // ✅ 1) Quest A 버튼 스킵 (XR 방식: OCULUS_INTEGRATION 없어도 동작)
        if (IsAButtonDownXR())
        {
            TriggerLoadNext();
            return;
        }

        // ✅ 2) Oculus Integration이 있을 때는 OVRInput도 추가 지원(있으면 더 안정적)
        // (심볼 없어도 XR로 이미 되니까, 이건 "있으면 보너스"임)
#if OCULUS_INTEGRATION
        if (OVRInput.GetDown(OVRInput.RawButton.A) || OVRInput.GetDown(OVRInput.Button.One))
        {
            TriggerLoadNext();
            return;
        }
#endif

        // ✅ 3) Editor/PC 테스트용 키 (Legacy Input이 켜져있을 때만)
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(editorSkipKey))
        {
            TriggerLoadNext();
            return;
        }
#endif
    }

    // XR 방식 A 버튼 Down 판정
    private bool IsAButtonDownXR()
    {
        // Quest에서 A는 보통 RightHand primaryButton
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!right.isValid) return false;

        right.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool curA);

        bool down = curA && !_prevRightPrimary;
        _prevRightPrimary = curA;

        return down;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        TriggerLoadNext();
    }

    private void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[Intro] Video error: " + msg);
        TriggerLoadNext();
    }

    /// <summary>
    /// 씬 전환 트리거 (버튼이 눌린 채 넘어가면 다음 씬에서 GetDown이 씹히는 현상 완화)
    /// </summary>
    private void TriggerLoadNext()
    {
        if (_loading) return;
        _loading = true;

        if (skipHintRoot) skipHintRoot.SetActive(false);

        StartCoroutine(LoadNextAfterRelease());
    }

    private IEnumerator LoadNextAfterRelease()
    {
        // A 버튼이 눌린 상태로 씬이 넘어가면 다음 씬에서 A Down이 안 잡힐 수 있어서
        // 최대 0.4초 정도만 "떼는 순간"까지 기다림
        float t = 0f;

        while (IsAButtonHeldXR() && t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

#if OCULUS_INTEGRATION
        // OVR도 눌림 상태면 같이 기다려줌(있으면 보너스)
        t = 0f;
        while (OVRInput.Get(OVRInput.RawButton.A) && t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
#endif

        RestoreGlobalAfterIntro();
        CleanupVideo();
        SceneManager.LoadScene(nextSceneName);
    }

    private bool IsAButtonHeldXR()
    {
        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!right.isValid) return false;

        right.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool curA);
        return curA;
    }

    private void LoadNextImmediate()
    {
        if (_loading) return;
        _loading = true;

        RestoreGlobalAfterIntro();
        CleanupVideo();
        SceneManager.LoadScene(nextSceneName);
    }

    // ✅ "바닥 까매짐" 같은 전역 RenderSettings/카메라 상태 원복
    private void RestoreGlobalAfterIntro()
    {
        if (restoreRenderSettings)
        {
            if (homeSkybox != null)
                RenderSettings.skybox = homeSkybox;

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1f;
        }

        if (forceMainCameraSkybox)
        {
            var cam = Camera.main;
            if (cam != null)
                cam.clearFlags = CameraClearFlags.Skybox;
        }
    }

    private void CleanupVideo()
    {
        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
            videoPlayer.errorReceived -= OnVideoError;

            try
            {
                if (videoPlayer.isPlaying) videoPlayer.Stop();
            }
            catch { }
        }
    }

    private void OnDestroy()
    {
        CleanupVideo();
    }
}
