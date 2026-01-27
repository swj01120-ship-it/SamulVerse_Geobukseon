using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AppUIRouter : MonoBehaviour
{
    [Header("Scene Names")]
    [SerializeField] private string homeSceneName = "Home";
    [SerializeField] private string mainGameSceneName = "MainGame";
    [SerializeField] private string tutorialSceneName = "Tutorial";

    [Header("Auto Find (scene마다 재탐색)")]
    [SerializeField] private PauseManager pauseManager;
    [SerializeField] private LoadingManager loadingManager;

    private void Awake()
    {
        // ✅ 씬마다 1개씩 쓰는 버전: DontDestroy/Singleton 제거
        SceneManager.sceneLoaded += OnSceneLoaded;

        FindManagersInScene();

        // 안전: 시작 씬에서 시간 멈춰있던 경우 복구
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindManagersInScene();

        // 씬 넘어갈 때 timeScale이 0으로 남아있으면 모든 UI/입력이 꼬임
        Time.timeScale = 1f;
    }

    private void FindManagersInScene()
    {
        // ✅ "항상 현재 씬의 것"으로 재할당 (비활성 포함)
        pauseManager = FindObjectOfType<PauseManager>(true);
        loadingManager = FindObjectOfType<LoadingManager>(true);
    }

    // --------------------------------------------------
    // Click SFX
    // --------------------------------------------------
    private void PlayClick()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayClickSound();
    }

    // ==================================================
    // Home - WindowBottom 버튼
    // ==================================================
    public void Home_Start()
    {
        PlayClick();
        LoadSceneWithLoading(mainGameSceneName);
    }

    public void Home_Tutorial()
    {
        PlayClick();
        LoadSceneWithLoading(tutorialSceneName);
    }

    public void Home_Exit_OpenExitMessage()
    {
        PlayClick();
        FindManagersInScene();
        pauseManager?.ShowExitMessageFromHome();
    }

    // ==================================================
    // PauseCanvas 버튼 (모든 씬 공통)
    // ==================================================
    public void PauseContinue()
    {
        PlayClick();
        FindManagersInScene();
        Time.timeScale = 1f;
        pauseManager?.Resume();
    }

    public void PauseRestart()
    {
        PlayClick();
        string cur = SceneManager.GetActiveScene().name;

        // MainGame이면 로딩 거쳐 리로드, 그 외는 Continue
        if (cur == mainGameSceneName)
        {
            FindManagersInScene();
            Time.timeScale = 1f;
            pauseManager?.Resume();
            LoadSceneWithLoading(mainGameSceneName);
        }
        else
        {
            PauseContinue();
        }
    }

    public void PauseGoHome()
    {
        PlayClick();
        string cur = SceneManager.GetActiveScene().name;

        // Home 씬에서 "첫화면"은 씬이동이 아니라 Continue로 처리
        if (cur == homeSceneName)
        {
            PauseContinue();
            return;
        }

        FindManagersInScene();
        Time.timeScale = 1f;
        pauseManager?.Resume();
        LoadSceneWithLoading(homeSceneName);
    }

    public void PauseExit_OpenExitMessage()
    {
        PlayClick();
        FindManagersInScene();
        pauseManager?.ShowExitMessageFromPause();
    }

    // ==================================================
    // ExitMessage 버튼 (모든 씬 공통)
    // ==================================================
    public void ExitCancel()
    {
        PlayClick();
        FindManagersInScene();
        pauseManager?.HideExitMessage();
    }

    public void ExitYes_Quit()
    {
        PlayClick();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ==================================================
    // Internal
    // ==================================================
    private void LoadSceneWithLoading(string sceneName)
    {
        FindManagersInScene();

        // ✅ 무조건 로딩 캔버스 거쳐서 이동
        if (loadingManager != null)
        {
            loadingManager.LoadSceneWithLoading(sceneName);
            return;
        }

        // 보험: 로딩매니저가 없으면 그냥 로드
        SceneManager.LoadScene(sceneName);
    }
}
