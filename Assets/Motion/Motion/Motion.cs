using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomAnimator : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;
    [Range(0, 2)] public int layer = 0;

    [Header("Choose ONE input")]
    [Tooltip("Animator 상태 이름(예: IdleA, LookAround, Stretch). 상태 이름으로 직접 CrossFade합니다.")]
    public List<string> stateNames = new List<string>();

    [Tooltip("Animator 트리거 파라미터 이름(예: DoIdleA, DoLookAround). 트리거를 쏴서 상태 전이시킵니다.")]
    public List<string> triggerNames = new List<string>();

    [Header("Timing")]
    [Tooltip("각 동작 사이의 최소 대기 시간(초)")]
    public float minIdleDelay = 0.2f;
    [Tooltip("각 동작 사이의 최대 대기 시간(초)")]
    public float maxIdleDelay = 0.8f;

    [Tooltip("CrossFade 블렌드 시간(초)")]
    public float crossFade = 0.15f;

    [Header("Options")]
    [Tooltip("같은 동작이 연속으로 나오지 않게 방지")]
    public bool avoidImmediateRepeat = true;

    [Tooltip("Animator.speed에 따라 재생길이를 보정할지")]
    public bool respectAnimatorSpeed = true;

    // ─────────────────────────────────────────────────────────────────────────────
    // 처음 시작 모션 한 번만 재생하는 기능
    public enum StartType { None, SpecificState, SpecificTrigger }

    [Header("Start once with a specific motion")]
    public StartType startType = StartType.None;
    public string firstStateName;    // 예: "Hiphop"
    public string firstTriggerName;  // 예: "DoHiphop"
    [Tooltip("첫 모션 직후, 같은 동작이 바로 또 나오지 않게 방지")]
    public bool noImmediateRepeat = true;
    // ─────────────────────────────────────────────────────────────────────────────

    int lastIndex = -1;
    bool useStates => stateNames != null && stateNames.Count > 0;
    bool useTriggers => !useStates && triggerNames != null && triggerNames.Count > 0;

    void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        minIdleDelay = 0.2f;
        maxIdleDelay = 0.8f;
        crossFade = 0.15f;
        respectAnimatorSpeed = true;
        avoidImmediateRepeat = true;

        startType = StartType.None;
        firstStateName = string.Empty;
        firstTriggerName = string.Empty;
        noImmediateRepeat = true;
    }

    void Start()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!useStates && !useTriggers && startType == StartType.None)
        {
            Debug.LogWarning("[RandomAnimator] stateNames 또는 triggerNames 중 하나를 채워주세요.");
            enabled = false;
            return;
        }
        StartCoroutine(PlayInitialThenLoop());
    }

    // 처음 모션 1회 재생 → 이후 랜덤 루프
    IEnumerator PlayInitialThenLoop()
    {
        // 1) 시작 모션
        if (startType == StartType.SpecificState && !string.IsNullOrEmpty(firstStateName))
        {
            // 즉시 시작(부드럽게 시작하려면 CrossFadeInFixedTime 사용)
            animator.Play(firstStateName, layer, 0f);
            yield return StartCoroutine(WaitUntilState(firstStateName));

            float len = GetCurrentStateLength();
            if (respectAnimatorSpeed && animator.speed > 0f) len /= animator.speed;
            yield return new WaitForSeconds(len);

            if (noImmediateRepeat && stateNames != null)
                lastIndex = stateNames.IndexOf(firstStateName);
        }
        else if (startType == StartType.SpecificTrigger && !string.IsNullOrEmpty(firstTriggerName))
        {
            animator.ResetTrigger(firstTriggerName);
            animator.SetTrigger(firstTriggerName);

            // 전이 반영 대기 후 길이만큼 대기
            yield return new WaitForSeconds(crossFade * 0.5f);
            float len = GetCurrentStateLength();
            if (respectAnimatorSpeed && animator.speed > 0f) len /= animator.speed;
            if (len < 0.1f) len = 0.5f; // 안전값
            yield return new WaitForSeconds(len);

            if (noImmediateRepeat && triggerNames != null)
                lastIndex = triggerNames.IndexOf(firstTriggerName);
        }

        // 2) 랜덤 루프
        yield return StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        while (true)
        {
            int idx = PickIndex();
            if (useStates)
            {
                string state = stateNames[idx];
                animator.CrossFadeInFixedTime(state, crossFade, layer);
                yield return StartCoroutine(WaitUntilState(state));

                float len = GetCurrentStateLength();
                if (respectAnimatorSpeed && animator.speed > 0f) len /= animator.speed;
                yield return new WaitForSeconds(len);
            }
            else // useTriggers
            {
                string trig = triggerNames[idx];
                animator.ResetTrigger(trig);
                animator.SetTrigger(trig);

                yield return new WaitForSeconds(crossFade * 0.5f);

                float len = GetCurrentStateLength();
                if (respectAnimatorSpeed && animator.speed > 0f) len /= animator.speed;
                if (len < 0.1f) len = 0.5f;
                yield return new WaitForSeconds(len);
            }

            float idleDelay = Random.Range(minIdleDelay, maxIdleDelay);
            if (idleDelay > 0) yield return new WaitForSeconds(idleDelay);
        }
    }

    int PickIndex()
    {
        List<string> list = useStates ? stateNames : triggerNames;
        if (!avoidImmediateRepeat || list.Count <= 1)
        {
            lastIndex = Random.Range(0, list.Count);
            return lastIndex;
        }

        int idx;
        do { idx = Random.Range(0, list.Count); }
        while (idx == lastIndex);
        lastIndex = idx;
        return idx;
    }

    IEnumerator WaitUntilState(string state)
    {
        float timeout = 2f;
        float t = 0f;
        while (t < timeout)
        {
            var info = animator.GetCurrentAnimatorStateInfo(layer);
            if (info.IsName(state)) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }

    float GetCurrentStateLength()
    {
        var info = animator.GetCurrentAnimatorStateInfo(layer);
        return Mathf.Max(0.0f, info.length);
    }
}
