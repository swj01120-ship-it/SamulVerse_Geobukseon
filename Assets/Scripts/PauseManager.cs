using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

// ✅ XR CommonUsages 명확히 지정
using XRCommonUsages = UnityEngine.XR.CommonUsages;

public class PauseManager : MonoBehaviour
{
    [Header("캔버스(각 씬의 것 연결)")]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject exitMessageCanvas;

    [Header("캔버스 위치(World Space)")]
    [SerializeField] private float distanceFromCamera = 2.36f;
    [SerializeField] private float verticalOffset = -0.3f;
    [SerializeField] private float horizontalOffset = 0f;

    [Header("VR 입력 사용")]
    [SerializeField] private bool useVRController = true;

    [Header("연타 방지(초)")]
    [SerializeField] private float pauseDebounce = 0.25f;

    private bool isPaused;
    private Camera cachedMainCamera;
    private float lastPauseTime = -999f;

    // XR 버튼 이전 상태(Down 판정용)
    private bool prevRightPrimary, prevRightSecondary;
    private bool prevLeftPrimary, prevLeftSecondary;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        SafeInitCanvases();
        ResetXRButtonStates();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cachedMainCamera = null;
        isPaused = false;
        Time.timeScale = 1f;

        SafeInitCanvases();
        ResetXRButtonStates();
    }

    // ✅ 오류났던 함수: 이게 반드시 있어야 함
    private void SafeInitCanvases()
    {
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        if (exitMessageCanvas != null) exitMessageCanvas.SetActive(false);

        RepositionCanvas(pauseCanvas, 30000);
        RepositionCanvas(exitMessageCanvas, 30001);
    }

    private void Update()
    {
        if (!useVRController) return;
        if (Time.unscaledTime - lastPauseTime < pauseDebounce) return;

        if (IsAnyPauseButtonDownXR() || IsAnyPauseButtonDownOVR())
        {
            lastPauseTime = Time.unscaledTime;
            TryTogglePause();
        }
    }

    private void TryTogglePause()
    {
        // ExitMessage가 떠있으면 먼저 닫기
        if (exitMessageCanvas != null && exitMessageCanvas.activeSelf)
        {
            HideExitMessage();
            return;
        }

        TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pauseCanvas != null)
        {
            if (isPaused) RepositionCanvas(pauseCanvas, 30000);
            pauseCanvas.SetActive(isPaused);
        }

        if (!isPaused && exitMessageCanvas != null)
            exitMessageCanvas.SetActive(false);

        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void Resume()
    {
        isPaused = false;
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        if (exitMessageCanvas != null) exitMessageCanvas.SetActive(false);
        Time.timeScale = 1f;
    }

    // ----------------------------------------------------
    // Exit Message
    // ----------------------------------------------------
    public void ShowExitMessageFromPause()
    {
        if (exitMessageCanvas == null) return;
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        RepositionCanvas(exitMessageCanvas, 30001);
        exitMessageCanvas.SetActive(true);
    }

    public void ShowExitMessageFromHome()
    {
        if (exitMessageCanvas == null) return;
        if (pauseCanvas != null) pauseCanvas.SetActive(false);
        RepositionCanvas(exitMessageCanvas, 30001);
        exitMessageCanvas.SetActive(true);
    }

    public void HideExitMessage()
    {
        if (exitMessageCanvas != null) exitMessageCanvas.SetActive(false);

        // Pause 중이었다면 PauseCanvas로 복귀
        if (isPaused && pauseCanvas != null)
            pauseCanvas.SetActive(true);
    }

    // ----------------------------------------------------
    // XR 입력
    // ----------------------------------------------------
    private bool IsAnyPauseButtonDownXR()
    {
        bool down = false;

        var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (right.isValid)
        {
            right.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool curA);
            right.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool curB);

            if (curA && !prevRightPrimary) down = true;
            if (curB && !prevRightSecondary) down = true;

            prevRightPrimary = curA;
            prevRightSecondary = curB;
        }

        var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (left.isValid)
        {
            left.TryGetFeatureValue(XRCommonUsages.primaryButton, out bool curX);
            left.TryGetFeatureValue(XRCommonUsages.secondaryButton, out bool curY);

            if (curX && !prevLeftPrimary) down = true;
            if (curY && !prevLeftSecondary) down = true;

            prevLeftPrimary = curX;
            prevLeftSecondary = curY;
        }

        return down;
    }

    private bool IsAnyPauseButtonDownOVR()
    {
#if OCULUS_INTEGRATION
        return OVRInput.GetDown(OVRInput.RawButton.A)
            || OVRInput.GetDown(OVRInput.RawButton.B)
            || OVRInput.GetDown(OVRInput.RawButton.X)
            || OVRInput.GetDown(OVRInput.RawButton.Y);
#else
        return false;
#endif
    }

    private void ResetXRButtonStates()
    {
        prevRightPrimary = prevRightSecondary = false;
        prevLeftPrimary = prevLeftSecondary = false;
    }

    // ----------------------------------------------------
    // 캔버스 위치
    // ----------------------------------------------------
    private Camera GetMainCamera()
    {
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
            if (cachedMainCamera == null)
            {
                var cams = FindObjectsOfType<Camera>();
                if (cams != null && cams.Length > 0) cachedMainCamera = cams[0];
            }
        }
        return cachedMainCamera;
    }

    private void RepositionCanvas(GameObject canvasObj, int sortingOrder)
    {
        if (canvasObj == null) return;

        var cam = GetMainCamera();
        if (cam == null) return;

        var canvas = canvasObj.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.sortingOrder = sortingOrder;

            Transform t = canvasObj.transform;
            t.position =
                cam.transform.position
                + cam.transform.forward * distanceFromCamera
                + cam.transform.up * verticalOffset
                + cam.transform.right * horizontalOffset;

            t.rotation = Quaternion.LookRotation(
                (t.position - cam.transform.position).normalized,
                Vector3.up
            );
        }
    }
}
