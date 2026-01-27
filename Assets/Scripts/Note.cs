using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Note : MonoBehaviour
{
    public enum NoteState
    {
        Flying,     // 판정 존 밖
        Hittable,   // 판정 존 안 (타격 가능)
        Hit,        // 맞음
        Missed      // 지나침
    }

    [Header("Note Settings")]
    public string drumType = "Jung";
    public float speed = 3f;

    [Tooltip("보통 DrumHit.targetPoint 위치로 자동 설정됩니다.")]
    public Vector3 targetPosition;

    private Vector3 moveDirection;
    private float spawnTime;

    [Header("Judgment Settings (distance from Judge Center)")]
    public float perfectWindow = 0.3f;
    public float goodWindow = 0.6f;

    [Header("Judge Center Offset (for +Z incoming notes)")]
    [Tooltip("노트가 +Z로 날아오면, JudgeCenter를 -Z로 살짝 빼는 게 보통 더 자연스럽습니다.")]
    public float judgeOffsetForward = 0.03f; // meters (추천 0.02~0.05)

    [Header("DrumStick Detection")]
    public float minimumStickSpeed = 0.8f;

    [Header("Lifetime / Safety")]
    public float maxLifetime = 15f;
    public float minLifeTime = 0.2f; // 스폰 직후 오작동 방지

    [Header("Debug")]
    public bool logDebug = true;

    [Header("Hit VFX (Optional)")]
    public GameObject hitVfxPrefab;     // 스파크/링 VFX 프리팹
    public float destroyDelay = 0.10f;  // 연출 후 삭제
    public float popScale = 1.25f;      // 맞을 때 살짝 커졌다가 사라짐

    // 내부 참조
    private DrumHit targetDrum;
    private Transform judgePoint;       // ⭐ 판정 기준점 = DrumHit.targetPoint
    private NoteState state = NoteState.Flying;

    public bool hasBeenResolved => state == NoteState.Hit || state == NoteState.Missed;

    void Awake()
    {
        // Trigger 판정용
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void Start()
    {
        spawnTime = Time.time;

        // 드럼 찾고 judgePoint/targetPosition 자동 세팅
        if (judgePoint == null)
            FindTargetDrum();

        if (judgePoint != null)
            targetPosition = judgePoint.position;

        // 이동 방향 설정 (+Z로 날아오는 노트면 Spawn이 더 작은 Z, Target이 더 큰 Z일 것)
        if (targetPosition != Vector3.zero)
            moveDirection = (targetPosition - transform.position).normalized;
        else
            moveDirection = Vector3.forward;

        if (logDebug)
        {
            Debug.Log($"[Note-{drumType}] Spawn. pos={transform.position}, target={targetPosition}, dir={moveDirection}");
            Debug.Log($"[Note-{drumType}] JudgeCenter={GetJudgeCenter()} (offset={judgeOffsetForward})");
        }
    }

    void Update()
    {
        if (hasBeenResolved) return;

        float alive = Time.time - spawnTime;

        // 이동
        transform.position += moveDirection * speed * Time.deltaTime;

        // 수명 제한
        if (alive > maxLifetime)
        {
            if (logDebug) Debug.Log($"[Note-{drumType}] MaxLifetime -> AutoMiss");
            MissAndDestroy();
            return;
        }

        // 스폰 직후(겹침/트리거 오작동) 방지
        if (alive < minLifeTime) return;

        Vector3 center = GetJudgeCenter();
        if (center == Vector3.zero) return;

        float dist = Vector3.Distance(transform.position, center);

        // 1) 판정 존 진입 (Good 범위 안으로 들어오면)
        if (state == NoteState.Flying && dist <= goodWindow)
        {
            state = NoteState.Hittable;
            if (logDebug) Debug.Log($"[Note-{drumType}] Enter Zone dist={dist:F3}");
        }

        // 2) 판정 존 통과/지나침 (Hittable 상태에서 다시 Good 밖으로 벗어나면)
        // 노트가 드럼을 지나 앞으로(+Z) 더 가면 dist가 다시 증가하는 패턴을 이용
        if (state == NoteState.Hittable && dist > goodWindow)
        {
            if (logDebug) Debug.Log($"[Note-{drumType}] Passed Zone -> Miss dist={dist:F3}");
            MissAndDestroy();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasBeenResolved) return;

        // ⭐ Cover/Target에 닿았다고 MISS 처리하지 않는다.
        // 판정은 "스틱이 존 안에서 노트에 닿았는가"로만 처리.

        if (state != NoteState.Hittable) return;
        if (!other.CompareTag("DrumStick")) return;

        DrumStickController stick = other.GetComponent<DrumStickController>();
        if (stick == null) return;

        if (stick.currentSpeed < minimumStickSpeed)
        {
            if (logDebug) Debug.Log($"[Note-{drumType}] Stick too slow ({stick.currentSpeed:F2}) -> ignore");
            return;
        }

        float dist = Vector3.Distance(transform.position, GetJudgeCenter());
        bool isPerfect = dist <= perfectWindow;
        bool isGood = dist <= goodWindow;

        if (logDebug)
            Debug.Log($"[Note-{drumType}] HIT! dist={dist:F3} perfect<={perfectWindow} good<={goodWindow}");

        if (isPerfect) HitAndDestroy(perfect: true);
        else if (isGood) HitAndDestroy(perfect: false);
        else MissAndDestroy();
    }

    // ⭐ 판정 기준점: targetPoint + (노트가 +Z로 오므로 center를 -Z로 약간 이동)
    private Vector3 GetJudgeCenter()
    {
        if (judgePoint == null) return Vector3.zero;

        // 노트가 +Z로 오면 “판정점”을 -Z로 살짝 빼서 시각적 타이밍을 맞춤
        // (필요없으면 judgeOffsetForward = 0 으로)
        return judgePoint.position + (-judgePoint.forward * judgeOffsetForward);
    }

    private void FindTargetDrum()
    {
        DrumHit[] allDrums = FindObjectsOfType<DrumHit>();

        foreach (DrumHit drum in allDrums)
        {
            if (drum.drumType == drumType && drum.targetPoint != null)
            {
                targetDrum = drum;
                judgePoint = drum.targetPoint; // ⭐ 핵심: 판정 기준점은 무조건 targetPoint
                return;
            }
        }

        Debug.LogError($"[Note-{drumType}] No target drum found (check drumType & targetPoint).");
    }

    private void HitAndDestroy(bool perfect)
    {
        if (hasBeenResolved) return;
        state = NoteState.Hit;

        if (targetDrum != null)
            targetDrum.OnNoteHit(perfect, transform.position);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.OnNoteHitInTutorial(perfect);
        else if (RhythmGameManager.Instance != null)
        {
            if (perfect) RhythmGameManager.Instance.OnPerfect();
            else RhythmGameManager.Instance.OnGood();
        }

        // 1) VFX 생성(선택)
        if (hitVfxPrefab != null)
            Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);

        // 2) 스케일 팝 연출
        StartCoroutine(PopAndDestroy());
    }

    IEnumerator PopAndDestroy()
    {
        Vector3 start = transform.localScale;
        Vector3 target = start * popScale;

        float t = 0f;
        while (t < destroyDelay)
        {
            float k = t / destroyDelay;
            transform.localScale = Vector3.Lerp(start, target, k);
            t += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }


    private void MissAndDestroy()
    {
        if (hasBeenResolved) return;

        state = NoteState.Missed;

        if (targetDrum != null)
            targetDrum.OnNoteMiss(transform.position);

        if (RhythmGameManager.Instance != null)
            RhythmGameManager.Instance.OnMiss();

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        // 에디터에서 판정 범위 확인용
        Vector3 center = Application.isPlaying ? GetJudgeCenter() : (judgePoint != null ? judgePoint.position : Vector3.zero);
        if (center == Vector3.zero) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, perfectWindow);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(center, goodWindow);
    }
}
