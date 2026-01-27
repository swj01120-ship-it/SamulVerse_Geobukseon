using UnityEngine;

public class CylinderRotatorVR : MonoBehaviour
{
    [Header("Ray Origin (Right Controller)")]
    public Transform rayOrigin;

    [Header("Input")]
    [Tooltip("트리거 아날로그 임계값(이 값 이상이면 누름으로 간주)")]
    [Range(0.05f, 0.95f)]
    public float triggerThreshold = 0.55f;

    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    [Header("Rotation")]
    [Tooltip("Yaw 변화(도)에 곱해지는 감도. 1이면 그대로, 2면 2배.")]
    public float sensitivity = 1.5f;
    public bool invert = false;

    [Header("Raycast")]
    public float rayDistance = 10f;
    public LayerMask hitMask = ~0;
    [Tooltip("Collider가 Trigger여도 잡히게 할지")]
    public bool allowTriggerHit = true;

    [Header("Debug")]
    public bool debugLog = false;

    private bool dragging;
    private float lastYaw;

    void Update()
    {
        if (rayOrigin == null) return;

        // ✅ 버튼 Get 대신 아날로그 트리거 값으로 안정화
        float trig = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, controller);
        bool pressed = trig >= triggerThreshold;

        if (!dragging)
        {
            // 잡기 시작은 "누른 순간" + "레이가 나를 맞췄을 때"만
            if (pressed && IsRayHittingMe())
            {
                dragging = true;
                lastYaw = rayOrigin.eulerAngles.y;
                if (debugLog) Debug.Log($"[CylinderRotatorVR] DRAG START trig={trig:0.00}");
            }
        }
        else
        {
            // 잡고 있는 동안엔 레이가 잠깐 벗어나도 계속 회전 (끊김 방지)
            if (!pressed)
            {
                dragging = false;
                if (debugLog) Debug.Log($"[CylinderRotatorVR] DRAG END trig={trig:0.00}");
                return;
            }

            float yaw = rayOrigin.eulerAngles.y;
            float delta = Mathf.DeltaAngle(lastYaw, yaw);
            lastYaw = yaw;

            float dir = invert ? -1f : 1f;
            float amount = delta * sensitivity * dir;

            transform.Rotate(0f, amount, 0f, Space.World);

            if (debugLog) Debug.Log($"[CylinderRotatorVR] delta={delta:0.00} amount={amount:0.00} trig={trig:0.00}");
        }
    }

    private bool IsRayHittingMe()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        var qti = allowTriggerHit ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore;

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, hitMask, qti))
        {
            if (hit.collider == null) return false;
            return hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform);
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (rayOrigin == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + rayOrigin.forward * rayDistance);
    }
#endif
}
