using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("일시정지 캔버스")]
    [SerializeField] private GameObject pauseCanvas;
    [SerializeField] private GameObject exitMessageCanvas;

    [Header("캔버스 위치 조정")]
    [SerializeField] private float distanceFromCamera = 2.36f;
    [SerializeField] private float verticalOffset = -0.3f;
    [SerializeField] private float horizontalOffset = 0f;

    [Header("입력 설정")]
    [SerializeField] private bool useKeyboard = true;
    [SerializeField] private bool useVRController = false;

    private bool isPaused = false;
    private Camera cachedMainCamera;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (pauseCanvas != null)
            {
                DontDestroyOnLoad(pauseCanvas);
            }

            if (exitMessageCanvas != null)
            {
                DontDestroyOnLoad(exitMessageCanvas);
            }
        }
        else
        {
            Destroy(gameObject);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        cachedMainCamera = null;
        RepositionCanvas();
        RepositionExitMessageCanvas();
    }

    void Start()
    {
        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        if (exitMessageCanvas != null)
        {
            exitMessageCanvas.SetActive(false);
        }

        RepositionCanvas();
        RepositionExitMessageCanvas();
    }

    void Update()
    {
        if (useKeyboard && Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (useVRController)
        {
            CheckVRInput();
        }
    }

    private void CheckVRInput()
    {
        // VR 컨트롤러 입력 (Meta XR SDK 설치 후 구현)
        /*
        // Menu 버튼으로 Pause (더 직관적)
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch) ||
            OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.RTouch))
        {
            TogglePause();
        }
        
        // 또는 A/B/X/Y 버튼
        if (OVRInput.GetDown(OVRInput.Button.One) ||   // A
            OVRInput.GetDown(OVRInput.Button.Two) ||   // B
            OVRInput.GetDown(OVRInput.Button.Three) || // X
            OVRInput.GetDown(OVRInput.Button.Four))    // Y
        {
            TogglePause();
        }
        */
    }

    private Camera GetMainCamera()
    {
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;

            if (cachedMainCamera == null)
            {
                Camera[] cameras = FindObjectsOfType<Camera>();
                if (cameras.Length > 0)
                {
                    cachedMainCamera = cameras[0];
                }
            }
        }

        return cachedMainCamera;
    }

    private void RepositionCanvas()
    {
        if (pauseCanvas == null) return;

        Camera mainCamera = GetMainCamera();
        if (mainCamera == null) return;

        Canvas canvas = pauseCanvas.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.sortingOrder = 30000;

            Transform canvasTransform = pauseCanvas.transform;

            Vector3 forward = mainCamera.transform.forward * distanceFromCamera;
            Vector3 vertical = mainCamera.transform.up * verticalOffset;
            Vector3 horizontal = mainCamera.transform.right * horizontalOffset;

            canvasTransform.position = mainCamera.transform.position + forward + vertical + horizontal;
            canvasTransform.rotation = mainCamera.transform.rotation;
        }
    }

    private void RepositionExitMessageCanvas()
    {
        if (exitMessageCanvas == null) return;

        Camera mainCamera = GetMainCamera();
        if (mainCamera == null) return;

        Canvas canvas = exitMessageCanvas.GetComponent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.sortingOrder = 30001;

            Transform canvasTransform = exitMessageCanvas.transform;

            Vector3 forward = mainCamera.transform.forward * distanceFromCamera;
            Vector3 vertical = mainCamera.transform.up * verticalOffset;
            Vector3 horizontal = mainCamera.transform.right * horizontalOffset;

            canvasTransform.position = mainCamera.transform.position + forward + vertical + horizontal;
            canvasTransform.rotation = mainCamera.transform.rotation;
        }
    }

    public GameObject GetExitMessageCanvas()
    {
        return exitMessageCanvas;
    }

    public GameObject GetPauseCanvas()
    {
        return pauseCanvas;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (pauseCanvas != null)
        {
            if (isPaused)
            {
                cachedMainCamera = null;
                RepositionCanvas();
            }
            pauseCanvas.SetActive(isPaused);
        }

        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void Resume()
    {
        isPaused = false;

        if (pauseCanvas != null)
        {
            pauseCanvas.SetActive(false);
        }

        if (exitMessageCanvas != null)
        {
            exitMessageCanvas.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void Pause()
    {
        isPaused = true;

        if (pauseCanvas != null)
        {
            cachedMainCamera = null;
            RepositionCanvas();
            pauseCanvas.SetActive(true);
        }

        Time.timeScale = 0f;
    }
}