using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrumStickController : MonoBehaviour
{
    public OVRInput.Controller controller; // Left or Right
    private Vector3 previousPosition;
    public float currentVelocity;

    void Start()
    {
        previousPosition = transform.position;
    }

    void Update()
    {
        // 속도 계산
        currentVelocity = (transform.position - previousPosition).magnitude / Time.deltaTime;
        previousPosition = transform.position;
    }

    public void TriggerHaptic(float intensity)
    {
        // 진동 강도 (0~1)
        float frequency = 0.5f;
        float amplitude = Mathf.Clamp01(intensity);
        OVRInput.SetControllerVibration(frequency, amplitude, controller);

        // 0.1초 후 진동 멈춤
        Invoke("StopHaptic", 0.1f);
    }

    void StopHaptic()
    {
        OVRInput.SetControllerVibration(0, 0, controller);
    }
}
