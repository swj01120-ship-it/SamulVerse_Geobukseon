using UnityEngine;

[DisallowMultipleComponent]
public class VRRayPointerVisibilityGate : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private VRRayPointer_VisualOnly rayVisual;

    [Header("UI Roots (MainGame)")]
    [Tooltip("결과 UI 루트(ScrollResultCanvas)")]
    [SerializeField] private GameObject scrollResultCanvas;

    [Tooltip("PauseManager가 켜는 Pause 캔버스(예: Canvas - Pause_esc)")]
    [SerializeField] private GameObject pauseCanvas;

    [Tooltip("PauseManager가 켜는 ExitMessage 캔버스")]
    [SerializeField] private GameObject exitMessageCanvas;

    [Tooltip("로딩 캔버스(있으면 넣기)")]
    [SerializeField] private GameObject loadingCanvas;

    [Header("Buttons")]
    [SerializeField] private bool showOnABXYDown = true;

    [Header("MainGame Policy")]
    [SerializeField] private bool startHidden = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private bool _rayOn;

    private void Awake()
    {
        if (rayVisual == null)
            rayVisual = GetComponent<VRRayPointer_VisualOnly>();

        if (rayVisual == null)
        {
            Debug.LogError("[VRRayPointerVisibilityGate] VRRayPointer_VisualOnly not found on this object.");
            enabled = false;
            return;
        }

        // ✅ 자동 연결(가능한 만큼)
        // PauseManager가 씬/돈디스트로이에 있어도 Find는 잡힘
        var pm = FindObjectOfType<PauseManager>(true);
        if (pm != null)
        {
            // PauseManager에 public/protected 필드명이 다를 수 있어서
            // 여기서는 "자동"을 강제하지 않고, 인스펙터 연결을 우선으로 둠.
            // (자동 연결까지 확실히 하려면 PauseManager 코드가 필요)
        }
    }

    private void OnEnable()
    {
        SetRay(!startHidden);
    }

    private void Update()
    {
        // 1) 결과창이 켜지면 ON
        if (IsActive(scrollResultCanvas))
        {
            SetRay(true);
            return;
        }

        // 2) Pause/Exit/Loading 중 하나라도 켜지면 ON
        if (IsActive(pauseCanvas) || IsActive(exitMessageCanvas) || IsActive(loadingCanvas))
        {
            SetRay(true);
            return;
        }

        // 3) A/B/X/Y 누른 순간(프레임) ON
        if (showOnABXYDown && IsAnyABXYDown())
        {
            SetRay(true);
            return;
        }

        // 4) 그 외는 OFF  (Pause 닫히면 여기로 떨어져서 자동 OFF)
        SetRay(false);
    }

    private bool IsActive(GameObject go)
    {
        return go != null && go.activeInHierarchy;
    }

    private bool IsAnyABXYDown()
    {
#if OCULUS_INTEGRATION
        return OVRInput.GetDown(OVRInput.RawButton.A) ||
               OVRInput.GetDown(OVRInput.RawButton.B) ||
               OVRInput.GetDown(OVRInput.RawButton.X) ||
               OVRInput.GetDown(OVRInput.RawButton.Y);
#else
        // Oculus Integration 심볼이 없으면 컴파일 에러 방지용
        return false;
#endif
    }

    private void SetRay(bool on)
    {
        if (_rayOn == on) return;

        _rayOn = on;
        rayVisual.SetVisible(on);

        if (debugLog)
            Debug.Log($"[VRRayPointerVisibilityGate] Ray {(on ? "ON" : "OFF")} ({name})");
    }
}
