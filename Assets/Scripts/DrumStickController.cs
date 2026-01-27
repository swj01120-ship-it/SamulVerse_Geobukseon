using UnityEngine;

/// <summary>
/// DrumStick의 속도를 추적하고 햅틱 피드백을 제공합니다.
/// 충돌 감지는 DrumHit.cs에서 처리.
/// (중요) StickTip에 Collider/Rigidbody가 없으면 자동 보장.
/// </summary>
public class DrumStickController : MonoBehaviour
{
    [Header("Controller Settings")]
    public OVRInput.Controller controller; // LTouch / RTouch

    [Header("Auto Physics Setup")]
    public bool autoEnsurePhysics = true;
    public bool addSphereColliderIfMissing = false;
    public float sphereRadius = 0.04f;
    public bool forceRigidbodyKinematic = true;

    [Header("Velocity Tracking")]
    private Vector3 previousPosition;
    public float currentVelocity;

    public float currentSpeed => currentVelocity;

    [Header("Debug")]
    public bool showVelocity = false;

    private Rigidbody _rb;

    private void Awake()
    {
        if (!autoEnsurePhysics) return;

        // Tag 체크(설정은 못하고 경고만)
        if (!CompareTag("DrumStick"))
        {
            Debug.LogWarning($"[DrumStickController] '{name}' Tag가 DrumStick이 아님 -> DrumHit가 무시할 수 있음");
        }

        // Rigidbody 보장
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = gameObject.AddComponent<Rigidbody>();

        if (forceRigidbodyKinematic)
        {
            _rb.isKinematic = true;
            _rb.useGravity = false;
        }

        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        // Collider 보장(이미 있으면 그대로 사용)
        var col = GetComponent<Collider>();
        if (col == null && addSphereColliderIfMissing)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = sphereRadius;
            sc.isTrigger = false;
        }
    }

    private void Start()
    {
        previousPosition = transform.position;
    }

    private void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        currentVelocity = (transform.position - previousPosition).magnitude / dt;
        previousPosition = transform.position;

        if (showVelocity)
            Debug.Log($"{gameObject.name} Velocity: {currentVelocity:F2}");
    }

    public void TriggerHaptic(float intensity, float duration)
    {
        float frequency = 0.5f;
        float amplitude = Mathf.Clamp01(intensity);
        OVRInput.SetControllerVibration(frequency, amplitude, controller);
        CancelInvoke(nameof(StopHaptic));
        Invoke(nameof(StopHaptic), Mathf.Max(0f, duration));
    }

    private void StopHaptic()
    {
        OVRInput.SetControllerVibration(0, 0, controller);
    }

    public float GetVelocity() => currentVelocity;
    public float GetSpeed() => currentVelocity;
}
