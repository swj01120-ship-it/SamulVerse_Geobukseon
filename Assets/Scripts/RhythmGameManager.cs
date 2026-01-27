using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RhythmGameManager : MonoBehaviour
{
    private bool gameStarted = false;
    public static RhythmGameManager Instance;

    [Header("Auto Start (중요)")]
    [Tooltip("MainGameAutoStartController.OnSongStart를 받으면 자동으로 게임 시작 처리")]
    public bool autoStartOnSongStart = true;

    [Tooltip("음악이 재생되기 시작하면 자동으로 게임 시작 처리(이벤트 못 받을 때 대비)")]
    public bool autoStartWhenMusicPlays = true;

    [Tooltip("디버그용: gameStarted=false여도 점수 반영(테스트 끝나면 꺼두기)")]
    public bool allowScoreWhenNotStarted = false;

    [Header("Game UI")]
    public Text scoreText;
    public Text comboText;
    public Text accuracyText;

    [Header("Result UI (Legacy - Fallback)")]
    public GameObject resultPanel;
    public Text titleText;
    public Text resultScoreText;
    public Text maxComboText;
    public Text perfectText;
    public Text goodText;
    public Text missText;
    public Text resultAccuracyText;
    public Text rankText;

    [Header("New Scroll Result UI (TMP)")]
    [Tooltip("새 결과 연출 스크립트(ScrollResultManager)를 여기에 연결")]
    public ScrollResultManager scrollResultManager;

    [Tooltip("새 ResultCanvas 루트 오브젝트(통째로 SetActive ON/OFF 하고 싶을 때)")]
    public GameObject scrollResultCanvasRoot;

    [Header("Game Stats")]
    public int score = 0;
    public int combo = 0;
    public int maxCombo = 0;

    [Header("Hit Stats")]
    public int perfectHits = 0;
    public int goodHits = 0;
    public int missHits = 0;

    [Header("Score Settings")]
    public int perfectScore = 100;
    public int goodScore = 50;
    public float comboMultiplierRate = 0.1f;

    [Header("Combo System")]
    public ComboSystem comboSystem;

    [Header("Animation (Legacy ResultPanel)")]
    public float fadeInDuration = 0.5f;
    public float scaleUpDuration = 0.3f;

    [Header("Game End Settings")]
    public float delayBeforeResults = 1f;
    private bool isGameEnded = false;

    private MusicManager musicManager;
    private BeatMapSpawner beatMapSpawner;

    private bool _loggedNotStartedOnce = false;

    public void NotifyGameStarted()
    {
        if (gameStarted) return;

        gameStarted = true;
        Debug.Log("[RhythmGameManager] ✅ GameStarted = true (NotifyGameStarted)");

        // (추천) 결과 UI들은 시작/재시작 시 숨김 보장
        if (scrollResultCanvasRoot != null)
            scrollResultCanvasRoot.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        // ScrollResultManager가 연결되어 있으면 내부 상태 리셋
        if (scrollResultManager != null)
            scrollResultManager.ResetAll();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void OnEnable()
    {
        if (autoStartOnSongStart)
        {
            MainGameAutoStartController.OnSongStart -= HandleSongStart;
            MainGameAutoStartController.OnSongStart += HandleSongStart;
        }
    }

    private void OnDisable()
    {
        if (autoStartOnSongStart)
        {
            MainGameAutoStartController.OnSongStart -= HandleSongStart;
        }
    }

    private void HandleSongStart()
    {
        NotifyGameStarted();
    }

    private void Start()
    {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (scrollResultCanvasRoot != null) scrollResultCanvasRoot.SetActive(false);

        musicManager = MusicManager.Instance;
        if (musicManager == null) musicManager = FindObjectOfType<MusicManager>();

        beatMapSpawner = FindObjectOfType<BeatMapSpawner>();

        UpdateUI();
    }

    private void Update()
    {
        // 보험: 이벤트를 못 받았는데도 음악이 재생되면 시작 처리
        if (!gameStarted && autoStartWhenMusicPlays && musicManager != null && musicManager.audioSource != null)
        {
            if (musicManager.audioSource.isPlaying)
            {
                NotifyGameStarted();
            }
        }

        if (!gameStarted) return;

        // 곡 종료 조건: 음악 종료 + 스폰 완료 + 중복 방지
        if (!isGameEnded && musicManager != null && musicManager.audioSource != null)
        {
            // Time.timeSinceLevelLoad > 3f : 씬 로드 직후 isPlaying false 오검출 방지
            if (!musicManager.audioSource.isPlaying && Time.timeSinceLevelLoad > 3f)
            {
                if (beatMapSpawner != null && beatMapSpawner.IsSpawningComplete())
                {
                    Debug.Log("[RhythmGameManager] Music ended and all notes spawned. Ending game...");
                    StartCoroutine(EndGameWithDelay());
                }
            }
        }
    }

    private IEnumerator EndGameWithDelay()
    {
        isGameEnded = true;

        if (beatMapSpawner != null) beatMapSpawner.StopSpawning();

        Debug.Log($"[RhythmGameManager] Waiting {delayBeforeResults}s before showing results...");
        yield return new WaitForSeconds(delayBeforeResults);

        // 남은 노트/장애물 정리(원본 유지)
        Note[] remainingNotes = FindObjectsOfType<Note>();
        foreach (Note note in remainingNotes) Destroy(note.gameObject);

        Obstacle[] remainingObstacles = FindObjectsOfType<Obstacle>();
        foreach (Obstacle obstacle in remainingObstacles) Destroy(obstacle.gameObject);

        // ✅ 결과 표시(새 UI 우선)
        ShowResultsUnified();
    }

    // -----------------------
    // Hit Events
    // -----------------------
    public void OnPerfect()
    {
        if ((!gameStarted && !allowScoreWhenNotStarted) || isGameEnded)
        {
            LogIgnored("OnPerfect");
            return;
        }

        perfectHits++;
        combo++;

        float multiplier = 1f + (combo / 10) * comboMultiplierRate;
        int earnedScore = Mathf.RoundToInt(perfectScore * multiplier);

        score += earnedScore;
        if (combo > maxCombo) maxCombo = combo;

        if (comboSystem != null) comboSystem.AddCombo();

        UpdateUI();
    }

    public void OnGood()
    {
        if ((!gameStarted && !allowScoreWhenNotStarted) || isGameEnded)
        {
            LogIgnored("OnGood");
            return;
        }

        goodHits++;
        combo++;

        float multiplier = 1f + (combo / 10) * comboMultiplierRate;
        int earnedScore = Mathf.RoundToInt(goodScore * multiplier);

        score += earnedScore;
        if (combo > maxCombo) maxCombo = combo;

        if (comboSystem != null) comboSystem.AddCombo();

        UpdateUI();
    }

    public void OnMiss()
    {
        if ((!gameStarted && !allowScoreWhenNotStarted) || isGameEnded)
        {
            LogIgnored("OnMiss");
            return;
        }

        missHits++;
        combo = 0;

        if (comboSystem != null) comboSystem.ResetCombo();

        UpdateUI();
    }

    private void LogIgnored(string fn)
    {
        if (_loggedNotStartedOnce) return;
        _loggedNotStartedOnce = true;

        Debug.LogWarning(
            $"[RhythmGameManager] ⚠️ {fn} 호출됐지만 점수 반영이 무시됨. " +
            $"gameStarted={gameStarted}, isGameEnded={isGameEnded}. " +
            $"(원인: NotifyGameStarted()가 안 불렸을 수 있음 / autoStart 옵션 확인)"
        );
    }

    // -----------------------
    // UI Update
    // -----------------------
    private void UpdateUI()
    {
        if (scoreText != null) scoreText.text = $"SCORE: {score:N0}";

        if (comboText != null)
        {
            if (combo > 0)
            {
                comboText.text = combo.ToString();

                if (combo >= 50) comboText.color = Color.red;
                else if (combo >= 30) comboText.color = Color.yellow;
                else if (combo >= 10) comboText.color = Color.green;
                else comboText.color = Color.white;
            }
            else comboText.text = "";
        }

        if (accuracyText != null)
        {
            float accuracy = GetAccuracy();
            accuracyText.text = $"ACC: {accuracy:F1}%";
        }
    }

    // -----------------------
    // Accuracy & Rank
    // -----------------------
    public float GetAccuracy()
    {
        int totalHits = perfectHits + goodHits + missHits;
        if (totalHits == 0) return 100f;

        float weightedHits = (perfectHits * 1f) + (goodHits * 0.5f);
        return (weightedHits / totalHits) * 100f;
    }

    public string GetRank()
    {
        float accuracy = GetAccuracy();

        if (accuracy >= 95f) return "SS";
        if (accuracy >= 90f) return "S";
        if (accuracy >= 80f) return "A";
        if (accuracy >= 70f) return "B";
        if (accuracy >= 60f) return "C";
        return "D";
    }

    // -----------------------
    // Result (Unified)
    // -----------------------
    public void ShowResults()
    {
        ShowResultsUnified();
    }
    private void ShowResultsUnified()
    {
        // 1) 새 UI가 연결되어 있으면 새 UI로 표시
        if (scrollResultManager != null)
        {
            if (scrollResultCanvasRoot != null)
                scrollResultCanvasRoot.SetActive(true);

            ApplyResultsToScrollUI();
            scrollResultManager.ShowResults();
            return;
        }

        // 2) 새 UI가 없다면 기존 레거시 결과 패널 표시
        ShowResultsLegacy();
    }

    private void ApplyResultsToScrollUI()
    {
        scrollResultManager.finalScore = score;
        scrollResultManager.maxCombo = maxCombo;

        scrollResultManager.perfectCount = perfectHits;

        // 현재 판정 구조: Perfect/Good/Miss → great는 0
        scrollResultManager.greatCount = 0;
        scrollResultManager.goodCount = goodHits;

        scrollResultManager.missCount = missHits;

        scrollResultManager.accuracy = GetAccuracy();
        scrollResultManager.grade = GetRank();
    }

    // -----------------------
    // Result (Legacy)
    // -----------------------
    public void ShowResultsLegacy()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            StartCoroutine(AnimateResultPanel());
        }

        if (titleText != null) titleText.text = "GAME CLEAR!";
        if (resultScoreText != null) resultScoreText.text = $"SCORE: {score:N0}";
        if (maxComboText != null) maxComboText.text = $"MAX COMBO: {maxCombo}";
        if (perfectText != null) perfectText.text = $"Perfect: {perfectHits}";
        if (goodText != null) goodText.text = $"Good: {goodHits}";
        if (missText != null) missText.text = $"Miss: {missHits}";
        if (resultAccuracyText != null) resultAccuracyText.text = $"{GetAccuracy():F1}%";

        if (rankText != null)
        {
            string rank = GetRank();
            rankText.text = rank;

            switch (rank)
            {
                case "SS":
                case "S":
                    rankText.color = new Color(1f, 0.84f, 0f);
                    break;
                case "A":
                    rankText.color = new Color(0.5f, 1f, 0.5f);
                    break;
                case "B":
                    rankText.color = new Color(0.5f, 0.5f, 1f);
                    break;
                default:
                    rankText.color = Color.white;
                    break;
            }
        }
    }

    private IEnumerator AnimateResultPanel()
    {
        if (resultPanel == null) yield break;

        CanvasGroup canvasGroup = resultPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = resultPanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        resultPanel.transform.localScale = Vector3.one * 0.8f;

        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;

            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            float scale = Mathf.Lerp(0.8f, 1.05f, EaseOutBack(t));
            resultPanel.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;

            float scale = Mathf.Lerp(1.05f, 1f, t);
            resultPanel.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        canvasGroup.alpha = 1f;
        resultPanel.transform.localScale = Vector3.one;
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
