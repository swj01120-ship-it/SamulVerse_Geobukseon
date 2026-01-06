using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RhythmGameManager : MonoBehaviour
{
    public static RhythmGameManager Instance;

    [Header("Game UI")]
    public Text scoreText;
    public Text comboText;
    public Text accuracyText;

    [Header("Result UI")]
    public GameObject resultPanel;
    public Text titleText;
    public Text resultScoreText;
    public Text maxComboText;
    public Text perfectText;
    public Text goodText;
    public Text missText;
    public Text resultAccuracyText;
    public Text rankText;

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

    [Header("Animation")]
    public float fadeInDuration = 0.5f;
    public float scaleUpDuration = 0.3f;

    //  게임 종료 관련 추가
    [Header("Game End Settings")]
    public float delayBeforeResults = 1f; // 마지막 노트 후 결과창까지 대기 시간
    private bool isGameEnded = false;
    private MusicManager musicManager;
    private BeatMapSpawner beatMapSpawner;

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
        // 결과창 숨기기
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        // MusicManager와 BeatMapSpawner 찾기
        musicManager = MusicManager.Instance;
        if (musicManager == null)
        {
            musicManager = FindObjectOfType<MusicManager>();
        }

        beatMapSpawner = FindObjectOfType<BeatMapSpawner>();

        UpdateUI();
    }

    // 매 프레임 음악 종료 체크
    void Update()
    {
        if (!isGameEnded && musicManager != null && musicManager.audioSource != null)
        {
            // 음악이 재생 중이었는데 멈췄고, 게임 시작 후 3초 이상 지났으면
            if (!musicManager.audioSource.isPlaying && Time.timeSinceLevelLoad > 3f)
            {
                // 노트 스폰도 완료됐는지 체크
                if (beatMapSpawner != null && beatMapSpawner.IsSpawningComplete())
                {
                    Debug.Log("Music ended and all notes spawned. Ending game...");
                    StartCoroutine(EndGameWithDelay());
                }
            }
        }
    }

    // ⭐ 게임 종료 코루틴 (딜레이 후 결과 표시)
    IEnumerator EndGameWithDelay()
    {
        isGameEnded = true;

        // 노트 생성 즉시 중단
        if (beatMapSpawner != null)
        {
            beatMapSpawner.StopSpawning();
        }

        Debug.Log($"Waiting {delayBeforeResults}s before showing results...");
        yield return new WaitForSeconds(delayBeforeResults);

        // 남은 노트들 모두 제거
        Note[] remainingNotes = FindObjectsOfType<Note>();
        Debug.Log($"Cleaning up {remainingNotes.Length} remaining notes");
        foreach (Note note in remainingNotes)
        {
            Destroy(note.gameObject);
        }

        // 장애물도 제거
        Obstacle[] remainingObstacles = FindObjectsOfType<Obstacle>();
        foreach (Obstacle obstacle in remainingObstacles)
        {
            Destroy(obstacle.gameObject);
        }

        // 결과창 표시
        ShowResults();
    }

    public void OnPerfect()
    {
        perfectHits++;
        combo++;

        // 콤보 배수 계산
        float multiplier = 1f + (combo / 10) * comboMultiplierRate;
        int earnedScore = Mathf.RoundToInt(perfectScore * multiplier);

        score += earnedScore;

        if (combo > maxCombo)
        {
            maxCombo = combo;
        }

        if (comboSystem != null)
        {
            comboSystem.AddCombo();
        }

        UpdateUI();

        Debug.Log($"PERFECT! +{earnedScore} (Combo: {combo}, x{multiplier:F1})");
    }

    public void OnGood()
    {
        goodHits++;
        combo++;

        float multiplier = 1f + (combo / 10) * comboMultiplierRate;
        int earnedScore = Mathf.RoundToInt(goodScore * multiplier);

        score += earnedScore;

        if (combo > maxCombo)
        {
            maxCombo = combo;
        }

        if (comboSystem != null)
        {
            comboSystem.AddCombo();
        }

        UpdateUI();

        Debug.Log($"GOOD! +{earnedScore} (Combo: {combo}, x{multiplier:F1})");
    }

    public void OnMiss()
    {
        missHits++;
        combo = 0;

        if (comboSystem != null)
        {
            comboSystem.ResetCombo();
        }

        UpdateUI();

        Debug.Log("MISS! Combo reset!");
    }

    void UpdateUI()
    {
        // 게임 중 UI 업데이트
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {score:N0}";
        }

        if (comboText != null)
        {
            if (combo > 0)
            {
                comboText.text = combo.ToString();

                if (combo >= 50)
                    comboText.color = Color.red;
                else if (combo >= 30)
                    comboText.color = Color.yellow;
                else if (combo >= 10)
                    comboText.color = Color.green;
                else
                    comboText.color = Color.white;
            }
            else
            {
                comboText.text = "";
            }
        }

        if (accuracyText != null)
        {
            float accuracy = GetAccuracy();
            accuracyText.text = $"ACC: {accuracy:F1}%";
        }
    }

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

    public void ShowResults()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            StartCoroutine(AnimateResultPanel());
        }

        // 텍스트 업데이트
        if (titleText != null)
            titleText.text = "GAME CLEAR!";

        if (resultScoreText != null)
            resultScoreText.text = $"SCORE: {score:N0}";

        if (maxComboText != null)
            maxComboText.text = $"MAX COMBO: {maxCombo}";

        if (perfectText != null)
            perfectText.text = $"Perfect: {perfectHits}";

        if (goodText != null)
            goodText.text = $"Good: {goodHits}";

        if (missText != null)
            missText.text = $"Miss: {missHits}";

        if (resultAccuracyText != null)
            resultAccuracyText.text = $"{GetAccuracy():F1}%";

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

        Debug.Log("=== GAME RESULTS ===");
        Debug.Log($"Score: {score:N0}");
        Debug.Log($"Max Combo: {maxCombo}");
        Debug.Log($"Perfect: {perfectHits} | Good: {goodHits} | Miss: {missHits}");
        Debug.Log($"Accuracy: {GetAccuracy():F1}%");
        Debug.Log($"Rank: {GetRank()}");
    }

    IEnumerator AnimateResultPanel()
    {
        CanvasGroup canvasGroup = resultPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = resultPanel.AddComponent<CanvasGroup>();
        }

        // 초기 상태
        canvasGroup.alpha = 0f;
        resultPanel.transform.localScale = Vector3.one * 0.8f;

        float elapsed = 0f;

        // 페이드 인 + 스케일 업
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeInDuration;

            // 페이드 인
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

            // 스케일 업 (EaseOutBack 효과)
            float scale = Mathf.Lerp(0.8f, 1.05f, EaseOutBack(t));
            resultPanel.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        // 약간 바운스
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.2f;

            float scale = Mathf.Lerp(1.05f, 1f, t);
            resultPanel.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        // 최종 상태
        canvasGroup.alpha = 1f;
        resultPanel.transform.localScale = Vector3.one;
    }

    // EaseOutBack 함수
    float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
