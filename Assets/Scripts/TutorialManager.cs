using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Tutorial Steps")]
    public TutorialStep currentStep = TutorialStep.Welcome;
    private int currentStepIndex = 0;

    [Header("UI References")]
    public Canvas tutorialCanvas;
    public Text instructionText;
    public Text stepCounterText;
    public GameObject nextButton;
    public GameObject skipButton;

    [Header("Highlight Objects")]
    public GameObject[] drumHighlights; // 4개 북 하이라이트
    public GameObject arrowGuide; // 화살표 가이드

    [Header("Practice Settings")]
    public GameObject practiceNotePrefab;
    public GameObject obstacclePrefab; // ⭐ Obstacle 프리팹 추가
    public Transform[] spawnPoints;
    public Transform[] targetPoints;
    public float practiceNoteSpeed = 2f;
    public float obstacleSpeed = 3f; // ⭐ 장애물 속도

    [Header("Target Counts")]
    public int requiredHitsPerDrum = 3;
    public int requiredComboHits = 5;
    public int requiredObstacleAvoids = 3; // ⭐ 회피해야 할 장애물 수

    [Header("Scene Management")]
    public string mainGameSceneName = "HandTracking_Main";

    // Progress Tracking
    private int[] drumHitCounts = new int[4];
    private int totalPracticeHits = 0;
    private int obstaclesAvoided = 0; // ⭐ 회피한 장애물 수
    private int obstaclesHit = 0; // ⭐ 맞은 장애물 수
    private bool stepCompleted = false;

    // Tutorial Steps Enum
    public enum TutorialStep
    {
        Welcome,              // 0
        DrumBasics,           // 1
        JudgmentSystem,       // 2
        NotePractice,         // 3
        ObstacleAvoidance,    // 4 ⭐ NEW!
        ComboSystem,          // 5
        FinalPractice,        // 6
        Complete              // 7
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        HideAllHighlights();
        if (arrowGuide != null) arrowGuide.SetActive(false);

        StartStep(TutorialStep.Welcome);
    }

    void Update()
    {
        switch (currentStep)
        {
            case TutorialStep.DrumBasics:
                UpdateDrumBasicsStep();
                break;
            case TutorialStep.NotePractice:
                UpdateNotePracticeStep();
                break;
            case TutorialStep.ObstacleAvoidance: // ⭐ NEW!
                UpdateObstacleAvoidanceStep();
                break;
            case TutorialStep.ComboSystem:
                UpdateComboSystemStep();
                break;
        }
    }

    public void StartStep(TutorialStep step)
    {
        currentStep = step;
        currentStepIndex = (int)step;
        stepCompleted = false;

        HideAllHighlights();

        switch (step)
        {
            case TutorialStep.Welcome:
                ShowWelcomeStep();
                break;
            case TutorialStep.DrumBasics:
                ShowDrumBasicsStep();
                break;
            case TutorialStep.JudgmentSystem:
                ShowJudgmentSystemStep();
                break;
            case TutorialStep.NotePractice:
                ShowNotePracticeStep();
                break;
            case TutorialStep.ObstacleAvoidance: // ⭐ NEW!
                ShowObstacleAvoidanceStep();
                break;
            case TutorialStep.ComboSystem:
                ShowComboSystemStep();
                break;
            case TutorialStep.FinalPractice:
                ShowFinalPracticeStep();
                break;
            case TutorialStep.Complete:
                ShowCompleteStep();
                break;
        }

        UpdateUI();
    }

    void ShowWelcomeStep()
    {
        SetInstruction("SamulVerse에 오신 것을 환영합니다!\n\n" +
                      "거북선과 함께 사물놀이 리듬을 즐겨보세요.\n\n" +
                      "'다음' 버튼을 눌러 시작하세요.");

        if (nextButton != null) nextButton.SetActive(true);
        if (skipButton != null) skipButton.SetActive(true);
    }

    void ShowDrumBasicsStep()
    {
        SetInstruction("VR 컨트롤러를 북채처럼 사용하세요.\n\n" +
                      "각 북을 " + requiredHitsPerDrum + "번씩 쳐보세요!\n\n" +
                      "먼저 '정' 북을 쳐보세요.");

        if (drumHighlights.Length > 0 && drumHighlights[0] != null)
        {
            drumHighlights[0].SetActive(true);
        }

        if (arrowGuide != null)
        {
            arrowGuide.SetActive(true);
        }

        drumHitCounts = new int[4];

        if (nextButton != null) nextButton.SetActive(false);
    }

    void UpdateDrumBasicsStep()
    {
        bool allDrumsCompleted = true;
        for (int i = 0; i < 4; i++)
        {
            if (drumHitCounts[i] < requiredHitsPerDrum)
            {
                allDrumsCompleted = false;

                HideAllHighlights();
                if (drumHighlights[i] != null)
                {
                    drumHighlights[i].SetActive(true);
                }

                string[] drumNames = { "정", "장구", "북", "징" };
                SetInstruction($"{drumNames[i]} 북을 쳐보세요\n\n" +
                              $"진행: {drumHitCounts[i]}/{requiredHitsPerDrum}");

                break;
            }
        }

        if (allDrumsCompleted && !stepCompleted)
        {
            stepCompleted = true;
            HideAllHighlights();
            SetInstruction("훌륭합니다! 모든 북을 성공적으로 쳤습니다!\n\n" +
                          "'다음' 버튼을 눌러 계속하세요.");
            if (nextButton != null) nextButton.SetActive(true);
        }
    }

    void ShowJudgmentSystemStep()
    {
        SetInstruction("판정 시스템 안내\n\n" +
                      "★ Perfect: 완벽한 타이밍! (노란색)\n" +
                      "○ Good: 좋은 타이밍 (파란색)\n" +
                      "✕ Miss: 놓침 (빨간색)\n\n" +
                      "타이밍을 잘 맞춰서 높은 점수를 얻으세요!");

        if (nextButton != null) nextButton.SetActive(true);
    }

    void ShowNotePracticeStep()
    {
        SetInstruction("날아오는 노트를 타이밍에 맞춰 치세요!\n\n" +
                      "연습 노트: 0/" + requiredComboHits);

        totalPracticeHits = 0;

        if (nextButton != null) nextButton.SetActive(false);

        StartCoroutine(SpawnPracticeNotes());
    }

    void UpdateNotePracticeStep()
    {
        SetInstruction("날아오는 노트를 타이밍에 맞춰 치세요!\n\n" +
                      $"진행: {totalPracticeHits}/{requiredComboHits}");

        if (totalPracticeHits >= requiredComboHits && !stepCompleted)
        {
            stepCompleted = true;
            StopAllCoroutines();
            SetInstruction("완벽합니다! 타이밍을 잘 맞추셨네요!\n\n" +
                          "'다음' 버튼을 눌러 계속하세요.");
            if (nextButton != null) nextButton.SetActive(true);
        }
    }

    // ⭐ NEW! 장애물 회피 단계
    void ShowObstacleAvoidanceStep()
    {
        SetInstruction("장애물 회피 훈련!\n\n" +
                      "날아오는 장애물을 고개를 움직여 피하세요!\n\n" +
                      "장애물에 맞으면 콤보가 초기화됩니다.\n\n" +
                      $"회피: 0/{requiredObstacleAvoids}");

        obstaclesAvoided = 0;
        obstaclesHit = 0;

        if (nextButton != null) nextButton.SetActive(false);

        // 장애물 스폰 시작
        StartCoroutine(SpawnPracticeObstacles());
    }

    void UpdateObstacleAvoidanceStep()
    {
        SetInstruction("장애물 회피 훈련!\n\n" +
                      "날아오는 장애물을 고개를 움직여 피하세요!\n\n" +
                      $"회피 성공: {obstaclesAvoided}/{requiredObstacleAvoids}\n" +
                      $"맞음: {obstaclesHit}회");

        if (obstaclesAvoided >= requiredObstacleAvoids && !stepCompleted)
        {
            stepCompleted = true;
            StopAllCoroutines();
            SetInstruction("훌륭합니다! 장애물을 잘 피하셨네요!\n\n" +
                          "실전에서도 장애물을 조심하세요.\n\n" +
                          "'다음' 버튼을 눌러 계속하세요.");
            if (nextButton != null) nextButton.SetActive(true);
        }
    }

    IEnumerator SpawnPracticeObstacles()
    {
        yield return new WaitForSeconds(2f);

        int spawnedCount = 0;
        int maxObstacles = requiredObstacleAvoids + 2; // 여유있게 더 생성

        while (obstaclesAvoided < requiredObstacleAvoids && spawnedCount < maxObstacles)
        {
            // 랜덤 위치에서 장애물 생성
            int randomDrum = Random.Range(0, 4);
            SpawnPracticeObstacle(randomDrum);
            spawnedCount++;

            yield return new WaitForSeconds(3f); // 천천히 생성
        }
    }

    void SpawnPracticeObstacle(int drumIndex)
    {
        if (obstacclePrefab == null || spawnPoints.Length <= drumIndex) return;

        Vector3 obstaclePos = spawnPoints[drumIndex].position;
        obstaclePos.y += 0.5f; // 약간 위로

        // 플레이어 카메라 찾기
        Transform playerCamera = null;
        GameObject cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
        if (cameraRig != null)
        {
            Transform trackingSpace = cameraRig.transform.Find("TrackingSpace");
            if (trackingSpace != null)
            {
                playerCamera = trackingSpace.Find("CenterEyeAnchor");
            }
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }

        if (playerCamera == null) return;

        // 타겟 위치 계산
        Vector3 targetPosition = playerCamera.position;

        switch (drumIndex)
        {
            case 0:
                targetPosition += playerCamera.right * -0.8f;
                targetPosition += Vector3.up * 0.3f;
                break;
            case 1:
                targetPosition += playerCamera.right * -0.5f;
                targetPosition += Vector3.up * -0.1f;
                break;
            case 2:
                targetPosition += playerCamera.right * 0.5f;
                targetPosition += Vector3.up * -0.1f;
                break;
            case 3:
                targetPosition += playerCamera.right * 0.8f;
                targetPosition += Vector3.up * 0.3f;
                break;
        }

        GameObject obstacleObj = Instantiate(
            obstacclePrefab,
            obstaclePos,
            Quaternion.identity
        );

        Obstacle obstacle = obstacleObj.GetComponent<Obstacle>();
        if (obstacle != null)
        {
            obstacle.speed = obstacleSpeed; // 튜토리얼용 느린 속도
            obstacle.targetPosition = targetPosition;
        }

        Debug.Log($"Tutorial obstacle spawned at drum {drumIndex}");
    }

    // ⭐ 장애물 회피 성공 시 호출 (Obstacle.cs에서 호출)
    public void OnObstacleAvoidedInTutorial()
    {
        if (currentStep == TutorialStep.ObstacleAvoidance)
        {
            obstaclesAvoided++;
            Debug.Log($"Obstacles avoided: {obstaclesAvoided}");
        }
    }

    // ⭐ 장애물 충돌 시 호출 (Obstacle.cs에서 호출)
    public void OnObstacleHitInTutorial()
    {
        if (currentStep == TutorialStep.ObstacleAvoidance)
        {
            obstaclesHit++;
            Debug.Log($"Obstacles hit: {obstaclesHit}");
        }
    }

    void ShowComboSystemStep()
    {
        SetInstruction("콤보 시스템\n\n" +
                      "연속으로 노트를 치면 콤보가 올라갑니다!\n" +
                      "콤보가 높을수록 점수도 높아지고,\n" +
                      "거북선의 노 젓는 속도도 빨라집니다.\n\n" +
                      "Miss나 장애물에 맞으면 콤보가 초기화됩니다.");

        if (nextButton != null) nextButton.SetActive(true);
    }

    void UpdateComboSystemStep()
    {
        // 콤보 관련 로직
    }

    void ShowFinalPracticeStep()
    {
        SetInstruction("마지막 연습!\n\n" +
                      "지금까지 배운 것을 활용해서\n" +
                      "짧은 패턴을 연주해보세요!");

        if (nextButton != null) nextButton.SetActive(false);

        StartCoroutine(SpawnFinalPracticePattern());
    }

    void ShowCompleteStep()
    {
        SetInstruction("튜토리얼 완료!\n\n" +
                      "이제 본 게임을 즐길 준비가 되었습니다.\n\n" +
                      "'게임 시작' 버튼을 눌러주세요!");

        if (nextButton != null)
        {
            nextButton.SetActive(true);
            Text buttonText = nextButton.GetComponentInChildren<Text>();
            if (buttonText != null) buttonText.text = "게임 시작";
        }
    }

    IEnumerator SpawnPracticeNotes()
    {
        yield return new WaitForSeconds(2f);

        while (totalPracticeHits < requiredComboHits)
        {
            int randomDrum = Random.Range(0, 4);
            SpawnPracticeNote(randomDrum);

            yield return new WaitForSeconds(2f);
        }
    }

    void SpawnPracticeNote(int drumIndex)
    {
        if (practiceNotePrefab == null || spawnPoints.Length <= drumIndex) return;

        GameObject noteObj = Instantiate(
            practiceNotePrefab,
            spawnPoints[drumIndex].position,
            Quaternion.identity
        );

        Note note = noteObj.GetComponent<Note>();
        if (note != null)
        {
            note.speed = practiceNoteSpeed;
            note.targetPosition = targetPoints[drumIndex].position;

            string[] drumTypes = { "Jung", "Jang", "Book", "Jing" };
            note.drumType = drumTypes[drumIndex];
        }
    }

    IEnumerator SpawnFinalPracticePattern()
    {
        yield return new WaitForSeconds(1f);

        // 간단한 패턴: 노트 + 장애물 혼합
        int[] notePattern = { 0, 1, 2, 3, 0, 1, 2, 3 };

        for (int i = 0; i < notePattern.Length; i++)
        {
            SpawnPracticeNote(notePattern[i]);

            // 중간에 장애물 2개 추가
            if (i == 3 || i == 6)
            {
                yield return new WaitForSeconds(1f);
                SpawnPracticeObstacle(Random.Range(0, 4));
            }

            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(3f);

        stepCompleted = true;
        SetInstruction("훌륭합니다! 모든 과정을 완료하셨습니다!\n\n" +
                      "'다음' 버튼을 눌러주세요.");
        if (nextButton != null) nextButton.SetActive(true);
    }

    public void OnDrumHitInTutorial(int drumIndex)
    {
        if (currentStep == TutorialStep.DrumBasics)
        {
            drumHitCounts[drumIndex]++;
            Debug.Log($"Drum {drumIndex} hit count: {drumHitCounts[drumIndex]}");
        }
    }

    public void OnNoteHitInTutorial(bool isPerfect)
    {
        if (currentStep == TutorialStep.NotePractice || currentStep == TutorialStep.FinalPractice)
        {
            totalPracticeHits++;
            Debug.Log($"Practice hits: {totalPracticeHits}");
        }
    }

    public void NextStep()
    {
        if (currentStepIndex < 7) // 7단계까지 (0~7)
        {
            StartStep((TutorialStep)(currentStepIndex + 1));
        }
        else
        {
            LoadMainGame();
        }
    }

    public void SkipTutorial()
    {
        LoadMainGame();
    }

    void LoadMainGame()
    {
        Debug.Log("Loading main game...");
        SceneManager.LoadScene("Main");
    }

    void SetInstruction(string text)
    {
        if (instructionText != null)
        {
            instructionText.text = text;
        }
    }

    void UpdateUI()
    {
        if (stepCounterText != null)
        {
            stepCounterText.text = $"단계 {currentStepIndex + 1}/8"; // 8단계로 변경
        }
    }

    void HideAllHighlights()
    {
        foreach (GameObject highlight in drumHighlights)
        {
            if (highlight != null)
            {
                highlight.SetActive(false);
            }
        }
    }
}
