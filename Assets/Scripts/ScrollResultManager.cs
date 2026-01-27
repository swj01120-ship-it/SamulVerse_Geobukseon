using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScrollResultManager : MonoBehaviour
{
    [Header("=== 족자 이미지 ===")]
    public Image statisticsScroll;
    public Image scoreScroll;
    public Image gradeScroll;

    [Header("=== 빛 효과 ===")]
    public Image gradeBorder;

    [Header("=== 텍스트 UI (TMP) ===")]
    public TextMeshProUGUI statisticsText;
    public TextMeshProUGUI scoreText_number;
    public TextMeshProUGUI maxCombo_number;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI textPercent;

    [Header("=== 파티클 ===")]
    public ParticleSystem gradeParticles;

    [Header("=== 하단 버튼 (왼쪽부터: Home / Replay / Quit) ===")]
    public Button homeButton;
    public Button replayButton;
    public Button quitButton;

    [Header("=== 씬 이름 설정 ===")]
    public string homeSceneName = "Home";
    public bool replayReloadCurrentScene = true;
    public string replaySceneName = "MainGame";

    [Header("=== 게임 결과 데이터(테스트용) ===")]
    public string grade = "A";
    public float accuracy = 92.5f;
    public int perfectCount = 120;
    public int greatCount = 45;
    public int goodCount = 30;
    public int missCount = 3;
    public int finalScore = 12000;
    public int maxCombo = 87;

    private bool _isShowing = false;
    private Coroutine _running;

    private void Awake()
    {
        if (homeButton != null) homeButton.onClick.AddListener(GoHome);
        if (replayButton != null) replayButton.onClick.AddListener(Replay);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
    }

    private void Start()
    {
        ResetAll();
    }

    private void OnDestroy()
    {
        if (homeButton != null) homeButton.onClick.RemoveListener(GoHome);
        if (replayButton != null) replayButton.onClick.RemoveListener(Replay);
        if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
    }

    // =========================
    // 외부에서 호출: 결과 표시
    // =========================
    public void ShowResults()
    {
        if (_isShowing) return;
        _isShowing = true;

        if (_running != null) StopCoroutine(_running);
        _running = StartCoroutine(ShowResultsSequence());
    }

    public void HideResultsInstant()
    {
        if (_running != null) StopCoroutine(_running);
        _running = null;

        _isShowing = false;
        ResetAll();
    }

    // =========================
    // 초기화
    // =========================
    public void ResetAll()
    {
        Debug.Log("[ScrollResultManager] ResetAll");

        if (statisticsScroll) statisticsScroll.fillAmount = 0f;
        if (scoreScroll) scoreScroll.fillAmount = 0f;
        if (gradeScroll) gradeScroll.fillAmount = 0f;

        // ✅ 인스펙터에 설정된 글씨 색(RGB)은 그대로 두고, 알파만 0으로
        SetAlphaOnly(statisticsText, 0f);
        SetAlphaOnly(scoreText_number, 0f);
        SetAlphaOnly(maxCombo_number, 0f);
        SetAlphaOnly(gradeText, 0f);
        SetAlphaOnly(textPercent, 0f);

        if (gradeBorder) gradeBorder.color = new Color(gradeBorder.color.r, gradeBorder.color.g, gradeBorder.color.b, 0f);

        if (gradeParticles) gradeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void SetAlphaOnly(TextMeshProUGUI tmp, float a)
    {
        if (tmp == null) return;

        Color c = tmp.color; // RGB 유지
        c.a = a;
        tmp.color = c;
        tmp.ForceMeshUpdate();
    }

    // =========================
    // 메인 시퀀스
    // =========================
    private IEnumerator ShowResultsSequence()
    {
        // 1) 텍스트 내용 채우기
        if (statisticsText != null)
        {
            statisticsText.text =
                $"Miss: {missCount}\n" +
                $"Good: {goodCount}\n" +
                $"Perfect: {perfectCount}";
            // 필요하면 Great도 넣어:
            // + $"\nGreat: {greatCount}";
        }

        // ✅ 라벨+값 같이 출력(스샷 스타일)
        if (scoreText_number != null)
            scoreText_number.text = $"Score\n{finalScore:N0}";

        if (maxCombo_number != null)
            maxCombo_number.text = $"Max Combo :\n{maxCombo:N0}";

        if (gradeText != null)
            gradeText.text = grade;

        if (textPercent != null)
            textPercent.text = $"Accuracy\n{accuracy:F1}%";

        // 2) 족자 펼침(순서: 통계 -> 점수 -> 등급)
        yield return StartCoroutine(FillScroll(statisticsScroll, 0.35f));
        yield return StartCoroutine(FadeTMPAlphaOnly(statisticsText, 0.25f));

        yield return StartCoroutine(FillScroll(scoreScroll, 0.35f));
        yield return StartCoroutine(FadeTMPAlphaOnly(scoreText_number, 0.20f));
        yield return StartCoroutine(FadeTMPAlphaOnly(maxCombo_number, 0.20f));

        yield return StartCoroutine(FillScroll(gradeScroll, 0.35f));
        yield return StartCoroutine(FadeTMPAlphaOnly(gradeText, 0.20f));
        yield return StartCoroutine(FadeTMPAlphaOnly(textPercent, 0.20f));

        // 3) 등급 테두리 빛 + 파티클
        yield return StartCoroutine(GradeGlowEffect(0.8f));

        if (gradeParticles != null) gradeParticles.Play();

        _running = null;
    }

    private IEnumerator FillScroll(Image img, float duration)
    {
        if (img == null) yield break;

        float t = 0f;
        img.fillAmount = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            img.fillAmount = Mathf.Lerp(0f, 1f, t / duration);
            yield return null;
        }

        img.fillAmount = 1f;
    }

    // ✅ RGB 건드리지 않고 알파만 페이드
    private IEnumerator FadeTMPAlphaOnly(TextMeshProUGUI tmp, float duration)
    {
        if (tmp == null) yield break;

        Color c = tmp.color; // RGB 유지
        c.a = 0f;
        tmp.color = c;
        tmp.ForceMeshUpdate();

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / duration);
            tmp.color = c;
            yield return null;
        }

        c.a = 1f;
        tmp.color = c;
        tmp.ForceMeshUpdate();
    }

    private IEnumerator GradeGlowEffect(float duration)
    {
        if (gradeBorder == null) yield break;

        float t = 0f;
        Color baseColor = gradeBorder.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float wave = Mathf.PingPong(t * 4f, 1f);
            gradeBorder.color = new Color(baseColor.r, baseColor.g, baseColor.b, wave);
            yield return null;
        }

        gradeBorder.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
    }

    // =========================
    // 버튼 기능: Home / Replay / Quit
    // =========================
    public void GoHome()
    {
        if (!string.IsNullOrEmpty(homeSceneName))
            SceneManager.LoadScene(homeSceneName);
    }

    public void Replay()
    {
        if (replayReloadCurrentScene)
        {
            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.name);
            return;
        }

        if (!string.IsNullOrEmpty(replaySceneName))
            SceneManager.LoadScene(replaySceneName);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
