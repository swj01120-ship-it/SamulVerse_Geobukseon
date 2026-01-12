using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("입력 모드 선택")]
    [Tooltip("Mouse: PC 마우스 입력 / VR: VR 컨트롤러 입력")]
    [SerializeField] private InputMode currentInputMode = InputMode.Mouse;

    [Header("VR 컨트롤러 설정")]
    [Tooltip("왼손 컨트롤러 사용")]
    [SerializeField] private bool useLeftController = true;
    [Tooltip("오른손 컨트롤러 사용")]
    [SerializeField] private bool useRightController = true;

    [Header("디버그")]
    [SerializeField] private bool showDebugLogs = true;

    public enum InputMode
    {
        Mouse,  // PC 마우스
        VR      // VR 컨트롤러
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("✓ InputManager 초기화 완료");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (currentInputMode == InputMode.Mouse)
        {
            // 마우스 입력은 Unity 기본 EventSystem이 처리
        }
        else if (currentInputMode == InputMode.VR)
        {
            HandleVRInput();
        }
    }

    private void HandleVRInput()
    {
        // VR 입력은 나중에 OVR SDK 설치 후 구현
        // 지금은 비워둠
    }

    /// <summary>
    /// 입력 모드 변경 (Inspector나 코드에서 호출 가능)
    /// </summary>
    public void SetInputMode(InputMode mode)
    {
        currentInputMode = mode;

        if (showDebugLogs)
        {
            Debug.Log($"━━━ 입력 모드 변경: {mode} ━━━");
        }
    }

    /// <summary>
    /// 현재 입력 모드 반환
    /// </summary>
    public InputMode GetInputMode()
    {
        return currentInputMode;
    }

    /// <summary>
    /// 현재 VR 모드인지 확인
    /// </summary>
    public bool IsVRMode()
    {
        return currentInputMode == InputMode.VR;
    }
}