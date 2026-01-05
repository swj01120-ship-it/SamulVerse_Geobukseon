using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Note : MonoBehaviour
{
    public float speed = 5f;
    public Vector3 targetPosition;

    [Header("Judgment")]
    public float perfectWindow = 0.15f;
    public float goodWindow = 0.35f;

    [Header("State")]
    public bool hasBeenHit = false;

    private float previousDistance = float.MaxValue;
    private bool hasApproachedTarget = false;

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

        // ★ 타겟에 가까워졌다가 멀어지기 시작하면 Miss ★
        if (hasApproachedTarget && currentDistance > previousDistance + 0.5f)
        {
            Debug.Log($"MISS - Note passed target (distance: {currentDistance:F2}m)");
            OnMiss();
            return;
        }

        // 타겟에 거의 도달했는데 안 쳤으면 Miss
        if (hasApproachedTarget && currentDistance < 0.3f)
        {
            Debug.Log($"MISS - Note reached target without hit");
            OnMiss();
            return;
        }

        previousDistance = currentDistance;
    }

    public void OnHit(bool isPerfect)
    {
        if (hasBeenHit) return;
        hasBeenHit = true;

        // ★ 파티클 재생 ★
        if (destroyParticle != null)
        {
            destroyParticle.transform.SetParent(null); // 부모 해제
            destroyParticle.Play();
            Destroy(destroyParticle.gameObject, 2f); // 2초 후 삭제
        }

        if (RhythmGameManager.Instance != null)
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
            // Perfect 범위
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(targetPosition, perfectWindow);

            // Good 범위
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition, goodWindow);

            // 노트에서 타겟까지 선
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}
