using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    public static LoadingManager Instance { get; private set; }

    [Header("로딩 캔버스 (수동 연결 또는 자동 검색)")]
    [SerializeField] private GameObject loadingCanvasObject;
    [SerializeField] private RectTransform loadingIcon;
    [SerializeField] private Text loadingText;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 230f;
    [SerializeField] private float minLoadingTime = 2f;

    [Header("캔버스 위치 조정")]
    [SerializeField] private float distanceFromCamera = 2.36f;
    [SerializeField] private float verticalOffset = 0f;
    [SerializeField] private float horizontalOffset = 0f;

    [Header("회전 고정(기울어짐 방지)")]
    [Tooltip("VR에서 카메라가 살짝 기울어져도(roll/pitch) 로딩 캔버스가 정면으로 뜨게 합니다.")]
    [SerializeField] private bool lockToUprightYawOnly = true;

    // WorldSpace 캔버스가 XR카메라 뒤로 밀리지 않도록 약간 앞으로 당기는 값(필요 시)
    [Tooltip("캔버스가 너무 가까워/멀어 보이면 distanceFromCamera를 조정하고, 미세 조정은 여기로.")]
    [SerializeField] private float extraForwardOffset = 0f;

    private Coroutine loadRoutine;
    private Camera cachedMainCamera;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (loadingCanvasObject != null)
                DontDestroyOnLoad(loadingCanvasObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 바뀌면 카메라가 바뀌는 경우가 많아서 재배치
        cachedMainCamera = null;
        RepositionCanvas();
    }

    private void Update()
    {
        if (loadingCanvasObject != null && loadingCanvasObject.activeSelf && loadingIcon != null)
        {
            loadingIcon.Rotate(0, 0, -rotationSpeed * Time.unscaledDeltaTime);
        }
    }

    // ✅ 외부에서 호출하는 함수
    public void LoadSceneWithLoading(string sceneName)
    {
        // 1) 씬이 빌드세팅에 없으면 여기서 막고 로그를 남김
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"❌ 씬을 로드할 수 없습니다: '{sceneName}'\n" +
                           $"Build Settings > Scenes In Build에 추가되어 있는지 확인해 주세요.");
            return;
        }

        // 2) 중복 로딩 방지
        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }

        loadRoutine = StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        Debug.Log($"━━━ 로딩 시작: {sceneName} ━━━");

        // GameManager 저장 타이밍 여유
        yield return new WaitForSecondsRealtime(0.1f);

        // 로딩 UI 찾기/보강
        FindLoadingElementsSafe();

        if (loadingCanvasObject == null)
        {
            Debug.LogError("❌ 로딩 캔버스(Canvas - Loading)를 찾을 수 없습니다! (씬에 존재하는지 확인)");
            loadRoutine = null;
            yield break;
        }

        // 캔버스 정렬/재배치/활성화
        ApplyCanvasSorting();
        RepositionCanvas();

        loadingCanvasObject.SetActive(true);

        if (loadingText != null)
            loadingText.text = "로딩 중...";

        // 최소 로딩 시간
        float elapsed = 0f;
        while (elapsed < minLoadingTime)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.Log("✓ 최소 로딩 시간 경과, 씬 로드 시작");

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError("❌ LoadSceneAsync가 실패했습니다. sceneName을 다시 확인해 주세요.");
            if (loadingCanvasObject != null) loadingCanvasObject.SetActive(false);
            loadRoutine = null;
            yield break;
        }

        // 로딩 완료까지 대기
        while (!asyncLoad.isDone)
            yield return null;

        // 로딩 UI 끄기
        if (loadingCanvasObject != null)
            loadingCanvasObject.SetActive(false);

        Debug.Log("✓ 로딩 완료");
        loadRoutine = null;
    }

    // ✅ 로딩 UI 자동 찾기(안전 강화)
    private void FindLoadingElementsSafe()
    {
        // 1) 캔버스 찾기
        if (loadingCanvasObject == null)
        {
            loadingCanvasObject = GameObject.Find("Canvas - Loading");
            if (loadingCanvasObject != null)
                DontDestroyOnLoad(loadingCanvasObject);
        }

        if (loadingCanvasObject == null) return;

        // 2) 아이콘 찾기
        if (loadingIcon == null)
        {
            Transform t = loadingCanvasObject.transform.Find("LoadingSpinner");
            if (t != null) loadingIcon = t.GetComponent<RectTransform>();
        }

        // 3) 텍스트 찾기
        if (loadingText == null)
        {
            Transform t = loadingCanvasObject.transform.Find("LoadingText");
            if (t != null) loadingText = t.GetComponent<Text>();
        }
    }

    private void ApplyCanvasSorting()
    {
        if (loadingCanvasObject == null) return;

        Canvas canvas = loadingCanvasObject.GetComponent<Canvas>();
        if (canvas != null)
            canvas.sortingOrder = 29999;
    }

    private Camera GetMainCameraSafe()
    {
        if (cachedMainCamera != null) return cachedMainCamera;

        // 1) 일반적으로는 Camera.main
        cachedMainCamera = Camera.main;
        if (cachedMainCamera != null) return cachedMainCamera;

        // 2) 없으면 씬의 첫 카메라라도 잡기(보험)
        var cams = FindObjectsOfType<Camera>(true);
        if (cams != null && cams.Length > 0)
            cachedMainCamera = cams[0];

        return cachedMainCamera;
    }

    private void RepositionCanvas()
    {
        if (loadingCanvasObject == null) return;

        Camera mainCamera = GetMainCameraSafe();
        if (mainCamera == null) return;

        Canvas canvas = loadingCanvasObject.GetComponent<Canvas>();
        if (canvas == null) return;

        // WorldSpace일 때만 위치/회전 보정
        if (canvas.renderMode == RenderMode.WorldSpace)
        {
            canvas.sortingOrder = 29999;

            Transform tr = loadingCanvasObject.transform;

            // 위치: 카메라 정면 거리 + 오프셋
            Vector3 forwardDir = mainCamera.transform.forward;
            Vector3 upDir = mainCamera.transform.up;
            Vector3 rightDir = mainCamera.transform.right;

            tr.position =
                mainCamera.transform.position
                + forwardDir * (distanceFromCamera + extraForwardOffset)
                + upDir * verticalOffset
                + rightDir * horizontalOffset;

            // ✅ 핵심: 기울어짐(roll/pitch) 제거하고 "정면"으로 보이게
            if (lockToUprightYawOnly)
            {
                Vector3 flatForward = mainCamera.transform.forward;
                flatForward.y = 0f;

                if (flatForward.sqrMagnitude < 0.0001f)
                    flatForward = Vector3.forward;

                tr.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            }
            else
            {
                // 기존 방식(카메라 회전 그대로 따라감)
                tr.rotation = mainCamera.transform.rotation;
            }
        }
        else
        {
            // ScreenSpace일 때는 위치 보정 불필요
            canvas.sortingOrder = 29999;
        }
    }

    // 캔버스 전환용 로딩(그대로 유지)
    public void ShowLoadingForCanvasTransition(System.Action onComplete)
    {
        StartCoroutine(CanvasTransitionCoroutine(onComplete));
    }

    private IEnumerator CanvasTransitionCoroutine(System.Action onComplete)
    {
        FindLoadingElementsSafe();

        if (loadingCanvasObject != null)
        {
            ApplyCanvasSorting();
            RepositionCanvas();
            loadingCanvasObject.SetActive(true);
        }

        if (loadingText != null)
            loadingText.text = "로딩 중...";

        yield return new WaitForSecondsRealtime(minLoadingTime);

        onComplete?.Invoke();

        if (loadingCanvasObject != null)
            loadingCanvasObject.SetActive(false);
    }
}
