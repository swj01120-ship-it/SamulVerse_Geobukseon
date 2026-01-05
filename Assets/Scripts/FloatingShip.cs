using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingShip : MonoBehaviour
{
    [Header("Float Settings")]
    public float floatAmplitude = 0.2f;  // 위아래 움직임 크기
    public float floatSpeed = 0.4f;      // 위아래 속도

    [Header("Tilt Settings")]
    public float tiltAmplitude = 1f;     // 좌우 기울기 각도
    public float tiltSpeed = 0.3f;       // 좌우 기울기 속도

    [Header("Roll Settings")]
    public float rollAmplitude = 0.8f;   // 앞뒤 기울기 각도
    public float rollSpeed = 0.35f;       // 앞뒤 기울기 속도

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Update()
    {
        // 위아래 흔들림 (파도)
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        Vector3 newPosition = startPosition + new Vector3(0, yOffset, 0);
        transform.position = newPosition;

        // 좌우 기울기 (Z축 회전)
        float tilt = Mathf.Sin(Time.time * tiltSpeed) * tiltAmplitude;

        // 앞뒤 기울기 (X축 회전)
        float roll = Mathf.Sin(Time.time * rollSpeed + 1.5f) * rollAmplitude;

        // 회전 적용
        Quaternion newRotation = startRotation * Quaternion.Euler(roll, 0, tilt);
        transform.rotation = newRotation;
    }
}
