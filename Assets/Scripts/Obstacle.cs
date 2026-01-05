using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public Vector3 targetPosition;

    [Header("Collision Settings")]
    public float damageDistance = 0.8f;
    public float destroyDistance = 5f;

    [Header("Safety")]
    public float minLifeTime = 0.5f;
    private float spawnTime;

    private Transform playerCamera;
    private bool hasDamaged = false;
    private float previousDistanceToTarget = float.MaxValue;

    void Start()
    {
        spawnTime = Time.time;

        // VR 카메라 찾기
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

        Debug.Log($"★ Obstacle spawned at {transform.position}");

        // 초기 거리 저장
        previousDistanceToTarget = Vector3.Distance(transform.position, targetPosition);
    }

    void Update()
    {
        float aliveTime = Time.time - spawnTime;

        // 이동
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        // === 최소 생존 시간 보장 ===
        if (aliveTime < minLifeTime)
        {
            previousDistanceToTarget = distanceToTarget;
            return;
        }

        // === 플레이어 충돌 체크 ===
        if (playerCamera != null && !hasDamaged)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerCamera.position);

            // 충돌!
            if (distanceToPlayer < damageDistance)
            {
                OnHitPlayer();
                return;
            }
        }

        // === 타겟 도달 체크 (방향 무관) ===
        if (distanceToTarget < 0.5f)
        {
            Debug.Log($"Obstacle reached target (alive {aliveTime:F1}s)");
            Destroy(gameObject);
            return;
        }

        // === 타겟을 지나쳐서 멀어지는 중인지 체크 ===
        // 이전보다 거리가 멀어지기 시작하면 = 타겟을 지나침
        if (distanceToTarget > previousDistanceToTarget + 0.5f)
        {
            Debug.Log($"Obstacle passed target (alive {aliveTime:F1}s)");
            Destroy(gameObject);
            return;
        }

        previousDistanceToTarget = distanceToTarget;
    }

    void OnHitPlayer()
    {
        Debug.Log("★ OBSTACLE HIT PLAYER! Combo reset!");

        if (RhythmGameManager.Instance != null)
        {
            RhythmGameManager.Instance.OnMiss();
        }

        OVRInput.SetControllerVibration(1f, 0.3f, OVRInput.Controller.LTouch);
        OVRInput.SetControllerVibration(1f, 0.3f, OVRInput.Controller.RTouch);

        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DrumStick"))
        {
            Debug.Log("★ Obstacle destroyed by drumstick!");

            if (RhythmGameManager.Instance != null)
            {
                RhythmGameManager.Instance.score += 50;
            }

            Destroy(gameObject);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (targetPosition != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition);
        }
    }
}
