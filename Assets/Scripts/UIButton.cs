using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UIButton : MonoBehaviour
{
    [Header("버튼 설정")]
    [SerializeField] private Button button;

    [Header("동작 타입")]
    [SerializeField] private ButtonActionType actionType;

    [Header("씬 이름 (씬 로드 시)")]
    [SerializeField] private string targetSceneName;

    [Header("캔버스 (캔버스 전환 시)")]
    [SerializeField] private GameObject targetCanvas;
    [SerializeField] private GameObject currentCanvas;

    [Header("일시정지 캔버스 (게임 재개 시)")]
    [SerializeField] private GameObject pauseCanvas;

    [Header("메인메뉴 씬 이름")]
    [SerializeField] private string mainMenuSceneName = "Opening";

    public enum ButtonActionType
    {
        LoadScene,
        SwitchCanvas,
        RestartCurrentScene,
        ShowExitMessage,
        QuitGame,
        ResumeGame,
        CloseCurrentCanvas,
        ReturnToMainMenu
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
    }

    private void OnEnable() => RegisterListener();
    private void Start() => RegisterListener();

    private void RegisterListener()
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        Debug.Log($"━━━ {gameObject.name} 클릭! ({actionType}) ━━━");

        // 효과음
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClickSound();

        switch (actionType)
        {
            case ButtonActionType.LoadScene:
                LoadSceneSafe(string.IsNullOrEmpty(targetSceneName) ? mainMenuSceneName : targetSceneName);
                break;

            case ButtonActionType.SwitchCanvas:
                SwitchCanvasSafe();
                break;

            case ButtonActionType.RestartCurrentScene:
                Time.timeScale = 1f;
                PauseManager.Instance?.Resume();
                LoadSceneSafe(SceneManager.GetActiveScene().name);
                break;

            case ButtonActionType.ShowExitMessage:
                FindCanvasReferencesForExitMessage();
                if (currentCanvas != null) currentCanvas.SetActive(false);
                if (targetCanvas != null) targetCanvas.SetActive(true);
                break;

            case ButtonActionType.QuitGame:
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;

            case ButtonActionType.ResumeGame:
                Time.timeScale = 1f;

                // pauseCanvas가 인스펙터에 없으면 PauseManager에서 받아오기
                if (pauseCanvas == null)
                    pauseCanvas = PauseManager.Instance != null ? PauseManager.Instance.GetPauseCanvas() : null;

                if (pauseCanvas != null)
                    pauseCanvas.SetActive(false);

                PauseManager.Instance?.Resume();
                break;

            case ButtonActionType.CloseCurrentCanvas:
                // ExitMessage 닫고 Pause로 복귀
                if (currentCanvas == null && PauseManager.Instance != null)
                    currentCanvas = PauseManager.Instance.GetExitMessageCanvas();

                if (pauseCanvas == null && PauseManager.Instance != null)
                    pauseCanvas = PauseManager.Instance.GetPauseCanvas();

                if (currentCanvas != null) currentCanvas.SetActive(false);
                if (pauseCanvas != null) pauseCanvas.SetActive(true);
                break;

            case ButtonActionType.ReturnToMainMenu:
                Time.timeScale = 1f;
                PauseManager.Instance?.Resume();

                // 미리보기 정리 (함수 없을 수도 있으니 안전하게)
                TryStopAllPreviews();

                LoadSceneSafe(mainMenuSceneName); // ✅ Opening
                break;
        }
    }

    // ===== 안전 로드 =====
    private void LoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("❌ LoadSceneSafe: sceneName이 비어 있습니다.");
            return;
        }

        // LoadingManager가 있으면 그걸 사용, 없으면 SceneManager로 바로 로드
        if (LoadingManager.Instance != null)
            LoadingManager.Instance.LoadSceneWithLoading(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    // ===== ExitMessage 캔버스 참조 자동 채우기 =====
    private void FindCanvasReferencesForExitMessage()
    {
        if (PauseManager.Instance == null) return;

        if (targetCanvas == null)
            targetCanvas = PauseManager.Instance.GetExitMessageCanvas();

        if (currentCanvas == null)
            currentCanvas = PauseManager.Instance.GetPauseCanvas();
    }

    // ===== 캔버스 전환(Null 안전) =====
    private void SwitchCanvasSafe()
    {
        if (currentCanvas == null || targetCanvas == null)
        {
            Debug.LogWarning("⚠ SwitchCanvas: currentCanvas 또는 targetCanvas가 비어 있습니다. 인스펙터 연결 확인해주세요.");
            return;
        }

        currentCanvas.SetActive(false);
        targetCanvas.SetActive(true);
    }

    // ===== TrackSelectorManager StopAllPreviews가 버전에 따라 없을 수 있음 =====
    private void TryStopAllPreviews()
    {
        if (TrackSelectorManager.Instance == null) return;

        // StopAllPreviews()가 있는 버전이면 호출
        var m = TrackSelectorManager.Instance.GetType().GetMethod("StopAllPreviews");
        if (m != null)
        {
            m.Invoke(TrackSelectorManager.Instance, null);
            return;
        }

        // 없으면 최소한 현재 재생만 정리할 방법이 없어도 크래시만 막음
        Debug.Log("ℹ TrackSelectorManager에 StopAllPreviews()가 없어 호출 생략했습니다.");
    }
}
