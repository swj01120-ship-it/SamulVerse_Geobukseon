using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour
{
    public float speed = 3f;
    public Vector3 targetPosition;

    [Header("Drum Type")]
    public string drumType = "Book"; // "Jung", "Jang", "Book", "Jing"

    [Header("Judgment")]
    public float perfectWindow = 0.3f;
    public float goodWindow = 0.6f;
    public float missWindow = 1.0f;

    [Header("State")]
    public bool hasBeenHit = false;

    private float previousDistance = float.MaxValue;
    private bool hasApproachedTarget = false;
    private bool hasPassedTarget = false; // 타겟 지나갔는지 체크

    public ParticleSystem destroyParticle;

    void Start()
    {
        if (targetPosition == Vector3.zero)
        {
            Debug.LogError("Note targetPosition is zero!");
        }

        previousDistance = Vector3.Distance(transform.position, targetPosition);
        Debug.Log($"Note created. Distance to target: {previousDistance:F2}m");
    }

    void Update()
    {
        if (hasBeenHit) return;

        // 타겟으로 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        float currentDistance = Vector3.Distance(transform.position, targetPosition);

        // 타겟에 가까워지는 중인지 확인
        if (currentDistance < previousDistance)
        {
            hasApproachedTarget = true;
        }

        // 타겟에 가까워졌다가 멀어지기 시작 = 지나감
        if (hasApproachedTarget && currentDistance > previousDistance + 0.1f)
        {
            if (!hasPassedTarget)
            {
                hasPassedTarget = true;
                Debug.Log($"[{drumType}] Note passed target zone");
            }
        }

        //  타겟을 완전히 지나쳐서 missWindow 밖으로 나갔을 때만 Miss
        if (hasPassedTarget && currentDistance > missWindow)
        {
            Debug.Log($"[{drumType}] AUTO MISS - passed target (distance: {currentDistance:F2}m > {missWindow:F2}m)");
            OnMiss();
            return;
        }

        // 타겟에 너무 가까이 도달 (goodWindow 안쪽까지 왔는데도 안 쳤으면 Miss)
        // 이전: 0.3f → 너무 빡빡함
        // 수정: goodWindow보다 안쪽으로 들어왔을 때
        if (hasApproachedTarget && currentDistance < goodWindow * 0.3f) // goodWindow의 30%
        {
            Debug.Log($"[{drumType}] AUTO MISS - too close without hit (distance: {currentDistance:F2}m)");
            OnMiss();
            return;
        }

        previousDistance = currentDistance;
    }

    public void OnHit(bool isPerfect)
    {
        if (hasBeenHit) return;
        hasBeenHit = true;

        Debug.Log($"Note hit: {(isPerfect ? "Perfect" : "Good")}");

        // ⭐ 튜토리얼 중이면 튜토리얼 매니저에 알림
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnNoteHitInTutorial(isPerfect);
            Debug.Log($"✅ [4단계] Tutorial note hit registered!");
        }
        // 실제 게임
        else if (RhythmGameManager.Instance != null)
        {
            if (isPerfect)
            {
                RhythmGameManager.Instance.OnPerfect();
            }
            else
            {
                RhythmGameManager.Instance.OnGood();
            }
        }

        Destroy(gameObject);
    }

    void OnMiss()
    {
        if (hasBeenHit) return;
        hasBeenHit = true;

        Debug.Log($"[{drumType}] MISS registered");

        if (RhythmGameManager.Instance != null)
        {
            RhythmGameManager.Instance.OnMiss();
        }

        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (targetPosition != Vector3.zero)
        {
            // Perfect 범위 (노란색)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition, perfectWindow);

            // Good 범위 (초록색)
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, goodWindow);

            // Miss 범위 (빨간색)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, missWindow);

            // 노트에서 타겟까지 선
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}
