using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DrumStick의 속도를 추적하고 햅틱 피드백을 제공합니다.
/// 
/// 충돌 감지는 DrumHit.cs (Cover 오브젝트)에서 처리됩니다.
/// OnTriggerEnter는 충돌하는 두 오브젝트 중 하나에만 있으면 작동하므로,
/// 이 스크립트에는 OnTriggerEnter가 없어도 정상 작동합니다.
/// </summary>
public class DrumStickController : MonoBehaviour
{
    [Header("Controller Settings")]
    public OVRInput.Controller controller; // Left or Right

    [Header("Velocity Tracking")]
    private Vector3 previousPosition;
    public float currentVelocity;

    [Header("Debug")]
    public bool showVelocity = false;

    // Unity 메시지 | 최초 1회
    void Start()
    {
        previousPosition = transform.position;
    }

    // Unity 메시지 | 매 프레임
    void Update()
    {
        // 속도 계산 (DrumHit.cs에서 판정 시 사용)
        currentVelocity = (transform.position - previousPosition).magnitude / Time.deltaTime;
        previousPosition = transform.position;

        // 디버그 모드일 때 속도 표시
        if (showVelocity)
        {
            Debug.Log($"{gameObject.name} Velocity: {currentVelocity:F2}");
        }
    }

    /// <summary>
    /// 햅틱 피드백 트리거 (DrumHit.cs에서 호출)
    /// </summary>
    public void TriggerHaptic(float intensity, float duration)
    {
        float frequency = 0.5f;
        float amplitude = Mathf.Clamp01(intensity);
        OVRInput.SetControllerVibration(frequency, amplitude, controller);

        Invoke("StopHaptic", duration);
    }

    // 햅틱 정지
    void StopHaptic()
    {
        OVRInput.SetControllerVibration(0, 0, controller);
    }

    /// <summary>
    /// 현재 속도 반환 (외부 참조용)
    /// </summary>
    public float GetVelocity()
    {
        return currentVelocity;
    }
}
