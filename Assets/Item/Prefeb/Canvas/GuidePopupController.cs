using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class GuidePopupController : MonoBehaviour
{
    [Header("Wiring")]
    [Tooltip("애니메이션 대상(Panel). 보통 PopupWindow(RectTransform)")]
    [SerializeField] private RectTransform guideRoot;

    [Tooltip("옵션창의 '가이드' 버튼 근처 목표 지점(GuideTargetPoint)")]
    [SerializeField] private Transform flyToTarget;

    [Tooltip("Canvas-Guide 루트(또는 가이드 루트)에 붙은 CanvasGroup")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Auto Show Rule")]
    [SerializeField] private string homeSceneName = "Home";
    [Tooltip("실행(Play) 시작 후 Home에 최초 1회만 자동 표시")]
    [SerializeField] private bool showOnlyOncePerPlay = true;

    [Header("Close Animation (확인 버튼)")]
    [SerializeField] private float closeDuration = 0.35f;
    [SerializeField] private float endScale = 0.05f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Open Animation (옵션의 가이드 버튼)")]
    [SerializeField] private bool animateOpen = true;
    [SerializeField] private float openDuration = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    // ✅ "이번 Play 실행" 동안 자동표시 1회 보장
    private static bool s_shownOnceThisPlay;

    // Domain Reload OFF여도 Play 시작마다 초기화
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        s_shownOnceThisPlay = false;
    }

    private Vector3 _startLocalPos;
    private Vector3 _startLocalScale;
    private Quaternion _startLocalRot;
    private bool _poseCaptured;

    private bool _isAnimating;
    private Coroutine _routine;

    private void Awake()
    {
        // guideRoot 자동 탐색(가장 안전한 순서)
        if (guideRoot == null)
        {
            // 1) 자기 자신이 RectTransform이면 그걸 우선
            if (TryGetComponent(out RectTransform selfRt))
                guideRoot = selfRt;
            else
                guideRoot = GetComponentInChildren<RectTransform>(true);
        }

        // CanvasGroup 자동 탐색 (없으면 만들어서라도 동작하게)
        if (canvasGroup == null)
        {
            // 가이드 루트에서 먼저 찾고, 없으면 부모쪽에서 찾기
            if (guideRoot != null)
                canvasGroup = guideRoot.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = GetComponentInParent<CanvasGroup>(true);

            // 그래도 없으면 생성 (누락 때문에 기능이 아예 죽는 걸 방지)
            if (canvasGroup == null && guideRoot != null)
            {
                canvasGroup = guideRoot.gameObject.AddComponent<CanvasGroup>();
                if (debugLog) Debug.LogWarning("[GuidePopupController] CanvasGroup missing -> auto added on guideRoot.");
            }
        }

        if (guideRoot == null)
        {
            Debug.LogError("[GuidePopupController] guideRoot is missing. Assign a RectTransform (Panel).");
            enabled = false;
            return;
        }

        if (canvasGroup == null)
        {
            Debug.LogError("[GuidePopupController] CanvasGroup is missing and could not be created.");
            enabled = false;
            return;
        }

        CaptureStartPoseIfNeeded();
        HideInstant(); // 시작 플래시 방지
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // 중복 구독 방지
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ✅ 씬 로드 완료 시점
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // UI 배치가 다음 프레임에 잡히는 경우가 많아서 1프레임 뒤 처리
        StartCoroutine(CoApplyAfterOneFrame(scene.name));
    }

    private IEnumerator CoApplyAfterOneFrame(string sceneName)
    {
        yield return null;

        CaptureStartPoseIfNeeded();

        if (debugLog)
            Debug.Log($"[GuidePopupController] SceneLoaded={sceneName} shownOnce={s_shownOnceThisPlay}");

        // Home이 아니면 무조건 숨김
        if (!string.Equals(sceneName, homeSceneName))
        {
            HideInstant();
            yield break;
        }

        // Home이면
        if (!showOnlyOncePerPlay)
        {
            ShowInstant();
            yield break;
        }

        // Home 최초 1회만 자동 표시
        if (!s_shownOnceThisPlay)
        {
            s_shownOnceThisPlay = true;
            ShowInstant();
        }
        else
        {
            HideInstant();
        }
    }

    // ───────── 버튼에서 호출 ─────────

    // 확인 버튼 OnClick
    public void CloseWithFly()
    {
        if (!isActiveAndEnabled) return;
        if (_isAnimating) return;

        StartAnim(CoCloseFly());
    }

    // 옵션창 "가이드" 버튼 OnClick
    public void OpenGuide()
    {
        if (!isActiveAndEnabled) return;
        if (_isAnimating) return;

        CaptureStartPoseIfNeeded();
        RestoreToStartPose();
        ForceShow();

        if (animateOpen)
            StartAnim(CoOpenPop());
    }

    // 외부에서 강제 숨김이 필요할 때
    public void HideGuideInstant()
    {
        HideInstant();
    }

    // ───────── 내부 유틸 ─────────

    private void CaptureStartPoseIfNeeded()
    {
        if (_poseCaptured || guideRoot == null) return;

        _startLocalPos = guideRoot.localPosition;
        _startLocalScale = guideRoot.localScale;
        _startLocalRot = guideRoot.localRotation;
        _poseCaptured = true;

        if (debugLog) Debug.Log("[GuidePopupController] Pose captured.");
    }

    private void RestoreToStartPose()
    {
        if (!_poseCaptured) CaptureStartPoseIfNeeded();
        if (!_poseCaptured || guideRoot == null) return;

        guideRoot.localPosition = _startLocalPos;
        guideRoot.localScale = _startLocalScale;
        guideRoot.localRotation = _startLocalRot;
    }

    private void ForceShow()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }

    private void ForceHide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void ShowInstant()
    {
        StopAnim();
        RestoreToStartPose();
        ForceShow();
    }

    private void HideInstant()
    {
        StopAnim();
        RestoreToStartPose(); // 다음 오픈 대비 항상 원위치
        ForceHide();
    }

    private void StartAnim(IEnumerator r)
    {
        StopAnim();
        _routine = StartCoroutine(r);
    }

    private void StopAnim()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }
        _isAnimating = false;
    }

    // ───────── 애니메이션 ─────────

    private IEnumerator CoCloseFly()
    {
        _isAnimating = true;

        // 클릭 막기
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Vector3 fromPos = guideRoot.position;
        Vector3 toPos = (flyToTarget != null) ? flyToTarget.position : fromPos;

        Vector3 fromScale = guideRoot.localScale;
        Vector3 toScale = _startLocalScale * Mathf.Max(0.001f, endScale);

        float t = 0f;
        while (t < closeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / closeDuration);
            float e = ease.Evaluate(p);

            guideRoot.position = Vector3.Lerp(fromPos, toPos, e);
            guideRoot.localScale = Vector3.Lerp(fromScale, toScale, e);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, e);

            yield return null;
        }

        HideInstant();

        _isAnimating = false;
        _routine = null;
    }

    private IEnumerator CoOpenPop()
    {
        _isAnimating = true;

        Vector3 baseScale = _startLocalScale;
        guideRoot.localScale = baseScale * 0.85f;

        float t = 0f;
        while (t < openDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / openDuration);
            float e = ease.Evaluate(p);

            guideRoot.localScale = Vector3.Lerp(baseScale * 0.85f, baseScale, e);
            yield return null;
        }

        guideRoot.localScale = baseScale;

        _isAnimating = false;
        _routine = null;
    }
}
